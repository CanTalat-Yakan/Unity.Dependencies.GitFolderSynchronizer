#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    public partial class GitFolderSynchronizer
    {
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
                CommitPushFetch(path, commitMessage);
                Close();
            }
            GUI.enabled = true;
        }
    }
}
#endif