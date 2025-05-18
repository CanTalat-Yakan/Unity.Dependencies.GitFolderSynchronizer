using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    public partial class GitFolderSynchronizer : EditorWindow
    {
        [MenuItem("Assets/Git Commit and Push", priority = 0)]
        public static void ShowWindow()
        {
            string path = GetSelectedPath();
            if (!string.IsNullOrEmpty(path))
            {
                _changedFiles = GetChangedFiles(path);

                var window = CreateInstance<GitFolderSynchronizer>();
                window.titleContent = new GUIContent("Git Commit Window");
                window.minSize = new Vector2(420, 300);
                window.position = new Rect(Screen.width / 2f, Screen.height / 2f, 420, 300);
                window.ShowUtility();
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

        [MenuItem("Assets/Git Push and Fetch", priority = 2)]
        public static void PushFetch()
        {
            PushOrigin();
            FetchOrigin();
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

        [MenuItem("Assets/Git Push and Fetch", true)]
        [MenuItem("Assets/Git Fetch and Pull", true)]
        public static bool ValidateGitFetchPull()
        {
            string path = GetSelectedPath();
            return !string.IsNullOrEmpty(path) && Directory.Exists(Path.Combine(path, ".git"));
        }
    }
}
