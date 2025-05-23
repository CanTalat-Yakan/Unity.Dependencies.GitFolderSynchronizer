#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Provides a Unity Editor window for synchronizing a local folder with a Git repository.
    /// </summary>
    /// <remarks>This class allows users to view uncommitted changes, enter a commit message, and perform Git
    /// operations such as committing and pushing changes directly from the Unity Editor. It is designed to streamline
    /// Git workflows for Unity projects by integrating basic Git functionality into the editor.</remarks>
    public partial class GitFolderSynchronizer : EditorWindow
    {
        private const string EmptyCommitMessage = "⠀⠀⠀⠀⠀";

        private Vector2 _scrollPosition;
        private static List<string> _changedFiles = new();

        /// <summary>
        /// Renders the graphical user interface (GUI) for committing and pushing changes to a Git repository.
        /// </summary>
        /// <remarks>This method displays a user interface for viewing uncommitted changes, entering a
        /// commit message,  and performing a commit and push operation to the selected Git repository. The interface
        /// includes  a list of changed files, a text field for the commit message, and a button to execute the
        /// operation.  The "Commit and Push" button is enabled only if there are uncommitted changes. Upon clicking the
        /// button,  the changes are committed and pushed to the repository, and the window is closed.</remarks>
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

        /// <summary>
        /// Commits all staged changes in the specified Git repository with the provided commit message.
        /// </summary>
        /// <remarks>This method stages all changes in the repository before committing them. If the
        /// commit message is empty or null, a default message is applied. The method logs the output and errors from
        /// the Git commit operation for debugging purposes.</remarks>
        /// <param name="path">The file system path to the root of the Git repository.</param>
        /// <param name="commitMessage">The message to associate with the commit. If the message is null or empty, a default message will be used.</param>
        private static void Commit(string path, string commitMessage)
        {
            bool emptyCommitMessage = false;
            if (emptyCommitMessage = string.IsNullOrEmpty(commitMessage))
                commitMessage = EmptyCommitMessage;

            RunGitCommand(path, "add .");
            var (commitOutput, commitError, exitCode) = RunGitCommand(path, $"commit -m \"{commitMessage}\"");

            if (emptyCommitMessage)
                commitOutput = commitOutput.Remove(15, 15);

            if (!string.IsNullOrEmpty(commitOutput))
                Debug.Log("[Git] " + commitOutput);

            if (!string.IsNullOrEmpty(commitError))
                Debug.LogError("[Git] " + commitError);
        }

        /// <summary>
        /// Executes a Git push command for the currently selected path and logs the output or errors.
        /// </summary>
        /// <remarks>This method retrieves the selected path, runs the Git push command, and logs the
        /// results.  If the push operation produces output, it is logged as informational.  If an error occurs and the
        /// exit code is non-zero, the error is logged as an error message.</remarks>
        private static void Push()
        {
            var path = GetSelectedPath();
            var (pushOutput, pushError, exitCode) = RunPushGitCommand(path);
                        
            if (!string.IsNullOrEmpty(pushOutput))
                Debug.Log("[Git] " + pushOutput);

            if (!string.IsNullOrEmpty(pushError) && exitCode != 0)
                Debug.LogError("[Git] " + pushError);
        }

        /// <summary>
        /// Determines whether the specified Git repository has uncommitted changes.
        /// </summary>
        /// <param name="path">The file system path to the Git repository.</param>
        /// <returns><see langword="true"/> if the repository has uncommitted changes; otherwise, <see langword="false"/>.</returns>
        private static bool HasUncommittedChanges(string path)
        {
            var (output, error, exitCode) = RunGitCommand(path, "status --porcelain");
            return !string.IsNullOrWhiteSpace(output);
        }

        /// <summary>
        /// Retrieves a list of files that have been changed in a Git repository at the specified path.
        /// </summary>
        /// <remarks>This method uses the `git status --porcelain` command to determine the status of
        /// files in the repository. The returned list includes files with statuses such as untracked, added, modified,
        /// deleted, renamed, copied, or in conflict.</remarks>
        /// <param name="path">The file system path to the root of the Git repository. This cannot be null or empty.</param>
        /// <returns>A list of strings, where each string represents a changed file and its status.  The format of each string is
        /// "[<c>Status</c>] <c>FilePath</c>", where <c>Status</c> indicates the type of change  (e.g., "Untracked",
        /// "Added", "Modified", etc.) and <c>FilePath</c> is the relative path of the file.</returns>
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