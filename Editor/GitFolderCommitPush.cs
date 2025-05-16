using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityEssentials
{
    public partial class GitFolderPusher : EditorWindow
    {
        [MenuItem("Assets/Git Commit and Push", true)]
        public static bool ValidateGitPush()
        {
            string path = GetSelectedPath();
            if (string.IsNullOrEmpty(path)) return false;

            string gitPath = Path.Combine(path, ".git");
            if (!Directory.Exists(gitPath)) return false;

            return HasUncommittedChanges(path);
        }

        [MenuItem("Assets/Git Commit and Push", priority = 0)]
        public static void ShowWindow()
        {
            string path = GetSelectedPath();
            if (!string.IsNullOrEmpty(path))
            {
                _changedFiles = GetChangedFiles(path);

                GitFolderPusher window = CreateInstance<GitFolderPusher>();
                window.titleContent = new GUIContent("Git Push Window");
                window.minSize = new Vector2(420, 300);
                window.position = new Rect(Screen.width / 2f, Screen.height / 2f, 420, 300);
                window.ShowUtility();
            }
        }

        private Vector2 _scrollPosition;
        private static List<string> _changedFiles = new();

        public void OnGUI()
        {
            string path = GetSelectedPath();
            string commitMessage = string.Empty;
            EditorGUILayout.BeginVertical("box");
            {
                GUILayout.Label("Commit & Push to Git", EditorStyles.boldLabel);

                EditorGUILayout.HelpBox("Repository: " + path, MessageType.Info);

                GUILayout.Space(5);
                GUILayout.Label("Changed Files:", EditorStyles.boldLabel);

                float remainingHeight = position.height - 200;

                if (_changedFiles.Count == 0)
                    EditorGUILayout.LabelField("No uncommitted changes detected.");
                else
                {
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(remainingHeight));
                    {
                        foreach (string file in _changedFiles)
                            EditorGUILayout.LabelField(file);
                    }
                    EditorGUILayout.EndScrollView();

                    GUILayout.Label($"Total Changes: {_changedFiles.Count}", EditorStyles.miniBoldLabel);
                }

                GUILayout.FlexibleSpace();

                GUILayout.Label("Commit Message:", EditorStyles.label);
                commitMessage = EditorGUILayout.TextField(commitMessage);

                GUILayout.Space(10);

                GUI.enabled = _changedFiles.Count > 0;
                if (GUILayout.Button("Commit & Push", GUILayout.Height(30)))
                {
                    CommitAndPush(path, commitMessage);
                    Close();
                }
                GUI.enabled = true;
            }
            EditorGUILayout.EndVertical();
        }

        private static void CommitAndPush(string path, string commitMessage)
        {
            bool emptyCommitMessage = false;
            if (emptyCommitMessage = string.IsNullOrEmpty(commitMessage))
                commitMessage = "‎ ";

            RunGitCommand(path, "add .");
            var (commitOutput, commitError, commitCode) = RunGitCommand(path, $"commit -m \"{commitMessage}\"");
            RunGitCommand(path, "push");

            if (emptyCommitMessage)
                commitOutput = commitOutput.Remove(15, 3);

            if (!string.IsNullOrEmpty(commitOutput))
                Debug.Log("[Git] " + commitOutput);

            if (!string.IsNullOrEmpty(commitError))
                Debug.LogError("[Git] " + commitError);
        }

        private static bool HasUncommittedChanges(string repoPath)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("git", "status --porcelain")
                {
                    WorkingDirectory = repoPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return !string.IsNullOrWhiteSpace(output);
                }
            }
            catch { return false; }
        }

        private static List<string> GetChangedFiles(string path)
        {
            List<string> files = new();
            try
            {
                var (output, error, code) = RunGitCommand(path, "status --porcelain");

                using (StringReader reader = new StringReader(output))
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
            }
            catch (System.Exception e) { Debug.LogError("[Git] Failed to get changed files: " + e.Message); }

            return files;
        }
    }
}
