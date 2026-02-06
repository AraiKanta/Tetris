using System;
using R3;

namespace Tetris.Views
{
    // ============================================================
    // MVP設計: View層 - インターフェース定義
    // ============================================================
    //
    // 【Viewの責務】
    // - UIの表示を担当する（テキスト、ボタン、パネルなど）
    // - ユーザー入力イベントを発火する（ボタンクリックなど）
    // - ロジックを持たない（Passive View）
    // - Presenterからの指示に従って表示を更新する
    //
    // 【インターフェースを使う理由】
    // - PresenterがViewの具体的な実装に依存しない
    // - テスト時にモックViewを差し替え可能
    // - 異なるプラットフォームで別のView実装が可能
    //
    // ============================================================

    /// <summary>
    /// ゲームUIのViewインターフェース
    /// 
    /// Presenterはこのインターフェースを通じてViewを操作する
    /// これにより、UIの具体的な実装（UGUI、UIToolkitなど）に依存しない
    /// </summary>
    public interface IGameView
    {
        // ============================================================
        // 表示更新メソッド（Presenterから呼び出される）
        // ============================================================
        // これらのメソッドは、Presenterから一方向に呼び出される
        // ViewはPresenterに値を返さない（Passive View）

        /// <summary>
        /// スコア表示を更新する
        /// </summary>
        /// <param name="score">表示するスコア</param>
        void UpdateScore(int score);

        /// <summary>
        /// 消去ライン数の表示を更新する
        /// </summary>
        /// <param name="lines">表示するライン数</param>
        void UpdateLines(int lines);

        /// <summary>
        /// ゲームオーバーパネルの表示/非表示を切り替える
        /// </summary>
        /// <param name="show">trueで表示、falseで非表示</param>
        void ShowGameOverPanel(bool show);

        // ============================================================
        // ユーザー入力イベント（Viewから発火される）
        // ============================================================
        // これらのObservableは、ユーザーがUIを操作した時にイベントを発行する
        // Presenterがこれらを購読して、適切なアクションを実行する

        /// <summary>
        /// リトライボタンがクリックされた時のイベント
        /// </summary>
        Observable<Unit> OnRetryClicked { get; }
    }

    /// <summary>
    /// テトロミノ入力のViewインターフェース
    /// 
    /// キーボードやタッチ入力をイベントとして通知する
    /// </summary>
    public interface IInputView
    {
        // ============================================================
        // 入力イベント（Presenterが購読する）
        // ============================================================

        /// <summary>左移動の入力イベント</summary>
        Observable<Unit> OnMoveLeft { get; }

        /// <summary>右移動の入力イベント</summary>
        Observable<Unit> OnMoveRight { get; }

        /// <summary>回転の入力イベント</summary>
        Observable<Unit> OnRotate { get; }

        /// <summary>ハードドロップの入力イベント</summary>
        Observable<Unit> OnHardDrop { get; }

        /// <summary>ソフトドロップ状態（押されている間true）</summary>
        Observable<bool> OnSoftDrop { get; }
    }
}
