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
        
        private static string _commitMessage = string.Empty;

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
                var window = EditorWindowDrawer
                    .CreateInstance("Git Commit and Push Window", new(420, 300))
                    .SetHeader(Header, EditorWindowStyle.Toolbar)
                    .SetBody(Body, EditorWindowStyle.Margin)
                    .SetFooter(Footer, EditorWindowStyle.HelpBox)
                    .GetCloseEvent(out Close)
                    .ShowAsUtility();
            }
        }

        public static void Header()
        {
            GUILayout.Label("Commit Message:", EditorStyles.label, GUILayout.Width(110));
            _commitMessage = EditorGUILayout.TextField(_commitMessage, EditorStyles.toolbarTextField);

            if (GUILayout.Button("Fetch and Pull", EditorStyles.toolbarButton))
                FetchPull();
        }

        public static void Body()
        {
            GUILayout.Label("Changed Files:", EditorStyles.boldLabel);

            if (ChangedFiles.Count == 0)
                EditorGUILayout.LabelField("No uncommitted changes detected.");

            foreach (string file in ChangedFiles)
                EditorGUILayout.LabelField(file);
        }

        public static void Footer()
        {
            string path = GetSelectedPath();

            GUILayout.Label($"Total Changes: {ChangedFiles.Count}", EditorStyles.miniBoldLabel);

            int assetsIndex = path.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
            EditorGUILayout.HelpBox("Git Repository Path: \n" + path.Substring(assetsIndex), MessageType.Info);

            GUI.enabled = ChangedFiles.Count > 0;
            if (GUILayout.Button("Commit and Push"))
            {
                Commit(path, _commitMessage);
                Push();
                Fetch();
                Close();
            }
            GUI.enabled = true;
        }
    }
}
#endif