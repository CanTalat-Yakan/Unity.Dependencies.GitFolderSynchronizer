#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System;

namespace UnityEssentials
{
    public partial class GitFolderSynchronizer : EditorWindow
    {
        private const string TokenKey = "GitToken";

        private static (string output, string error, int exitCode) RunGitCommand(string path, string arguments)
        {
            string output = string.Empty;
            string error = string.Empty;
            int exitCode = -1;
            string token = EditorPrefs.GetString(TokenKey, "");

            try
            {
                ProcessStartInfo startInfo = new("git", arguments)
                {
                    WorkingDirectory = path,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                startInfo.EnvironmentVariables["LC_ALL"] = "C";

                if (arguments.StartsWith("push") && !string.IsNullOrEmpty(token))
                {
                    // Store the token in memory for this command only
                    startInfo.EnvironmentVariables["GIT_ASKPASS"] = "echo";
                    startInfo.EnvironmentVariables["GIT_USERNAME"] = "token";
                    startInfo.EnvironmentVariables["GIT_PASSWORD"] = token;
                }

                using (Process process = Process.Start(startInfo))
                {
                    output = process.StandardOutput.ReadToEnd();
                    error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                }
            }
            catch (System.Exception ex)
            {
                error = ex.Message;
                Debug.LogError($"[Git] Command 'git {arguments}' failed: {ex.Message}");
            }

            return (output, error, exitCode);
        }

        private static (string output, string error, int exitCode) RunPushGitCommand(string path)
        {
            string token = EditorPrefs.GetString(TokenKey, "");
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[Git] No token found for push operation");
                return ("", "No Git token configured in EditorPrefs", -1);
            }

            // First get the remote URL
            var (remoteOutput, remoteError, remoteExitCode) = RunGitCommand(path, "remote get-url origin");
            if (remoteExitCode != 0 || string.IsNullOrEmpty(remoteOutput))
            {
                Debug.LogError("[Git] Failed to get remote URL");
                return (remoteOutput, remoteError, remoteExitCode);
            }

            string remoteUrl = remoteOutput.Trim();

            // Handle different URL formats
            string authenticatedUrl;
            if (remoteUrl.StartsWith("https://"))
            {
                // For HTTPS URLs, insert the token
                var uri = new Uri(remoteUrl);
                authenticatedUrl = $"https://{token}@{uri.Host}{uri.PathAndQuery} HEAD";
            }
            else if (remoteUrl.StartsWith("git@"))
            {
                // For SSH URLs, we can't use token auth - might want to use SSH keys instead
                Debug.LogError("[Git] SSH remote detected - tokens don't work with SSH. Use SSH keys instead.");
                return ("", "SSH remote detected - use SSH keys instead of tokens", -1);
            }
            else
            {
                Debug.LogError($"[Git] Unsupported remote URL format: {remoteUrl}");
                return ("", $"Unsupported remote URL format: {remoteUrl}", -1);
            }

            // Execute the push with the authenticated URL
            return RunGitCommand(path, $"push {authenticatedUrl}");
        }

        private static string GetSelectedPath()
        {
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(assetPath)) return null;
            string fullPath = Path.GetFullPath(assetPath);
            return Directory.Exists(fullPath) ? fullPath : Path.GetDirectoryName(fullPath);
        }
    }
}
#endif