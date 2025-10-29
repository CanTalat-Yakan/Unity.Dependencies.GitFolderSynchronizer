#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityEssentials
{
    public partial class GitFolderSynchronizer
    {
        private static bool HasUnpushedCommits(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(Path.Combine(path, ".git")))
                return false;

            // Use status -b to detect [ahead N]
            var (output, _, exitCode) = RunGitCommand(path, "status --porcelain -b");
            if (exitCode != 0 || string.IsNullOrEmpty(output))
                return false;

            string firstLine = output.Split('\n')[0];
            return firstLine.Contains("[ahead");
        }

        private static List<string> GetUnpushedCommitSummaries(string path)
        {
            List<string> commits = new();
            if (string.IsNullOrEmpty(path) || !Directory.Exists(Path.Combine(path, ".git")))
                return commits;

            // Determine upstream tracking branch if any
            var (upstreamOut, _, upstreamCode) = RunGitCommand(path, "rev-parse --abbrev-ref --symbolic-full-name @{u}");
            string upstreamRef = null;
            if (upstreamCode == 0 && !string.IsNullOrWhiteSpace(upstreamOut))
                upstreamRef = upstreamOut.Trim();

            // Fallback to origin/<branch>
            if (string.IsNullOrEmpty(upstreamRef))
            {
                var (branchOut, _, branchCode) = RunGitCommand(path, "rev-parse --abbrev-ref HEAD");
                if (branchCode != 0 || string.IsNullOrWhiteSpace(branchOut))
                    return commits;
                string branch = branchOut.Trim();
                upstreamRef = $"origin/{branch}";
            }

            var (logOut, _, logCode) = RunGitCommand(path, $"log --oneline {upstreamRef}..HEAD");
            if (logCode != 0 || string.IsNullOrWhiteSpace(logOut))
                return commits;

            using StringReader reader = new(logOut);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                commits.Add(line.Trim());
            }

            return commits;
        }

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

        private static void Push(string token)
        {
            var path = GetSelectedPath();
            var (pushOutput, pushError, exitCode) = RunPushGitCommand(path, token);

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
            var (logOut, logErr, logCode) = RunGitCommand(path, "log --date=iso-strict --pretty=format:%ad  -  %h  -  %s");
            if (logCode != 0)
            {
                Debug.LogError($"[Git] Failed to read log: {logErr}");
                return;
            }
        }
    }
}
#endif