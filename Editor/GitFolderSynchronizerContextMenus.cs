#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace UnityEssentials
{
    public partial class GitFolderSynchronizer
    {
        public static Action Close;
        public static List<string> ChangedFiles = new();

        [MenuItem("Assets/Git Commit and Push", priority = -100)]
        public static void ShowWindow()
        {
            string path = GetSelectedPath();
            if (!string.IsNullOrEmpty(path))
            {
                ChangedFiles = GetChangedFiles(path);
                new EditorWindowDrawer("Git Commit and Push Window", new(420, 300))
                    .SetHeader(Header)
                    .SetBody(Body)
                    .SetFooter(Footer)
                    .GetCloseEvent(out Close)
                    .ShowUtility();
            }
        }

        public static void CommitPushFetch(string path, string commitMessage)
        {
            Commit(path, commitMessage);
            PushOrigin();
            FetchOrigin();
        }

        [MenuItem("Assets/Git Fetch and Pull", priority = -99)]
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