#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace UnityEssentials
{
    /// <summary>
    /// Provides functionality for synchronizing a Git repository within a selected folder in the Unity Editor.
    /// </summary>
    /// <remarks>This class includes methods for performing common Git operations such as commit, push, fetch,
    /// and pull. It integrates with the Unity Editor through menu items, allowing users to execute Git commands
    /// directly from the Unity interface. The class also validates the availability of Git operations based on the
    /// selected folder's state.</remarks>
    public partial class GitFolderSynchronizer
    {
        public static Action Close;
        public static List<string> ChangedFiles = new();

        [MenuItem("Assets/Git Commit and Push", priority = 0)]
        public static void ShowWindow()
        {
            string path = GetSelectedPath();
            if (!string.IsNullOrEmpty(path))
            {
                ChangedFiles = GetChangedFiles(path);
                new EditorWindowDrawer()
                    .ShowUtility("Git Commit and Push Window", new(420, 300))
                    .SetHeader(Header)
                    .SetBody(Body, out _)
                    .SetFooter(Footer)
                    .GetCloseEvent(out Close);
            }
        }

        public static void CommitPushFetch(string path, string commitMessage)
        {
            Commit(path, commitMessage);
            PushOrigin();
            FetchOrigin();
        }

        [MenuItem("Assets/Git Fetch and Pull", priority = 1)]
        public static void FetchPull()
        {
            FetchOrigin();
            PullOrigin();
        }

        public static void PushOrigin() => Push();
        public static void FetchOrigin() => Fetch();
        public static void PullOrigin() => Pull();

        [MenuItem("Assets/Git Commit and Push", true)]
        public static bool ValidateGitCommit()
        {
            string path = GetSelectedPath();
            return !string.IsNullOrEmpty(path) && Directory.Exists(Path.Combine(path, ".git"))
                && HasUncommittedChanges(path);
        }

        [MenuItem("Assets/Git Fetch and Pull", true)]
        public static bool ValidateGitFetchPull()
        {
            string path = GetSelectedPath();
            return !string.IsNullOrEmpty(path) && Directory.Exists(Path.Combine(path, ".git"));
        }
    }
}
#endif