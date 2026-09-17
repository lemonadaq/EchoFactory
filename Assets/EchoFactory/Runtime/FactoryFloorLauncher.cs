using System.Reflection;
using EchoFactory.Core;
using UnityEngine;

namespace EchoFactory.Runtime
{
    /// <summary>Small bridge between the existing strategic UI and the asset-free factory floor view.</summary>
    public sealed class FactoryFloorLauncher : MonoBehaviour
    {
        private const float W = 1440f, H = 900f;
        private StrategicGameUI strategicUI;
        private FactoryFloorView floor;
        private GUIStyle button;
        private FieldInfo stateField;
        private FieldInfo screenField;
        private FieldInfo selectedFacilityField;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttach()
        {
            if (FindObjectOfType<FactoryFloorLauncher>() != null) return;
            var go = new GameObject("EchoFactory Factory Floor Launcher");
            DontDestroyOnLoad(go);
            go.AddComponent<FactoryFloorLauncher>();
        }

        private void Awake()
        {
            strategicUI = FindObjectOfType<StrategicGameUI>();
            floor = FindObjectOfType<FactoryFloorView>();
            if (floor == null)
            {
                var go = new GameObject("EchoFactory Factory Floor");
                DontDestroyOnLoad(go);
                floor = go.AddComponent<FactoryFloorView>();
            }
            if (strategicUI != null)
            {
                var type = typeof(StrategicGameUI);
                stateField = type.GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);
                screenField = type.GetField("screen", BindingFlags.Instance | BindingFlags.NonPublic);
                selectedFacilityField = type.GetField("selectedFacility", BindingFlags.Instance | BindingFlags.NonPublic);
            }
        }

        private void OnGUI()
        {
            if (strategicUI == null || floor == null || stateField == null || screenField == null || selectedFacilityField == null) return;
            if (button == null) button = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold };
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / W, Screen.height / H, 1));

            StrategicState state = stateField.GetValue(strategicUI) as StrategicState;
            object screen = screenField.GetValue(strategicUI);
            int facilityId = (int)selectedFacilityField.GetValue(strategicUI);
            if (state == null || screen == null || screen.ToString() != "Facility" || facilityId < 0) return;

            if (GUI.Button(new Rect(1120, 118, 275, 42), "WIDOK HALI  →", button))
                floor.Show(state, facilityId);
        }
    }
}
