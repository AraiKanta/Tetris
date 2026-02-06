using UnityEngine;

namespace Tetris.Core
{
    /// <summary>
    /// カメラ自動フィット - グリッドを画面に収める
    /// 
    /// 【座標システム】
    /// - グリッド: x=-0.5〜9.5, y=-0.5〜19.5 (10x20マス)
    /// - 中心: (4.5, 9.5)
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraAutoFit : MonoBehaviour
    {
        [Header("グリッド設定")]
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 20;

        [Header("余白設定")]
        [SerializeField] private float topPadding = 2f;
        [SerializeField] private float bottomPadding = 0.5f;
        [SerializeField] private float sidePadding = 0.5f;

        private Camera _camera;
        private int _lastWidth;
        private int _lastHeight;

        private void Start()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            FitCamera();
        }

        private void LateUpdate()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
            {
                FitCamera();
            }
        }

        private void FitCamera()
        {
            if (_camera == null) return;

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            if (_lastWidth <= 0 || _lastHeight <= 0) return;

            // グリッドの実サイズ（-0.5〜9.5 = 10ユニット幅）
            float gridW = gridWidth;   // 10
            float gridH = gridHeight;  // 20

            // 余白込みの表示領域
            float viewWidth = gridW + sidePadding * 2;      // 10 + 1 = 11
            float viewHeight = gridH + topPadding + bottomPadding;  // 20 + 2.5 = 22.5

            // グリッドの中心（セル4.5が中心）
            // X: 0〜9の中心 = 4.5
            // Y: 余白を考慮して調整
            float centerX = (gridWidth - 1) / 2f;  // 4.5
            float centerY = (gridHeight - 1) / 2f + (topPadding - bottomPadding) / 2f;

            // 画面アスペクト比
            float screenAspect = (float)_lastWidth / _lastHeight;
            float viewAspect = viewWidth / viewHeight;

            float orthoSize;
            if (screenAspect >= viewAspect)
            {
                // 横長画面 → 高さにフィット
                orthoSize = viewHeight / 2f;
            }
            else
            {
                // 縦長画面 → 幅にフィット
                orthoSize = viewWidth / screenAspect / 2f;
            }

            _camera.orthographicSize = orthoSize;
        }
    }
}
