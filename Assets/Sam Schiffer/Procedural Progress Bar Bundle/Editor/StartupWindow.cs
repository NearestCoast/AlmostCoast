using UnityEngine;
using UnityEditor;

namespace Renge.PBBundle {
    [InitializeOnLoad]
    public class StartupWindow : EditorWindow {
        private bool m_ShowHelpOnStartup = true;
        private static Texture2D s_CoverImage;
        private static string s_CoverImagePath = string.Empty;
        private static string s_ReadmePath = string.Empty;

        const string k_HelpWindowShownKey = "rg-bundle-helpWindowShown";
        const string k_ShowHelpOnStartupKey = "rg-bundle-showHelpOnStartup";

        static StartupWindow() {
            EditorApplication.update += OnStartup;
            EditorApplication.quitting += OnQuitting;
        }

        private static void OnStartup() {
            if (!Application.isPlaying) {
                bool shouldShow = EditorPrefs.GetInt(k_ShowHelpOnStartupKey, 1) > 0;
                bool helpWindowShown = EditorPrefs.GetInt(k_HelpWindowShownKey, 0) > 0;

                if (!shouldShow || helpWindowShown)
                    return;

                EditorPrefs.SetInt(k_HelpWindowShownKey, 1);
                Init();
            }
            EditorApplication.update -= OnStartup;
        }

        private static void OnQuitting() {
            EditorPrefs.SetInt(k_HelpWindowShownKey, 0);
            EditorApplication.quitting -= OnQuitting;
        }

        [MenuItem("Window/Procedural Progress Bar Bundle/Help", false)]
        private static void Init() {
            StartupWindow window = (StartupWindow)GetWindow(typeof(StartupWindow), true, "Procedural Progress Bar Bundle");
            window.minSize = new Vector2(700, 420);
            window.maxSize = new Vector2(700, 420);
            window.Show();
        }

        private void OnGUI() {
            using (new GUILayout.HorizontalScope()) {
                if (!s_CoverImage) {
                    if (s_CoverImagePath == string.Empty) {
                        var paths = AssetDatabase.FindAssets("bundle-image-01 t:texture2d");
                        if (paths.Length != 0)
                            s_CoverImagePath = AssetDatabase.GUIDToAssetPath(paths[0]);
                    }
                    s_CoverImage = AssetDatabase.LoadAssetAtPath<Texture2D>(s_CoverImagePath);
                }
                if (s_CoverImage) {
                    GUI.DrawTexture(new Rect(0, 0, 240, 420), s_CoverImage, ScaleMode.StretchToFill, true, 0);
                    GUILayout.Space(240);
                }

                using (new GUILayout.VerticalScope()) {
                    var headerStyle = new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
                    var secondHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
                    var textStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, margin = new RectOffset(25, 25, 0, 0), wordWrap = true };
                    
                    GUILayout.Space(32);
                    GUILayout.Label("The Procedural Progress Bar Bundle", headerStyle);

                    GUILayout.Space(32);

                    GUILayout.Label("Thank you for purchasing the Procedural Progress Bar Bundle by Sam Schiffer!", secondHeaderStyle);
                    GUILayout.Space(10);
                    GUILayout.Label("You now have access to the following assets. Add them to your library and import them to get started!", textStyle);

                    GUILayout.FlexibleSpace();

                    int buttonWidth = 200;
                    int buttonHeight = 30;
                    using (new GUILayout.HorizontalScope()) {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Procedural Progress Bars", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight))) {
                            Application.OpenURL("https://u3d.as/2XdK");
                        }
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Circular Progress Bars", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight))) {
                            Application.OpenURL("https://u3d.as/2jpc");
                        }
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.FlexibleSpace();

                    using (new GUILayout.HorizontalScope()) {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Support Discord", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight))) {
                            Application.OpenURL("https://discord.gg/ewAueXSZ3V");
                        }
                        GUILayout.FlexibleSpace();
                    }

                    using (new GUILayout.HorizontalScope()) {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(new GUIContent() { text = "Support E-mail", tooltip = "support@rengegames.com"}, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight))) {
                            Application.OpenURL("mailto:support@rengegames.com");
                        }
                        GUILayout.FlexibleSpace();
                    }


                    GUILayout.FlexibleSpace();

                    EditorGUILayout.HelpBox("You can reopen this window any time in 'Window > Procedural Progress Bar Bundle > Help'", MessageType.Info);
                    GUILayout.Space(10);
                    using (new GUILayout.HorizontalScope()) {
                        EditorGUILayout.LabelField("PB Bundle 1.0.0", EditorStyles.boldLabel);

                        m_ShowHelpOnStartup = EditorPrefs.GetInt(k_ShowHelpOnStartupKey, 1) > 0;
                        m_ShowHelpOnStartup = EditorGUILayout.ToggleLeft("Show this window at startup", m_ShowHelpOnStartup);
                        EditorPrefs.SetInt(k_ShowHelpOnStartupKey, m_ShowHelpOnStartup ? 1 : 0);
                    }

                }
            }
        }
    }
}