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
        private string commitMessage = "‎ ";
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
            GUILayout.Label("Commit & Push to Git", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Repository: " + gitFolderPath, MessageType.Info);

            GUILayout.Space(5);
            GUILayout.Label("Changed Files:", EditorStyles.boldLabel);

            if (changedFiles.Count == 0)
                EditorGUILayout.LabelField("No uncommitted changes detected.");
            else
            {
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(100));
                foreach (string file in changedFiles)
                    EditorGUILayout.LabelField(file);
                EditorGUILayout.EndScrollView();

                GUILayout.Label($"Total Changes: {changedFiles.Count}", EditorStyles.miniBoldLabel);
            }

            GUILayout.Space(10);
            GUILayout.Label("Commit Message:", EditorStyles.label);
            commitMessage = EditorGUILayout.TextField(commitMessage);

            GUILayout.FlexibleSpace();
            GUI.enabled = changedFiles.Count > 0 && !string.IsNullOrWhiteSpace(commitMessage);

            if (GUILayout.Button("Commit & Push", GUILayout.Height(30)))
            {
                CommitAndPush();
                Close();
            }

            GUI.enabled = true;
            EditorGUILayout.EndVertical();
        }

        private void CommitAndPush()
        {
            if (string.IsNullOrWhiteSpace(commitMessage))
            {
                Debug.LogError("Commit message cannot be empty.");
                return;
            }

            RunGitCommand("add .");
            RunGitCommand($"commit -m \"{commitMessage}\"");
            RunGitCommand("push");
        }

        private void RunGitCommand(string arguments)
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

                // Characters to remove: invisible LTR/RTL and directional formatting
                char[] invisibleChars = new[] { '\u200E', '\u200F', '\u202A', '\u202B', '\u202C', '\u202D', '\u202E' };

                // Remove them from commitMessage and output
                string cleanedCommitMessage = new string(commitMessage?.Where(c => !invisibleChars.Contains(c)).ToArray());
                string cleanedOutput = new string(output?.Where(c => !invisibleChars.Contains(c)).ToArray());

                // Check if commit message is empty or whitespace after cleaning
                bool isInvisibleCommit = string.IsNullOrWhiteSpace(cleanedCommitMessage);

                // Only log if the cleaned output has content
                if (!string.IsNullOrEmpty(cleanedOutput))
                    Debug.Log("[Git] " + cleanedOutput);


                if (!string.IsNullOrEmpty(error) && process.ExitCode != 0)
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