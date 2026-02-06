using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Tetris.UI
{
    /// <summary>
    /// アプリ終了確認ダイアログ
    /// 
    /// 【機能】
    /// - 終了ボタンまたはAndroidの戻るボタンで確認ダイアログを表示
    /// - ダイアログ表示中はゲームを一時停止（TimeScale = 0）
    /// - 「終了」でアプリ終了、「戻る」でゲーム再開
    /// </summary>
    public class ExitConfirmDialog : MonoBehaviour
    {
        [Header("UI参照")]
        [SerializeField]
        [Tooltip("確認ダイアログのパネル")]
        private GameObject confirmPanel;

        [SerializeField]
        [Tooltip("終了ボタン（メイン画面）")]
        private Button exitButton;

        [SerializeField]
        [Tooltip("確認ダイアログの「終了」ボタン")]
        private Button confirmExitButton;

        [SerializeField]
        [Tooltip("確認ダイアログの「戻る」ボタン")]
        private Button cancelButton;

        [Header("オプション")]
        [SerializeField]
        [Tooltip("Androidの戻るボタンでダイアログを表示")]
        private bool handleAndroidBackButton = true;

        private float _savedTimeScale = 1f;
        private bool _isDialogOpen = false;

        private void Start()
        {
            // 初期状態でダイアログを非表示
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(false);
            }

            // 終了ボタン（メイン画面）
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(ShowConfirmDialog);
            }

            // 確認ダイアログの「終了」ボタン
            if (confirmExitButton != null)
            {
                confirmExitButton.onClick.AddListener(QuitApplication);
            }

            // 確認ダイアログの「戻る」ボタン
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(HideConfirmDialog);
            }
        }

        private void Update()
        {
            // 新Input Systemを使用してEscキー（Androidの戻るボタン）を検出
            if (handleAndroidBackButton)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                {
                    if (_isDialogOpen)
                    {
                        HideConfirmDialog();
                    }
                    else
                    {
                        ShowConfirmDialog();
                    }
                }
            }
        }

        /// <summary>
        /// 確認ダイアログを表示し、ゲームを一時停止
        /// </summary>
        public void ShowConfirmDialog()
        {
            if (_isDialogOpen) return;

            _isDialogOpen = true;

            // 現在のTimeScaleを保存して一時停止
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            // ダイアログを表示
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(true);
            }

            Debug.Log("[ExitConfirmDialog] Dialog shown, game paused");
        }

        /// <summary>
        /// 確認ダイアログを閉じ、ゲームを再開
        /// </summary>
        public void HideConfirmDialog()
        {
            if (!_isDialogOpen) return;

            _isDialogOpen = false;

            // ダイアログを非表示
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(false);
            }

            // TimeScaleを復元
            Time.timeScale = _savedTimeScale;

            Debug.Log("[ExitConfirmDialog] Dialog hidden, game resumed");
        }

        /// <summary>
        /// アプリケーションを終了
        /// </summary>
        public void QuitApplication()
        {
            Debug.Log("[ExitConfirmDialog] Quitting application...");

            // TimeScaleを戻してから終了
            Time.timeScale = 1f;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(ShowConfirmDialog);
            }
            if (confirmExitButton != null)
            {
                confirmExitButton.onClick.RemoveListener(QuitApplication);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HideConfirmDialog);
            }

            // TimeScaleを確実に復元
            if (_isDialogOpen)
            {
                Time.timeScale = _savedTimeScale;
            }
        }
    }
}
