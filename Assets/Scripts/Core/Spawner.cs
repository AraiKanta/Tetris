using UnityEngine;
using VContainer;
using VContainer.Unity;
using Tetris.Models;

namespace Tetris.Core
{
    // ============================================================
    // MVP設計: Spawnerの役割
    // ============================================================
    //
    // 【Spawnerの責務】
    // - テトロミノのインスタンス化を担当
    // - ランダムなテトロミノを選択して生成する
    // - 生成されたテトロミノにDIを適用する
    //
    // 【循環依存の回避】
    // GamePresenter → Spawner → GamePresenter は循環依存になる
    // そのため、SpawnerはGameModelのみを参照し、
    // GamePresenterには依存しない設計にする
    //
    // ============================================================

    /// <summary>
    /// テトロミノを生成するクラス
    /// 
    /// VContainerのIObjectResolverを使用して、
    /// 生成されたテトロミノに依存性を注入する
    /// </summary>
    public class Spawner : MonoBehaviour
    {
        // ============================================================
        // 依存性（VContainerで注入）
        // ============================================================

        /// <summary>スポーン設定（Prefabリストとスポーン位置）</summary>
        private SpawnSettings _settings;

        /// <summary>DIを適用するためのResolver</summary>
        private IObjectResolver _resolver;

        /// <summary>ゲームモデル（プレイ状態チェック用）</summary>
        private GameModel _model;

        /// <summary>
        /// 依存性注入メソッド
        /// 
        /// VContainerが自動的に呼び出す
        /// ※GamePresenterは循環依存を避けるため注入しない
        /// </summary>
        [Inject]
        public void Construct(
            SpawnSettings settings, 
            IObjectResolver resolver,
            GameModel model)
        {
            _settings = settings;
            _resolver = resolver;
            _model = model;
        }

        // ============================================================
        // スポーン処理
        // ============================================================

        /// <summary>
        /// 次のテトロミノを生成
        /// 
        /// ランダムに選択したPrefabをインスタンス化し、
        /// VContainerを通じてDIを適用する
        /// </summary>
        public void SpawnNext()
        {
            // ゲームオーバー中は生成しない（Modelを直接チェック）
            if (_model != null && !_model.IsPlaying.CurrentValue)
            {
                return;
            }

            // Prefabリストの検証
            if (_settings?.TetrominoPrefabs == null || 
                _settings.TetrominoPrefabs.Length == 0)
            {
                Debug.LogError("[Spawner] テトロミノPrefabが設定されていません！");
                return;
            }

            // ランダムにテトロミノを選択
            int randomIndex = Random.Range(0, _settings.TetrominoPrefabs.Length);
            GameObject prefab = _settings.TetrominoPrefabs[randomIndex];

            if (prefab == null)
            {
                Debug.LogError($"[Spawner] Prefab[{randomIndex}]がnullです！");
                return;
            }

            // VContainerを使ってインスタンス化
            // これにより、生成されたTetrominoにも依存性が注入される
            _resolver.Instantiate(prefab, _settings.SpawnPosition, Quaternion.identity);
        }
    }
}
