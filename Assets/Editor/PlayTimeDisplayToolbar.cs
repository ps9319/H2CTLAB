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
            if (EditorApplication.isPlaying)
            {
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        private static void OnToolbarGUI()
        {
            GUILayout.FlexibleSpace();
            
            if (EditorApplication.isPlaying)
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(UnityEngine.Time.time);
                string timeString = $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}";
                GUILayout.Label(timeString, PlayTimeToolbarStyles.timeLabelStyle, GUILayout.ExpandHeight(true));
            }
        }
    }
}