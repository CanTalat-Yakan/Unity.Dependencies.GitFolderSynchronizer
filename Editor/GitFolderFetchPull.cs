#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Provides functionality for synchronizing Git repositories within the Unity Editor.
    /// </summary>
    /// <remarks>This class is designed to be used as part of the Unity Editor environment. It includes
    /// methods for  performing common Git operations such as fetching and pulling changes from a remote repository. 
    /// The operations are executed on the currently selected repository path, and feedback is provided  through Unity's
    /// logging system.</remarks>
    public partial class GitFolderSynchronizer : EditorWindow
    {
        /// <summary>
        /// Executes a Git fetch operation on the currently selected repository.
        /// </summary>
        /// <remarks>This method retrieves updates from the remote repository for the currently selected 
        /// local repository. If no repository is selected, or if the fetch operation fails,  an error message is
        /// logged.</remarks>
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

        /// <summary>
        /// Pulls the latest changes from the remote repository for the currently selected path.
        /// </summary>
        /// <remarks>This method checks if the selected repository is behind the remote branch. If it is, 
        /// it attempts to pull the latest changes. If the repository is already up-to-date,  no action is taken. Logs
        /// are generated to indicate the success or failure of the operation.</remarks>
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

        /// <summary>
        /// Determines whether the local Git repository is behind its remote counterpart.
        /// </summary>
        /// <remarks>This method checks the Git status of the specified repository to determine if it is
        /// behind the remote branch. If the Git command fails, the method logs an error and returns <see
        /// langword="false"/>.</remarks>
        /// <param name="repositoryPath">The file system path to the local Git repository.</param>
        /// <returns><see langword="true"/> if the local repository is behind the remote repository; otherwise, <see
        /// langword="false"/>.</returns>
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