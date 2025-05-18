#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    public partial class GitFolderSynchronizer : EditorWindow
    {
        private const string EmptyCommitMessage = "⠀";

        private Vector2 _scrollPosition;
        private static List<string> _changedFiles = new();

        public void OnGUI()
        {
            string path = GetSelectedPath();
            string commitMessage = string.Empty;
            EditorGUILayout.BeginVertical("box");
            {
                GUILayout.Label("Push to Git", EditorStyles.boldLabel);

                EditorGUILayout.HelpBox("Repository: " + path, MessageType.Info);

                GUILayout.Space(5);
                GUILayout.Label("Changed Files:", EditorStyles.boldLabel);

                float remainingHeight = position.height - 200;

                if (_changedFiles.Count == 0)
                    EditorGUILayout.LabelField("No uncommitted changes detected.");
                else
                {
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(remainingHeight));

                    foreach (string file in _changedFiles)
                        EditorGUILayout.LabelField(file);

                    EditorGUILayout.EndScrollView();

                    GUILayout.Label($"Total Changes: {_changedFiles.Count}", EditorStyles.miniBoldLabel);
                }

                GUILayout.FlexibleSpace();

                GUILayout.Label("Commit Message:", EditorStyles.label);
                commitMessage = EditorGUILayout.TextField(commitMessage);

                GUILayout.Space(10);

                GUI.enabled = _changedFiles.Count > 0;
                if (GUILayout.Button("Commit and Push", GUILayout.Height(30)))
                {
                    CommitPushFetch(path, commitMessage);
                    Close();
                }
                GUI.enabled = true;
            }
            EditorGUILayout.EndVertical();
        }

        private static void Commit(string path, string commitMessage)
        {
            bool emptyCommitMessage = false;
            if (emptyCommitMessage = string.IsNullOrEmpty(commitMessage))
                commitMessage = EmptyCommitMessage;

            RunGitCommand(path, "add .");
            var (commitOutput, commitError, exitCode) = RunGitCommand(path, $"commit -m \"{commitMessage}\"");

            if (emptyCommitMessage)
                commitOutput = commitOutput.Remove(15, 3);

            if (!string.IsNullOrEmpty(commitOutput))
                Debug.Log("[Git] " + commitOutput);

            if (!string.IsNullOrEmpty(commitError))
                Debug.LogError("[Git] " + commitError);
        }

        private static void Push()
        {
            var path = GetSelectedPath();
            var (pushOutput, pushError, exitCode) = RunPushGitCommand(path);
                        
            if (!string.IsNullOrEmpty(pushOutput))
                Debug.Log("[Git] " + pushOutput);

            if (!string.IsNullOrEmpty(pushError) && exitCode != 0)
                Debug.LogError("[Git] " + pushError);
        }

        private static bool HasUncommittedChanges(string path)
        {
            var (output, error, exitCode) = RunGitCommand(path, "status --porcelain");
            return !string.IsNullOrWhiteSpace(output);
        }

        private static List<string> GetChangedFiles(string path)
        {
            List<string> files = new();
            var (output, error, exitCode) = RunGitCommand(path, "status --porcelain");
            using (StringReader reader = new(output))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string statusCode = line.Length >= 2 ? line[..2].Trim() : "";
                    string filePath = line.Length > 3 ? line[3..].Trim() : line.Trim();

                    string statusLabel = statusCode switch
                    {
                        "??" => "Untracked",
                        "A" => "Added",
                        "M" => "Modified",
                        "D" => "Deleted",
                        "R" => "Renamed",
                        "C" => "Copied",
                        "U" => "Conflict",
                        _ => "Changed"
                    };

                    files.Add($"[{statusLabel}] {filePath}");
                }
            }

            return files;
        }
    }
}
#endif