using System;
using UnityEngine;
using R3;

namespace Tetris.Models
{
    // ============================================================
    // MVP設計: Model層
    // ============================================================
    // 
    // 【Modelの責務】
    // - ゲームの状態（データ）を保持する
    // - ビジネスロジック（ライン消去、スコア計算）を実行する
    // - 状態変化をReactivePropertyで通知する
    // - UIやUnityの機能に依存しない（純粋なC#クラス）
    //
    // 【MVPにおけるModelの役割】
    // - Presenterからのみ操作される
    // - Viewを直接知らない（疎結合）
    // - テスト可能な純粋なロジック
    //
    // ============================================================

    /// <summary>
    /// ゲーム全体の状態を管理するModel
    /// 
    /// スコア、消去ライン数、ゲームオーバー状態などの
    /// ゲームのコアデータを保持し、状態変化を通知する
    /// </summary>
    public class GameModel : IDisposable
    {
        // ============================================================
        // 定数
        // ============================================================

        /// <summary>グリッドの横幅（標準テトリスは10マス）</summary>
        public const int GRID_WIDTH = 10;

        /// <summary>グリッドの縦幅（標準テトリスは20マス）</summary>
        public const int GRID_HEIGHT = 20;

        /// <summary>1ライン消去時の基本スコア</summary>
        private const int SCORE_PER_LINE = 100;

        // ============================================================
        // 状態（ReactiveProperty）
        // ============================================================
        // ReactivePropertyを使用することで、状態変化を購読可能にする
        // これにより、Presenterが変化を検知してViewに反映できる

        /// <summary>現在のスコア（読み取り専用で公開）</summary>
        public ReadOnlyReactiveProperty<int> Score => _score;
        private readonly ReactiveProperty<int> _score = new(0);

        /// <summary>消去したライン数（読み取り専用で公開）</summary>
        public ReadOnlyReactiveProperty<int> LinesCleared => _linesCleared;
        private readonly ReactiveProperty<int> _linesCleared = new(0);

        /// <summary>ゲームがプレイ中かどうか（読み取り専用で公開）</summary>
        public ReadOnlyReactiveProperty<bool> IsPlaying => _isPlaying;
        private readonly ReactiveProperty<bool> _isPlaying = new(true);

        /// <summary>ゲームオーバー状態（読み取り専用で公開）</summary>
        public ReadOnlyReactiveProperty<bool> IsGameOver => _isGameOver;
        private readonly ReactiveProperty<bool> _isGameOver = new(false);

        // ============================================================
        // グリッドデータ
        // ============================================================
        // 2次元配列でブロックの配置を管理
        // nullの場合は空、Transformが入っている場合はブロックがある

        /// <summary>
        /// グリッド上のブロック配置
        /// grid[x, y] = そのセルにあるブロックのTransform（なければnull）
        /// </summary>
        private readonly Transform[,] _grid = new Transform[GRID_WIDTH, GRID_HEIGHT];

        // ============================================================
        // グリッド操作メソッド
        // ============================================================

        /// <summary>
        /// 指定座標のグリッドセルを取得
        /// </summary>
        /// <param name="x">X座標（0〜GRID_WIDTH-1）</param>
        /// <param name="y">Y座標（0〜GRID_HEIGHT-1）</param>
        /// <returns>セルにあるブロック、またはnull</returns>
        public Transform GetCell(int x, int y)
        {
            if (x < 0 || x >= GRID_WIDTH || y < 0 || y >= GRID_HEIGHT)
                return null;
            return _grid[x, y];
        }

        /// <summary>
        /// 指定座標にブロックを配置
        /// </summary>
        public void SetCell(int x, int y, Transform block)
        {
            if (x >= 0 && x < GRID_WIDTH && y >= 0 && y < GRID_HEIGHT)
            {
                _grid[x, y] = block;
            }
        }

        /// <summary>
        /// 指定座標が有効な範囲内かつ空いているかをチェック
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <returns>有効で空いていればtrue</returns>
        public bool IsValidCell(int x, int y)
        {
            // X座標が範囲内か
            if (x < 0 || x >= GRID_WIDTH)
                return false;

            // Y座標が下限より下か
            if (y < 0)
                return false;

            // Y座標が上限を超えている場合は許可（スポーン位置対応）
            if (y >= GRID_HEIGHT)
                return true;

            // セルが空いているか
            return _grid[x, y] == null;
        }

        /// <summary>
        /// 指定行が完全に埋まっているかをチェック
        /// </summary>
        /// <param name="y">チェックする行のY座標</param>
        /// <returns>行が完全に埋まっていればtrue</returns>
        public bool IsRowFull(int y)
        {
            if (y < 0 || y >= GRID_HEIGHT)
                return false;

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                if (_grid[x, y] == null)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 指定行のブロックを全て取得
        /// </summary>
        public Transform[] GetRowBlocks(int y)
        {
            Transform[] blocks = new Transform[GRID_WIDTH];
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                blocks[x] = _grid[x, y];
            }
            return blocks;
        }

        /// <summary>
        /// 指定行をクリア（nullで埋める）
        /// </summary>
        public void ClearRow(int y)
        {
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                _grid[x, y] = null;
            }
        }

        /// <summary>
        /// 指定行より上のブロックを1行下に移動（データのみ）
        /// </summary>
        public void ShiftRowsDown(int clearedY)
        {
            for (int y = clearedY; y < GRID_HEIGHT - 1; y++)
            {
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    _grid[x, y] = _grid[x, y + 1];
                }
            }

            // 最上行をクリア
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                _grid[x, GRID_HEIGHT - 1] = null;
            }
        }

        // ============================================================
        // スコア・ライン操作メソッド
        // ============================================================

        /// <summary>
        /// ライン消去時のスコア加算
        /// 同時消去ライン数に応じてボーナス
        /// </summary>
        /// <param name="lineCount">消去したライン数</param>
        public void AddLinesCleared(int lineCount)
        {
            if (lineCount <= 0) return;

            _linesCleared.Value += lineCount;

            // 同時消去ボーナス: 1ライン=100, 2=300, 3=500, 4=800
            int bonus = lineCount switch
            {
                1 => 100,
                2 => 300,
                3 => 500,
                4 => 800,
                _ => lineCount * 200
            };

            _score.Value += bonus;
        }

        // ============================================================
        // ゲーム状態操作メソッド
        // ============================================================

        /// <summary>
        /// ゲームオーバーを発生させる
        /// </summary>
        public void TriggerGameOver()
        {
            _isPlaying.Value = false;
            _isGameOver.Value = true;
        }

        /// <summary>
        /// ゲームをリセットして最初から開始
        /// </summary>
        public void Reset()
        {
            // グリッドをクリア
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    _grid[x, y] = null;
                }
            }

            // 状態をリセット
            _score.Value = 0;
            _linesCleared.Value = 0;
            _isGameOver.Value = false;
            _isPlaying.Value = true;
        }

        // ============================================================
        // IDisposable実装
        // ============================================================

        public void Dispose()
        {
            _score.Dispose();
            _linesCleared.Dispose();
            _isPlaying.Dispose();
            _isGameOver.Dispose();
        }
    }
}
