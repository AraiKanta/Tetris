using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using R3;

namespace Tetris.Views
{
    // ============================================================
    // MVP設計: View層 - 具体的な実装
    // ============================================================
    //
    // 【GameViewの責務】
    // - IGameViewインターフェースを実装する
    // - Unity UIコンポーネント（Text, Button, Panel）を管理する
    // - Presenterからの指示に従ってUIを更新する
    // - ユーザー入力（ボタンクリック）をイベントとして発火する
    // - ロジックを一切持たない（Passive View）
    //
    // 【重要なポイント】
    // - このクラスはMonoBehaviourを継承してUnityと連携
    // - しかし、ゲームロジックは含まない
    // - 「どう表示するか」だけを担当する
    //
    // ============================================================

    /// <summary>
    /// ゲームUIのView実装
    /// 
    /// IGameViewインターフェースを実装し、
    /// UnityのUIコンポーネントを使って実際の表示を行う
    /// </summary>
    public class GameView : MonoBehaviour, IGameView
    {
        // ============================================================
        // インスペクタ設定（UIコンポーネントの参照）
        // ============================================================
        // これらはUnityエディタからアサインする
        // Viewはこれらのコンポーネントを「持っている」だけで、
        // いつ・何を表示するかはPresenterが決定する

        [Header("スコア表示")]
        [SerializeField]
        [Tooltip("現在のスコアを表示するテキスト")]
        private TextMeshProUGUI scoreText;

        [SerializeField]
        [Tooltip("消去ライン数を表示するテキスト")]
        private TextMeshProUGUI linesText;

        [Header("ゲームオーバーUI")]
        [SerializeField]
        [Tooltip("ゲームオーバー時に表示するパネル")]
        private GameObject gameOverPanel;

        [SerializeField]
        [Tooltip("リトライボタン")]
        private Button retryButton;

        // ============================================================
        // イベント（IGameView実装）
        // ============================================================
        // ユーザー入力をイベントとして公開
        // Presenterがこれを購読してアクションを実行する

        /// <summary>リトライボタンクリックイベント</summary>
        public Observable<Unit> OnRetryClicked => _onRetryClicked;
        private readonly Subject<Unit> _onRetryClicked = new();

        // ============================================================
        // Unity ライフサイクル
        // ============================================================

        private void Awake()
        {
            // 初期化: ゲームオーバーパネルを非表示
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            // リトライボタンのクリックをイベントに変換
            // これによりPresenterがボタンの存在を知らずにイベントを購読できる
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(() => 
                {
                    _onRetryClicked.OnNext(Unit.Default);
                });
            }
        }

        private void OnDestroy()
        {
            _onRetryClicked.Dispose();
        }

        // ============================================================
        // IGameView 実装 - 表示更新メソッド
        // ============================================================
        // これらのメソッドはPresenterから呼び出される
        // ViewはただPresenterの指示に従って表示を更新するだけ

        /// <summary>
        /// スコア表示を更新
        /// 
        /// Presenterが「スコアをXXXに更新して」と指示する
        /// Viewは指示された値をそのまま表示する
        /// </summary>
        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }

        /// <summary>
        /// 消去ライン数の表示を更新
        /// </summary>
        public void UpdateLines(int lines)
        {
            if (linesText != null)
            {
                linesText.text = $"Lines: {lines}";
            }
        }

        /// <summary>
        /// ゲームオーバーパネルの表示/非表示を切り替え
        /// 
        /// Presenterが「ゲームオーバーパネルを表示して」と指示する
        /// Viewは指示に従ってパネルを表示/非表示にする
        /// </summary>
        public void ShowGameOverPanel(bool show)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(show);
            }
        }
    }
}
