using UnityEditor;
using UnityEngine;
using System;

namespace UnityToolbarExtender.Time
{
    static class PlayTimeToolbarStyles
    {
        public static readonly GUIStyle timeLabelStyle;

        static PlayTimeToolbarStyles()
        {
            timeLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }

    [InitializeOnLoad]
    public class PlayTimeDisplayToolbar
    {
        static PlayTimeDisplayToolbar()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
            ToolbarExtender.RightToolbarGUI.Add(OnRightToolbarGUI);
            EditorApplication.update += UpdateTimer;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.update += UpdateTimer;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.update -= UpdateTimer;
            }
        }

        private static void UpdateTimer()
        {
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static float timeScaleSliderValue = 1f;

        private static void OnToolbarGUI()
        {
            GUILayout.FlexibleSpace();

            // 슬라이더로 Time.timeScale 조작
            GUILayout.Label("TimeScale", PlayTimeToolbarStyles.timeLabelStyle, GUILayout.ExpandHeight(true));
            float newValue = GUILayout.HorizontalSlider(timeScaleSliderValue, 0f, 3f, GUILayout.Width(80));
            if (Math.Abs(newValue - timeScaleSliderValue) > 0.001f)
            {
                timeScaleSliderValue = newValue;
                UnityEngine.Time.timeScale = timeScaleSliderValue;
            }

            string timeScaleString = $"{UnityEngine.Time.timeScale:F2}x";
            GUILayout.Label(timeScaleString, PlayTimeToolbarStyles.timeLabelStyle, GUILayout.ExpandHeight(true));
        }

        private static void OnRightToolbarGUI()
        {
            string timeString;
            if (!EditorApplication.isPlaying)
            {
                timeString = "00:00";
            }
            else
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(UnityEngine.Time.time);
                timeString = $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}";
            }
            GUILayout.Label(timeString, PlayTimeToolbarStyles.timeLabelStyle, GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
        }

        // 단축키: Ctrl+Q → 1x <-> 2x 토글
        [MenuItem("Tools/TimeScale/2x Toggle %q")]
        private static void ToggleDoubleTimeScale()
        {
            if (UnityEngine.Time.timeScale >= 2.0f)
            {
                UnityEngine.Time.timeScale = 1.0f;
            }
            else
            {
                UnityEngine.Time.timeScale = 2.0f;
            }
            timeScaleSliderValue = UnityEngine.Time.timeScale;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        // 단축키: Ctrl+W → 1x <-> 0.5x 토글
        [MenuItem("Tools/TimeScale/0.5x Toggle %w")]
        private static void ToggleHalfTimeScale()
        {
            if (UnityEngine.Time.timeScale <= 0.5f)
            {
                UnityEngine.Time.timeScale = 1.0f;
            }
            else
            {
                UnityEngine.Time.timeScale = 0.5f;
            }
            timeScaleSliderValue = UnityEngine.Time.timeScale;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}