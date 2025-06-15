#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityEssentials
{
    public partial class GitFolderSynchronizer
    {
        private static void Commit(string path, string commitMessage)
        {
            const string EmptyCommitMessage = "⠀⠀⠀⠀⠀";
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