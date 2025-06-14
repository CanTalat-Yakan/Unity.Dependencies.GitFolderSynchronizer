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
        public static bool ValidateGitCommit()
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
                new EditorWindowDrawer("Git Commit and Push Window", new(420, 300))
                    .SetHeader(Header)
                    .SetBody(Body)
                    .SetFooter(Footer)
                    .GetCloseEvent(out Close)
                    .ShowUtility();
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
            else foreach (string file in ChangedFiles)
                    EditorGUILayout.LabelField(file);
        }

        public static void Footer()
        {
            string path = GetSelectedPath();
            string commitMessage = string.Empty;

            GUILayout.Label($"Total Changes: {ChangedFiles.Count}", EditorStyles.miniBoldLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Commit Message:", EditorStyles.label, GUILayout.Width(110));
            commitMessage = EditorGUILayout.TextField(commitMessage);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUI.enabled = ChangedFiles.Count > 0;
            if (GUILayout.Button("Commit and Push", GUILayout.Height(24)))
            {
                Commit(path, commitMessage);
                Push();
                Fetch();
                Close();
            }
            GUI.enabled = true;
        }
    }
}
#endif