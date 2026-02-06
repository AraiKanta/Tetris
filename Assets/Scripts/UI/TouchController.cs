using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using R3;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Tetris.UI
{
    /// <summary>
    /// モバイルタッチコントロール - スマホ用の操作
    /// 
    /// 【操作方法】
    /// - 左スワイプ: 左移動
    /// - 右スワイプ: 右移動
    /// - 下スワイプ: ソフトドロップ
    /// - タップ: 回転
    /// - 長押し: ハードドロップ
    /// 
    /// 【注意】
    /// UIボタンをタップした場合はスワイプ処理をスキップ
    /// </summary>
    public class TouchController : MonoBehaviour
    {
        [Header("スワイプ設定")]
        [SerializeField]
        [Tooltip("スワイプと判定する最小距離（ピクセル）")]
        private float swipeThreshold = 50f;

        [SerializeField]
        [Tooltip("長押しと判定する時間（秒）")]
        private float holdThreshold = 0.5f;

        [Header("UIボタン（オプション）")]
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button dropButton;
        [SerializeField] private Button hardDropButton;

        // イベント
        public Observable<Unit> OnMoveLeft => _onMoveLeft;
        private readonly Subject<Unit> _onMoveLeft = new Subject<Unit>();

        public Observable<Unit> OnMoveRight => _onMoveRight;
        private readonly Subject<Unit> _onMoveRight = new Subject<Unit>();

        public Observable<Unit> OnRotate => _onRotate;
        private readonly Subject<Unit> _onRotate = new Subject<Unit>();

        public Observable<bool> OnSoftDrop => _onSoftDrop;
        private readonly ReactiveProperty<bool> _onSoftDrop = new ReactiveProperty<bool>(false);

        public Observable<Unit> OnHardDrop => _onHardDrop;
        private readonly Subject<Unit> _onHardDrop = new Subject<Unit>();

        // 内部状態
        private Vector2 _touchStartPosition;
        private float _touchStartTime;
        private bool _swipeDetected;
        private int _activeTouchId = -1;
        private bool _touchStartedOnUI = false;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void Start()
        {
            SetupButtonListeners();
            Debug.Log("[TouchController] Started");
        }

        private void Update()
        {
            HandleTouchInput();
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
            _onMoveLeft.Dispose();
            _onMoveRight.Dispose();
            _onRotate.Dispose();
            _onSoftDrop.Dispose();
            _onHardDrop.Dispose();
        }

        private void SetupButtonListeners()
        {
            if (leftButton != null)
            {
                leftButton.OnClickAsObservable()
                    .Subscribe(_ => _onMoveLeft.OnNext(Unit.Default))
                    .AddTo(_disposables);
            }

            if (rightButton != null)
            {
                rightButton.OnClickAsObservable()
                    .Subscribe(_ => _onMoveRight.OnNext(Unit.Default))
                    .AddTo(_disposables);
            }

            if (rotateButton != null)
            {
                rotateButton.OnClickAsObservable()
                    .Subscribe(_ => _onRotate.OnNext(Unit.Default))
                    .AddTo(_disposables);
            }

            // ドロップボタンはPointerDown/Upを使う
            if (dropButton != null)
            {
                var handler = dropButton.gameObject.AddComponent<PointerDownHandler>();
                handler.OnPointerDownEvent
                    .Subscribe(_ => _onSoftDrop.Value = true)
                    .AddTo(_disposables);
                handler.OnPointerUpEvent
                    .Subscribe(_ => _onSoftDrop.Value = false)
                    .AddTo(_disposables);
            }

            if (hardDropButton != null)
            {
                hardDropButton.OnClickAsObservable()
                    .Subscribe(_ => _onHardDrop.OnNext(Unit.Default))
                    .AddTo(_disposables);
            }
        }

        /// <summary>
        /// タッチ位置がUIの上にあるかチェック
        /// </summary>
        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = screenPosition;
            
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            return results.Count > 0;
        }

        private void HandleTouchInput()
        {
            var touches = Touch.activeTouches;

            // タッチがない場合
            if (touches.Count == 0)
            {
                if (_activeTouchId >= 0 && !_touchStartedOnUI)
                {
                    // タッチ終了（UI上で開始していない場合のみ）
                    if (!_swipeDetected)
                    {
                        float duration = Time.time - _touchStartTime;
                        if (duration < holdThreshold)
                        {
                            _onRotate.OnNext(Unit.Default);
                        }
                        else
                        {
                            _onHardDrop.OnNext(Unit.Default);
                        }
                    }
                }

                _activeTouchId = -1;
                _swipeDetected = false;
                _touchStartedOnUI = false;
                _onSoftDrop.Value = false;
                return;
            }

            var touch = touches[0];

            if (_activeTouchId < 0)
            {
                // 新しいタッチ開始
                _activeTouchId = touch.touchId;
                _touchStartPosition = touch.screenPosition;
                _touchStartTime = Time.time;
                _swipeDetected = false;
                
                // UIの上でタッチが開始されたかチェック
                _touchStartedOnUI = IsPointerOverUI(touch.screenPosition);
                
                if (_touchStartedOnUI)
                {
                    Debug.Log("[TouchController] Touch started on UI - ignoring swipe");
                }
            }
            else if (touch.touchId == _activeTouchId && !_touchStartedOnUI)
            {
                // UI以外でのタッチの場合のみスワイプ処理
                if (!_swipeDetected)
                {
                    Vector2 delta = touch.screenPosition - _touchStartPosition;

                    if (delta.magnitude >= swipeThreshold)
                    {
                        _swipeDetected = true;

                        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                        {
                            if (delta.x > 0)
                            {
                                _onMoveRight.OnNext(Unit.Default);
                            }
                            else
                            {
                                _onMoveLeft.OnNext(Unit.Default);
                            }
                        }
                        else if (delta.y < 0)
                        {
                            _onSoftDrop.Value = true;
                        }

                        // 連続スワイプを許可
                        _touchStartPosition = touch.screenPosition;
                        _swipeDetected = false;
                    }
                }
            }

            // タッチ終了
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                if (!_swipeDetected && !_touchStartedOnUI)
                {
                    float duration = Time.time - _touchStartTime;
                    if (duration < holdThreshold)
                    {
                        _onRotate.OnNext(Unit.Default);
                    }
                    else
                    {
                        _onHardDrop.OnNext(Unit.Default);
                    }
                }

                _activeTouchId = -1;
                _swipeDetected = false;
                _touchStartedOnUI = false;
                _onSoftDrop.Value = false;
            }
        }
    }

    /// <summary>
    /// ボタン押下検出用ヘルパー
    /// </summary>
    public class PointerDownHandler : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler
    {
        private readonly Subject<Unit> _onPointerDown = new Subject<Unit>();
        private readonly Subject<Unit> _onPointerUp = new Subject<Unit>();

        public Observable<Unit> OnPointerDownEvent => _onPointerDown;
        public Observable<Unit> OnPointerUpEvent => _onPointerUp;

        public void OnPointerDown(PointerEventData eventData)
        {
            _onPointerDown.OnNext(Unit.Default);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _onPointerUp.OnNext(Unit.Default);
        }

        private void OnDestroy()
        {
            _onPointerDown.Dispose();
            _onPointerUp.Dispose();
        }
    }
}
