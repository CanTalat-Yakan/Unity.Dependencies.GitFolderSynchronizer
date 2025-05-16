using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using System.Linq;

#if UNITY_EDITOR
namespace UnityEssentials
{
    public class GitFolderPusher : EditorWindow
    {
        private static string gitFolderPath;
        private string commitMessage = string.Empty;
        private Vector2 scroll;
        private static List<string> changedFiles = new();

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
            gitFolderPath = GetSelectedPath();
            if (!string.IsNullOrEmpty(gitFolderPath))
            {
                changedFiles = GetChangedFiles(gitFolderPath);

                GitFolderPusher window = CreateInstance<GitFolderPusher>();
                window.titleContent = new GUIContent("Git Push Window");
                window.minSize = new Vector2(420, 300);
                window.position = new Rect(Screen.width / 2f, Screen.height / 2f, 420, 300);
                window.ShowUtility();
            }
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical("box");
            {
                GUILayout.Label("Commit & Push to Git", EditorStyles.boldLabel);

                EditorGUILayout.HelpBox("Repository: " + gitFolderPath, MessageType.Info);

                GUILayout.Space(5);
                GUILayout.Label("Changed Files:", EditorStyles.boldLabel);

                // Calculate the remaining space for the scroll view
                float remainingHeight = position.height - 200;

                if (changedFiles.Count == 0)
                    EditorGUILayout.LabelField("No uncommitted changes detected.");
                else
                {
                    // Begin a scroll view that fills the available space
                    scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(remainingHeight));
                    {
                        foreach (string file in changedFiles)
                            EditorGUILayout.LabelField(file);
                    }
                    EditorGUILayout.EndScrollView();

                    GUILayout.Label($"Total Changes: {changedFiles.Count}", EditorStyles.miniBoldLabel);
                }

                // This will push the following elements to the bottom
                GUILayout.FlexibleSpace();

                GUILayout.Label("Commit Message:", EditorStyles.label);
                commitMessage = EditorGUILayout.TextField(commitMessage);

                GUILayout.Space(10);

                GUI.enabled = changedFiles.Count > 0;
                if (GUILayout.Button("Commit & Push", GUILayout.Height(30)))
                {
                    CommitAndPush();
                    Close();
                }
                GUI.enabled = true;
            }
            EditorGUILayout.EndVertical();
        }

        private void CommitAndPush()
        {
            bool emptyCommitMessage = false;
            if (emptyCommitMessage = string.IsNullOrEmpty(commitMessage))
                commitMessage = "‎ ";

            RunGitCommand("add .");
            RunGitCommand($"commit -m \"{commitMessage}\"", emptyCommitMessage);
            RunGitCommand("push");
        }

        private void RunGitCommand(string arguments, bool emptyCommitMessage = false)
        {
            ProcessStartInfo startInfo = new("git", arguments)
            {
                WorkingDirectory = gitFolderPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (emptyCommitMessage)
                    output = output.Remove(15, 3);

                var hasCommitMessage = !string.IsNullOrEmpty(output);
                if (hasCommitMessage)
                Debug.Log("[Git] " + output);

                var hasErrorMessage = !string.IsNullOrEmpty(error);
                if (hasCommitMessage && process.ExitCode != 0)
                    Debug.LogError("[Git] " + error);
            }
        }

        private static string GetSelectedPath()
        {
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(assetPath)) return null;
            string fullPath = Path.GetFullPath(assetPath);
            return Directory.Exists(fullPath) ? fullPath : Path.GetDirectoryName(fullPath);
        }

        private static bool HasUncommittedChanges(string repoPath)
        {
            try
            {
                ProcessStartInfo startInfo = new("git", "status --porcelain")
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

        private static List<string> GetChangedFiles(string repoPath)
        {
            List<string> files = new();
            try
            {
                ProcessStartInfo startInfo = new("git", "status --porcelain")
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
                                "A" or "A " or " A" => "Added",
                                "M" or "M " or " M" => "Modified",
                                "D" or "D " or " D" => "Deleted",
                                "R" or "R " or " R" => "Renamed",
                                "C" or "C " or " C" => "Copied",
                                "U" or "U " or " U" => "Conflict",
                                _ => "Changed"
                            };

                            files.Add($"[{statusLabel}] {filePath}");
                        }
                    }
                }
            }
            catch (System.Exception e) { Debug.LogError("[Git] Failed to get changed files: " + e.Message); }

            return files;
        }
    }
}
#endif