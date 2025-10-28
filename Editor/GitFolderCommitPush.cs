#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UnityEssentials
{
    public partial class GitFolderSynchronizer
    {
        private static void Commit(string path, string commitMessage)
        {
            const string EmptyCommitMessage = "⠀⠀⠀⠀⠀";
            bool emptyCommitMessage = string.IsNullOrEmpty(commitMessage);
            if (emptyCommitMessage)
                commitMessage = EmptyCommitMessage;

            RunGitCommand(path, "add .");
            var (commitOutput, commitError, _) = RunGitCommand(path, $"commit -m \"{commitMessage}\"");

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
            var (output, _, _) = RunGitCommand(path, "status --porcelain");
            return !string.IsNullOrWhiteSpace(output);
        }

        private static List<string> GetChangedFiles(string path)
        {
            List<string> files = new();
            var (output, _, _) = RunGitCommand(path, "status --porcelain");
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

        // Build or overwrite a CHANGELOG.txt at the repository path with all commit dates and messages.
        // Includes the latest (HEAD) commit by querying git log after operations complete.
        private static void GenerateChangelog(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(Path.Combine(path, ".git")))
            {
                Debug.LogError("[Git] Invalid repository path for changelog generation.");
                return;
            }

            // Get current branch (non-fatal if fails)
            var (branchOut, _, _) = RunGitCommand(path, "rev-parse --abbrev-ref HEAD");
            string branch = string.IsNullOrWhiteSpace(branchOut) ? "(unknown)" : branchOut.Trim();

            // Get commit log: ISO date and subject, newest first
            var (logOut, logErr, logCode) = RunGitCommand(path, "log --date=iso-strict --pretty=format:%ad — %h — %s");
            if (logCode != 0)
            {
                Debug.LogError($"[Git] Failed to read log: {logErr}");
                return;
            }

            StringBuilder sb = new();
            sb.AppendLine("CHANGELOG");
            sb.AppendLine("=========");
            sb.AppendLine($"Repository: {path}");
            sb.AppendLine($"Branch: {branch}");
            sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ} UTC");
            sb.AppendLine();

            using (StringReader reader = new(logOut))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    sb.Append("- ");
                    sb.AppendLine(line.Trim());
                }
            }

            string changelogPath = Path.Combine(path, "CHANGELOG.txt");
            try
            {
                File.WriteAllText(changelogPath, sb.ToString(), Encoding.UTF8);
                Debug.Log($"[Git] CHANGELOG generated at: {changelogPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Git] Failed to write CHANGELOG: {ex.Message}");
            }
        }
    }
}
#endif