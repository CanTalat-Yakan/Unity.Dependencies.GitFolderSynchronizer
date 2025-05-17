using UnityEditor;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace UnityEssentials
{
    public partial class GitFolderSynchronizer : EditorWindow
    {
        private static (string output, string error, int exitCode) RunGitCommand(string path, string arguments)
        {
            string output = string.Empty;
            string error = string.Empty;
            int exitCode = -1;

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

        private static string GetSelectedPath()
        {
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(assetPath)) return null;
            string fullPath = Path.GetFullPath(assetPath);
            return Directory.Exists(fullPath) ? fullPath : Path.GetDirectoryName(fullPath);
        }
    }
}