#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    public partial class GitFolderSynchronizer
    {
        public static Action Close;

        public static List<string> ChangedFiles = new();

        [MenuItem("Assets/Git Commit and Push", true)]
        public static bool ValidateGitFolderSynchronizer()
        {
            string path = GetSelectedPath();
            return !string.IsNullOrEmpty(path) && Directory.Exists(Path.Combine(path, ".git"))
                && HasUncommittedChanges(path);
        }

        [MenuItem("Assets/Git Commit and Push", priority = -100)]
        public static void ShowWindow()
        {
            string path = GetSelectedPath();
            if (!string.IsNullOrEmpty(path))
            {
                ChangedFiles = GetChangedFiles(path);
                EditorWindowDrawer
                    .CreateInstance("Git Commit and Push Window", new(420, 300))
                    .SetHeader(Header)
                    .SetBody(Body, EditorWindowStyle.Margin)
                    .SetFooter(Footer, EditorWindowStyle.HelpBox)
                    .GetCloseEvent(out Close)
                    .ShowAsUtility();
            }
        }

        public static void Header()
        {
            string path = GetSelectedPath();
            EditorGUILayout.HelpBox("Repository: " + path, MessageType.Info);
        }

        public static void Body()
        {
            GUILayout.Label("Changed Files:", EditorStyles.boldLabel);

            if (ChangedFiles.Count == 0)
                EditorGUILayout.LabelField("No uncommitted changes detected.");

            foreach (string file in ChangedFiles)
                EditorGUILayout.LabelField(file);

            GUILayout.FlexibleSpace();

            GUILayout.Label($"Total Changes: {ChangedFiles.Count}", EditorStyles.miniBoldLabel);
        }

        public static void Footer()
        {
            string path = GetSelectedPath();
            string commitMessage = string.Empty;

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Commit Message:", EditorStyles.label, GUILayout.Width(110));
                commitMessage = EditorGUILayout.TextField(commitMessage);
            }

            GUI.enabled = ChangedFiles.Count > 0;
            if (GUILayout.Button("Commit and Push", GUILayout.Height(24)))
            {
                Commit(path, commitMessage);
                Push();
                Fetch();
                GenerateChangelog(path);
                AssetDatabase.Refresh();
                Close();
            }
            GUI.enabled = true;

            GUILayout.Space(6);
            if (GUILayout.Button("Generate CHANGELOG.txt", GUILayout.Height(22)))
            {
                GenerateChangelog(path);
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Assets/Git Generate CHANGELOG.txt", true)]
        public static bool ValidateGenerateChangelog()
        {
            string path = GetSelectedPath();
            return !string.IsNullOrEmpty(path) && Directory.Exists(Path.Combine(path, ".git"));
        }

        [MenuItem("Assets/Git Generate CHANGELOG.txt", priority = -98)]
        public static void MenuGenerateChangelog()
        {
            string path = GetSelectedPath();
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[Git] No repository selected.");
                return;
            }
            GenerateChangelog(path);
            AssetDatabase.Refresh();
        }
    }
}
#endif