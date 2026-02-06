using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using Cysharp.Threading.Tasks;
using R3;
using Tetris.Presenters;
using Tetris.UI;

namespace Tetris.Core
{
    // ============================================================
    // MVP設計: Tetrominoの役割
    // ============================================================
    //
    // 【Tetrominoの責務】
    // - プレイヤー入力を処理する（キーボード/タッチ）
    // - 移動・回転ロジックを実行する
    // - Presenterに固定・ライン消去を依頼する
    //
    // 【MVPにおける位置づけ】
    // Tetrominoは「動的に生成されるPresenter兼View」
    // - View的側面: 視覚的なブロック表示
    // - Presenter的側面: 入力処理とロジック
    //
    // 厳密にはTetrominoPresenter/TetrominoViewに分離可能だが、
    // 密結合な操作が多いため、実用的には1クラスにまとめる
    //
    // ============================================================

    /// <summary>
    /// テトロミノ制御クラス
    /// 
    /// 個々のテトロミノの動作を管理し、
    /// GamePresenterと連携してゲームを進行する
    /// </summary>
    public class Tetromino : MonoBehaviour, IDisposable
    {
        // ============================================================
        // インスペクタ設定
        // ============================================================

        [Header("落下設定")]
        [SerializeField]
        [Tooltip("通常の落下間隔（秒）")]
        private float fallTime = 1.0f;

        [SerializeField]
        [Tooltip("ソフトドロップ時の速度倍率")]
        private float softDropMultiplier = 0.1f;

        [Header("回転設定")]
        [SerializeField]
        [Tooltip("回転の中心点")]
        private Transform pivot;

        // ============================================================
        // 依存性（VContainerで注入）
        // ============================================================

        /// <summary>ゲームプレゼンター（メインロジック担当）</summary>
        private GamePresenter _presenter;

        /// <summary>スポナー（次のテトロミノ生成用）</summary>
        private Spawner _spawner;

        /// <summary>タッチコントローラー（モバイル入力用）</summary>
        private TouchController _touchController;

        [Inject]
        public void Construct(GamePresenter presenter, Spawner spawner)
        {
            _presenter = presenter;
            _spawner = spawner;
            _touchController = FindAnyObjectByType<TouchController>();
        }

        // ============================================================
        // 状態管理
        // ============================================================

        /// <summary>テトロミノがアクティブかどうか</summary>
        private readonly ReactiveProperty<bool> _isActive = new(true);

        /// <summary>購読管理用</summary>
        private readonly CompositeDisposable _disposables = new();

        /// <summary>非同期処理のキャンセル用</summary>
        private CancellationTokenSource _cts;

        /// <summary>ソフトドロップ中かどうか</summary>
        private bool _isSoftDropping = false;

        // ============================================================
        // Unity ライフサイクル
        // ============================================================

        private void Start()
        {
            _cts = new CancellationTokenSource();

            // ゲームオーバーチェック
            if (_presenter != null && _presenter.CheckGameOver(transform))
            {
                _presenter.TriggerGameOver();
                _isActive.Value = false;
                return;
            }

            SetupTouchSubscriptions();
            FallLoopAsync(_cts.Token).Forget();
        }

        private void Update()
        {
            if (!_isActive.Value) return;
            if (_presenter != null && !_presenter.IsPlaying) return;

            HandleKeyboardInput();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            Dispose();
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _isActive.Dispose();
        }

        // ============================================================
        // タッチ入力購読
        // ============================================================

        private void SetupTouchSubscriptions()
        {
            if (_touchController == null) return;

            _touchController.OnMoveLeft
                .Where(_ => _isActive.Value)
                .Subscribe(_ => Move(Vector3.left))
                .AddTo(_disposables);

            _touchController.OnMoveRight
                .Where(_ => _isActive.Value)
                .Subscribe(_ => Move(Vector3.right))
                .AddTo(_disposables);

            _touchController.OnRotate
                .Where(_ => _isActive.Value)
                .Subscribe(_ => Rotate())
                .AddTo(_disposables);

            _touchController.OnHardDrop
                .Where(_ => _isActive.Value)
                .Subscribe(_ => HardDrop())
                .AddTo(_disposables);

            _touchController.OnSoftDrop
                .Subscribe(isSoftDropping => _isSoftDropping = isSoftDropping)
                .AddTo(_disposables);
        }

        // ============================================================
        // キーボード入力
        // ============================================================

        private void HandleKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.leftArrowKey.wasPressedThisFrame)
                Move(Vector3.left);

            if (keyboard.rightArrowKey.wasPressedThisFrame)
                Move(Vector3.right);

            if (keyboard.upArrowKey.wasPressedThisFrame)
                Rotate();

            if (keyboard.spaceKey.wasPressedThisFrame)
                HardDrop();

            bool keyboardSoftDrop = keyboard.downArrowKey.isPressed;
            if (keyboardSoftDrop)
                _isSoftDropping = true;
            else if (_touchController == null)
                _isSoftDropping = false;
        }

        // ============================================================
        // 落下処理（UniTask）
        // ============================================================

        private async UniTaskVoid FallLoopAsync(CancellationToken ct)
        {
            try
            {
                while (_isActive.Value && !ct.IsCancellationRequested)
                {
                    float currentFallTime = _isSoftDropping
                        ? fallTime * softDropMultiplier
                        : fallTime;

                    await UniTask.Delay(
                        TimeSpan.FromSeconds(currentFallTime),
                        cancellationToken: ct
                    );

                    if (_isActive.Value)
                        Move(Vector3.down);
                }
            }
            catch (OperationCanceledException) { }
        }

        // ============================================================
        // 移動・回転ロジック
        // ============================================================

        private void Move(Vector3 direction)
        {
            transform.position += direction;

            if (!IsValidPosition())
            {
                transform.position -= direction;

                if (direction == Vector3.down)
                    LockTetromino();
            }
        }

        private void Rotate()
        {
            Transform pivotPoint = pivot != null ? pivot : transform;
            transform.RotateAround(pivotPoint.position, Vector3.forward, -90f);

            if (!IsValidPosition())
            {
                if (!TryWallKick())
                    transform.RotateAround(pivotPoint.position, Vector3.forward, 90f);
            }
        }

        private bool TryWallKick()
        {
            Vector3[] kickOffsets = { Vector3.left, Vector3.right, Vector3.left * 2, Vector3.right * 2 };

            foreach (var offset in kickOffsets)
            {
                transform.position += offset;
                if (IsValidPosition()) return true;
                transform.position -= offset;
            }
            return false;
        }

        private void HardDrop()
        {
            while (true)
            {
                transform.position += Vector3.down;
                if (!IsValidPosition())
                {
                    transform.position -= Vector3.down;
                    break;
                }
            }
            LockTetromino();
        }

        // ============================================================
        // 位置検証・固定
        // ============================================================

        private bool IsValidPosition()
        {
            return _presenter != null && _presenter.IsValidPosition(transform);
        }

        /// <summary>
        /// テトロミノを固定してPresenterに通知
        /// 
        /// MVPのフロー:
        /// 1. Tetromino → Presenterにブロック追加を依頼
        /// 2. Presenter → Modelにデータを登録
        /// 3. Presenter → ライン消去チェック
        /// 4. Spawner → 次のテトロミノ生成
        /// </summary>
        private void LockTetromino()
        {
            _isActive.Value = false;

            if (_presenter != null)
            {
                _presenter.AddTetrominoToGrid(transform);
                _presenter.CheckAndClearLines();
            }

            if (_spawner != null)
                _spawner.SpawnNext();
        }
    }
}
