using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;
using R3;
using Tetris.Models;
using Tetris.Views;
using Tetris.Core;

namespace Tetris.Presenters
{
    // ============================================================
    // MVP設計: Presenter層
    // ============================================================
    //
    // 【Presenterの責務】
    // - ModelとViewの仲介者として機能する
    // - Modelの状態変化を監視してViewに反映する
    // - Viewからの入力イベントを受けてModelを更新する
    // - ゲームロジック（ライン消去処理など）を実行する
    //
    // 【Presenterの特徴】
    // - ModelとViewの両方を参照する（中間層）
    // - IStartable/ITickableでUnityのライフサイクルに対応
    // - VContainerで依存性を注入される
    //
    // 【MVPにおけるデータフロー】
    // 1. View: ユーザー入力 → イベント発火
    // 2. Presenter: イベントを受けてModelを更新
    // 3. Model: 状態変化 → ReactivePropertyで通知
    // 4. Presenter: 変化を検知してViewを更新
    // 5. View: 表示を更新
    //
    // ============================================================

    /// <summary>
    /// ゲーム全体を管理するPresenter
    /// 
    /// GameModelとGameViewを仲介し、
    /// ゲームのメインロジックを実行する
    /// </summary>
    public class GamePresenter : IStartable, IDisposable
    {
        // ============================================================
        // 依存性（VContainerで注入）
        // ============================================================
        // コンストラクタインジェクションで受け取る
        // これによりモックを使ったテストが容易になる

        /// <summary>ゲームの状態を保持するModel</summary>
        private readonly GameModel _model;

        /// <summary>UIを表示するView</summary>
        private readonly IGameView _view;

        /// <summary>テトロミノを生成するSpawner</summary>
        private readonly Spawner _spawner;

        /// <summary>購読の一括管理用</summary>
        private readonly CompositeDisposable _disposables = new();

        // ============================================================
        // コンストラクタ（依存性注入）
        // ============================================================

        /// <summary>
        /// GamePresenterのコンストラクタ
        /// 
        /// VContainerが自動的に依存性を注入する
        /// ModelとViewを受け取り、仲介の準備をする
        /// </summary>
        [Inject]
        public GamePresenter(GameModel model, IGameView view, Spawner spawner)
        {
            _model = model;
            _view = view;
            _spawner = spawner;
        }

        // ============================================================
        // IStartable実装（初期化）
        // ============================================================

        /// <summary>
        /// ゲーム開始時の初期化
        /// 
        /// MonoBehaviourのStartに相当
        /// ModelとViewの購読を設定する
        /// </summary>
        public void Start()
        {
            // ----------------------------------------
            // Model → View へのバインディング
            // ----------------------------------------
            // Modelの状態変化を監視してViewに反映する
            // これがMVPの核心：PresenterがModelの変化を検知してViewを更新

            // スコアが変化したらViewを更新
            _model.Score
                .Subscribe(score => _view.UpdateScore(score))
                .AddTo(_disposables);

            // ライン数が変化したらViewを更新
            _model.LinesCleared
                .Subscribe(lines => _view.UpdateLines(lines))
                .AddTo(_disposables);

            // ゲームオーバー状態が変化したらViewを更新
            _model.IsGameOver
                .Subscribe(isGameOver => _view.ShowGameOverPanel(isGameOver))
                .AddTo(_disposables);

            // ----------------------------------------
            // View → Presenter へのバインディング
            // ----------------------------------------
            // Viewの入力イベントを購読してアクションを実行

            // リトライボタンがクリックされたらゲームをリセット
            _view.OnRetryClicked
                .Subscribe(_ => HandleRetry())
                .AddTo(_disposables);

            // 初期表示を更新
            _view.UpdateScore(0);
            _view.UpdateLines(0);

            // 最初のテトロミノを生成
            _spawner.SpawnNext();
        }

        // ============================================================
        // ゲームロジック
        // ============================================================

