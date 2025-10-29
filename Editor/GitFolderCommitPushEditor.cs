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
                var window = EditorWindowDrawer
                    .CreateInstance("Git Commit and Push Window", new(420, 300))
                    .SetHeader(Header, EditorWindowStyle.Toolbar)
                    .SetBody(Body)
                    .SetFooter(Footer)
                    .GetCloseEvent(out Close)
                    .ShowAsUtility();
            }
        }

        public static void Header()
        {
            string path = GetSelectedPath();
            string commitMessage = string.Empty;

            GUILayout.Label("Commit Message:", EditorStyles.label, GUILayout.Width(110));
            commitMessage = EditorGUILayout.TextField(commitMessage, EditorStyles.toolbarTextField);

            GUI.enabled = ChangedFiles.Count > 0;
            if (GUILayout.Button("Commit and Push", EditorStyles.toolbarButton))
            {
                Commit(path, commitMessage);
                Push();
                Fetch();
                Close();
            }

            GUI.enabled = true;
        }

        public static void Body()
        {
            string path = GetSelectedPath();
            
            if (!string.IsNullOrEmpty(path))
            {
                int assetsIndex = path.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
                if (assetsIndex >= 0)
                    path = path.Substring(assetsIndex);
            }
            
            EditorGUILayout.HelpBox("Git Repository Path: \n" + path, MessageType.Info);
            
            GUILayout.Space(10);
            
            GUILayout.Label("Changed Files:", EditorStyles.boldLabel);

            if (ChangedFiles.Count == 0)
                EditorGUILayout.LabelField("No uncommitted changes detected.");

            foreach (string file in ChangedFiles)
                EditorGUILayout.LabelField(file);
        }

        public static void Footer()
        {
            GUILayout.Label($"Total Changes: {ChangedFiles.Count}", EditorStyles.miniBoldLabel);
        }
    }
}
#endif