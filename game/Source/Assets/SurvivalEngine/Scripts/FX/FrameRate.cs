using UnityEngine;

namespace SurvivalEngine
{
    public class FrameRate : MonoBehaviour
    {
        private float average_delta = 0f;
        private GUIStyle style;

        private void Start()
        {
            style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = Screen.height / 50;
            style.normal.textColor = new Color(0f, 0f, 0.4f, 1f);
        }

        void Update()
        {
            float diff = Time.unscaledDeltaTime - average_delta;
            average_delta += diff * 0.2f;
        }

        void OnGUI()
        {
            float miliseconds = average_delta * 1000f;
            float frame_rate = 1f / average_delta;
            string text = miliseconds.ToString("0.0") + " ms (" + frame_rate.ToString("0") + " fps)";

            Rect rect = new Rect(0, 0, Screen.width, Screen.height / 50);
            GUI.Label(rect, text, style);
        }
    }
}