        /// <summary>
        /// リトライ処理
        /// 
        /// Viewからのリトライ要求を受けて、
        /// Modelをリセットしてゲームを再開する
        /// </summary>
        private void HandleRetry()
        {
            // シーン上の全テトロミノを削除
            var tetrominoes = UnityEngine.Object.FindObjectsByType<Tetromino>(FindObjectsSortMode.None);
            foreach (var tetromino in tetrominoes)
            {
                UnityEngine.Object.Destroy(tetromino.gameObject);
            }

            // Modelをリセット
            _model.Reset();

            // 新しいテトロミノを生成
            _spawner.SpawnNext();
        }

        /// <summary>
        /// テトロミノをグリッドに追加
        /// 
        /// TetrominoPresenterから呼び出される
        /// ブロックをグリッドに登録する
        /// </summary>
        public void AddTetrominoToGrid(Transform tetromino)
        {
            // 各ブロック（子オブジェクト）をグリッドに登録
            foreach (Transform child in tetromino)
            {
                // SpriteRendererを持つもののみ（ブロック識別）
                if (child.GetComponent<SpriteRenderer>() == null) continue;

                Vector2 pos = RoundVector(child.position);
                int x = (int)pos.x;
                int y = (int)pos.y;

                if (y < GameModel.GRID_HEIGHT)
                {
                    _model.SetCell(x, y, child);
                }
            }
        }

        /// <summary>
        /// ライン消去チェックと実行
        /// 
        /// テトロミノが固定された後に呼び出される
        /// 完成したラインを検出して消去する
        /// </summary>
        public void CheckAndClearLines()
        {
            int linesCleared = 0;

            // 下から上に向かってチェック
            for (int y = 0; y < GameModel.GRID_HEIGHT; y++)
            {
                if (_model.IsRowFull(y))
                {
                    // ラインを消去
                    ClearLine(y);
                    linesCleared++;

                    // 同じ行を再チェック（上から落ちてきたブロック用）
                    y--;
                }
            }

            // スコアを加算
            if (linesCleared > 0)
            {
                _model.AddLinesCleared(linesCleared);
            }
        }

        /// <summary>
        /// 1ラインを消去して上のブロックを落下させる
        /// </summary>
        private void ClearLine(int y)
        {
            // ブロックを削除（GameObjectを破壊）
            Transform[] blocks = _model.GetRowBlocks(y);
            foreach (var block in blocks)
            {
                if (block != null)
                {
                    UnityEngine.Object.Destroy(block.gameObject);
                }
            }

            // グリッドデータをシフト
            _model.ShiftRowsDown(y);

            // 上のブロックの位置を更新（視覚的に落下）
            for (int row = y; row < GameModel.GRID_HEIGHT; row++)
            {
                for (int x = 0; x < GameModel.GRID_WIDTH; x++)
                {
                    Transform block = _model.GetCell(x, row);
                    if (block != null)
                    {
                        block.position = new Vector3(x, row, 0);
                    }
                }
            }
        }

        /// <summary>
        /// 指定位置が有効かチェック
        /// </summary>
        public bool IsValidPosition(Transform tetromino)
        {
            foreach (Transform child in tetromino)
            {
                if (child.GetComponent<SpriteRenderer>() == null) continue;

                Vector2 pos = RoundVector(child.position);
                int x = (int)pos.x;
                int y = (int)pos.y;

                if (!_model.IsValidCell(x, y))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// ゲームオーバーチェック
        /// </summary>
        public bool CheckGameOver(Transform tetromino)
        {
            foreach (Transform child in tetromino)
            {
                if (child.GetComponent<SpriteRenderer>() == null) continue;

                Vector2 pos = RoundVector(child.position);
                int x = (int)pos.x;
                int y = (int)pos.y;

                if (y >= GameModel.GRID_HEIGHT - 1)
                {
                    if (_model.GetCell(x, y) != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// ゲームオーバーを発生させる
        /// </summary>
        public void TriggerGameOver()
        {
            _model.TriggerGameOver();
        }

        /// <summary>
        /// ゲームがプレイ中かどうか
        /// </summary>
        public bool IsPlaying => _model.IsPlaying.CurrentValue;

        /// <summary>
        /// 座標を丸める
        /// </summary>
        private Vector2 RoundVector(Vector2 v)
        {
            return new Vector2(Mathf.Round(v.x), Mathf.Round(v.y));
        }

        // ============================================================
        // IDisposable実装
        // ============================================================

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
