using UnityEngine;

namespace Tetris.Core
{
    /// <summary>
    /// グリッド描画クラス - 正確なマス目を描画
    /// 
    /// 【座標システム】
    /// - セル(0,0)は位置(0,0)を中心とする1x1の正方形
    /// - セル(0,0)の範囲: x=-0.5〜0.5, y=-0.5〜0.5
    /// - グリッド全体(10x20): x=-0.5〜9.5, y=-0.5〜19.5
    /// </summary>
    public class GridBorder : MonoBehaviour
    {
        [Header("外観設定")]
        [SerializeField] private Color borderColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color gridLineColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private float borderWidth = 0.08f;
        [SerializeField] private float gridLineWidth = 0.02f;

        [Header("グリッドサイズ")]
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 20;

        private void Start()
        {
            DrawGrid();
        }

        private void DrawGrid()
        {
            // グリッドの境界座標
            float left = -0.5f;
            float right = gridWidth - 0.5f;   // 10 - 0.5 = 9.5
            float bottom = -0.5f;
            float top = gridHeight - 0.5f;    // 20 - 0.5 = 19.5

            // 外枠を描画
            DrawBorder(left, right, bottom, top);

            // 縦線を描画（セル間の境界）
            for (int x = 1; x < gridWidth; x++)
            {
                float xPos = x - 0.5f;  // x=1 → 0.5, x=2 → 1.5, ...
                DrawLine($"VLine_{x}", 
                    new Vector3(xPos, bottom, 0), 
                    new Vector3(xPos, top, 0), 
                    gridLineColor, gridLineWidth);
            }

            // 横線を描画（セル間の境界）
            for (int y = 1; y < gridHeight; y++)
            {
                float yPos = y - 0.5f;  // y=1 → 0.5, y=2 → 1.5, ...
                DrawLine($"HLine_{y}", 
                    new Vector3(left, yPos, 0), 
                    new Vector3(right, yPos, 0), 
                    gridLineColor, gridLineWidth);
            }
        }

        private void DrawBorder(float left, float right, float bottom, float top)
        {
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(transform);
            
            LineRenderer lr = borderObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = borderColor;
            lr.endColor = borderColor;
            lr.startWidth = borderWidth;
            lr.endWidth = borderWidth;
            lr.loop = true;
            lr.positionCount = 4;
            lr.sortingOrder = -1;
            lr.useWorldSpace = true;

            lr.SetPosition(0, new Vector3(left, bottom, 0));
            lr.SetPosition(1, new Vector3(right, bottom, 0));
            lr.SetPosition(2, new Vector3(right, top, 0));
            lr.SetPosition(3, new Vector3(left, top, 0));
        }

        private void DrawLine(string name, Vector3 start, Vector3 end, Color color, float width)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.SetParent(transform);
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.positionCount = 2;
            lr.sortingOrder = -1;
            lr.useWorldSpace = true;

            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }
    }
}
