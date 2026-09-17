using UnityEngine;

namespace EchoFactory.Runtime
{
    /// <summary>Lightweight industrial blueprint backdrop. It adds visual identity without requiring art assets.</summary>
    public sealed class IndustrialBackdrop : MonoBehaviour
    {
        private const float W = 1440f;
        private const float H = 900f;
        private GUIStyle label;
        private Texture2D pixel;
        private readonly Color paper = new Color(.91f, .92f, .90f);
        private readonly Color grid = new Color(.82f, .84f, .81f);
        private readonly Color line = new Color(.66f, .69f, .66f);
        private readonly Color accent = new Color(.08f, .42f, .43f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttach()
        {
            if (FindObjectOfType<IndustrialBackdrop>() != null) return;
            var go = new GameObject("EchoFactory Industrial Backdrop");
            DontDestroyOnLoad(go);
            go.AddComponent<IndustrialBackdrop>();
        }

        private void Awake()
        {
            pixel = Texture2D.whiteTexture;
            label = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            label.normal.textColor = new Color(.48f, .51f, .49f);
        }

        private void OnGUI()
        {
            GUI.depth = 100;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / W, Screen.height / H, 1));
            var old = GUI.color;
            GUI.color = paper;
            GUI.DrawTexture(new Rect(0, 0, W, H), pixel);
            GUI.color = grid;

            for (int x = 0; x <= 1440; x += 60) GUI.DrawTexture(new Rect(x, 0, 1, H), pixel);
            for (int y = 0; y <= 900; y += 60) GUI.DrawTexture(new Rect(0, y, W, 1), pixel);

            // A subtle logistics network sits behind the strategic cards.
            GUI.color = line;
            DrawLine(30, 700, 690, 700, 3);
            DrawLine(690, 700, 690, 160, 3);
            DrawLine(690, 160, 1350, 160, 3);
            DrawLine(105, 745, 580, 745, 2);
            DrawLine(580, 745, 580, 285, 2);

            GUI.color = accent;
            DrawNode(690, 700, 8);
            DrawNode(690, 160, 8);
            DrawNode(580, 745, 6);
            DrawNode(580, 285, 6);

            GUI.color = Color.white;
            GUI.Label(new Rect(1050, 760, 300, 30), "INDUSTRIAL NETWORK // SECTOR 01", label);
            GUI.color = old;
        }

        private void DrawLine(float x1, float y1, float x2, float y2, float width)
        {
            Matrix4x4 matrix = GUI.matrix;
            float angle = Mathf.Atan2(y2 - y1, x2 - x1) * Mathf.Rad2Deg;
            float length = Vector2.Distance(new Vector2(x1, y1), new Vector2(x2, y2));
            GUIUtility.RotateAroundPivot(angle, new Vector2(x1, y1));
            GUI.DrawTexture(new Rect(x1, y1 - width * .5f, length, width), pixel);
            GUI.matrix = matrix;
        }

        private void DrawNode(float x, float y, float size)
        {
            GUI.DrawTexture(new Rect(x - size * .5f, y - size * .5f, size, size), pixel);
        }
    }
}
