#if UNITY_EDITOR
using UnityEngine;

namespace UnityEssentials
{
    public partial class GitFolderSynchronizer
    {
        private static void Fetch()
        {
            string path = GetSelectedPath();
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[Git] No repository selected.");
                return;
            }

            var (fetchOutput, fetchError, fetchExitCode) = RunGitCommand(path, "fetch");
            if (fetchExitCode != 0)
            {
                Debug.LogError($"[Git] Fetch failed: {fetchError}");
                return;
            }
        }

        private static void Pull()
        {
            string path = GetSelectedPath();
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[Git] No repository selected.");
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

        private static bool CheckIfBehind(string repositoryPath)
        {
            var (output, error, exitCode) = RunGitCommand(repositoryPath, "status --porcelain -b");
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
#endif