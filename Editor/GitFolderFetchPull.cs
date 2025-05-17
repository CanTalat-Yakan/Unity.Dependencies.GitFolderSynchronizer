using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    public partial class GitFolderSynchronizer : EditorWindow
    {
        [MenuItem("Assets/Git Fetch and Pull", true)]
        public static bool ValidateGitFetchPull()
        {
            string path = GetSelectedPath();
            return !string.IsNullOrEmpty(path) && Directory.Exists(Path.Combine(path, ".git"));
        }

        [MenuItem("Assets/Git Fetch and Pull", priority = 0)]
        public static void FetchOrigin() => FetchAndPull();

        private static void FetchAndPull()
        {
            string path = GetSelectedPath();
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[Git] No repository selected.");
                return;
            }

            var (fetchOutput, fetchError, exitCode) = RunGitCommand(path, "fetch");
            if (exitCode != 0)
            {
                Debug.LogError($"[Git] Fetch failed: {fetchError}");
                return;
            }

            bool isBehind = CheckIfBehind(path);
            if (isBehind)
            {
                // Pull changes
                var (pullOutput, pullError, pullExitCode) = RunGitCommand(path, "pull");
                if (pullExitCode != 0)
                {
                    Debug.LogError($"[Git] Pull failed: {pullError}");
                    return;
                }
                Debug.Log($"[Git] Successfully pulled changes:\n{pullOutput}");
            }
            else Debug.Log("[Git] Repository is up-to-date");
        }

        private static bool CheckIfBehind(string repoPath)
        {
            var (output, error, exitCode) = RunGitCommand(repoPath, "status --porcelain -b");
            if (exitCode != 0)
            {
                Debug.LogError("[Git] Status check failed: " + error);
                return false;
            }

            string[] lines = output.Split('\n');
            return lines.Length > 0 && lines[0].Contains("[behind");
        }
    }
}