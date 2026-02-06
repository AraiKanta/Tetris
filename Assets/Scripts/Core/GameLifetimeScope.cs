using UnityEngine;
using VContainer;
using VContainer.Unity;
using Tetris.Models;
using Tetris.Views;
using Tetris.Presenters;

namespace Tetris.Core
{
    // ============================================================
    // MVP設計: DI設定（VContainer LifetimeScope）
    // ============================================================
    //
    // 【GameLifetimeScopeの責務】
    // - VContainerのDI設定を行う
    // - Model、View、Presenterをコンテナに登録する
    // - 依存関係を解決してインスタンスを生成する
    //
    // 【MVPとDIの関係】
    // - DIを使うことでMVP各層の依存関係を疎結合に保てる
    // - Presenterはインターフェース（IGameView）に依存するため、
    //   具体的なView実装を差し替え可能
    // - テスト時にモックをインジェクション可能
    //
    // ============================================================

    /// <summary>
    /// ゲームのDI設定を行うLifetimeScope
    /// 
    /// VContainerの機能を使って、
    /// MVP各層のインスタンスを生成・管理する
    /// </summary>
    public class GameLifetimeScope : LifetimeScope
    {
        // ============================================================
        // インスペクタ設定
        // ============================================================

        [Header("テトロミノ設定")]
        [SerializeField]
        [Tooltip("生成可能なテトロミノPrefabのリスト（7種類）")]
        private GameObject[] tetrominoPrefabs;

        [SerializeField]
        [Tooltip("テトロミノのスポーン位置")]
        private Vector3 spawnPosition = new Vector3(5, 19, 0);

        [Header("View参照")]
        [SerializeField]
        [Tooltip("GameViewコンポーネントを持つGameObject")]
        private GameView gameView;

        // ============================================================
        // DI設定
        // ============================================================

        /// <summary>
        /// 依存性の登録を行う
        /// 
        /// VContainerに各クラスを登録し、
        /// 依存関係を自動解決できるようにする
        /// </summary>
        protected override void Configure(IContainerBuilder builder)
        {
            // ----------------------------------------
            // Model層の登録
            // ----------------------------------------
            // GameModelをシングルトンとして登録
            // ゲーム中は1つのインスタンスを共有
            builder.Register<GameModel>(Lifetime.Singleton);

            // ----------------------------------------
            // View層の登録
            // ----------------------------------------
            // GameViewをIGameViewインターフェースとして登録
            // これによりPresenterはIGameViewに依存し、
            // 具体的なGameView実装に依存しない
            builder.RegisterComponent(gameView).As<IGameView>();

            // ----------------------------------------
            // Presenter層の登録
            // ----------------------------------------
            // GamePresenterをシングルトン+エントリーポイントとして登録
            // - AsSelf(): 動的生成されるTetrominoからも解決可能
            // - AsImplementedInterfaces(): IStartable実装によりStart()が自動呼び出し
            builder.Register<GamePresenter>(Lifetime.Singleton)
                .AsSelf()
                .AsImplementedInterfaces();

            // ----------------------------------------
            // その他のコンポーネント
            // ----------------------------------------
            // Spawner（シーン内のコンポーネントを登録）
            builder.RegisterComponentInHierarchy<Spawner>();

            // スポーン設定を値オブジェクトとして登録
            builder.RegisterInstance(new SpawnSettings
            {
                TetrominoPrefabs = tetrominoPrefabs,
                SpawnPosition = spawnPosition
            });
        }
    }

    /// <summary>
    /// スポーン設定を保持する値オブジェクト
    /// 
    /// ImmutableなデータをDI経由で渡すためのクラス
    /// </summary>
    public class SpawnSettings
    {
        /// <summary>テトロミノPrefabのリスト</summary>
        public GameObject[] TetrominoPrefabs { get; set; }

        /// <summary>スポーン位置</summary>
        public Vector3 SpawnPosition { get; set; }
    }
}
