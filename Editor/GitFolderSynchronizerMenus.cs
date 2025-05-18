using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    public partial class GitFolderSynchronizer : EditorWindow
    {
        [MenuItem("Assets/Git/Commit", priority = 0)]
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

        [MenuItem("Assets/Git/Push", priority = 1)]
        public static void PushOrigin() => Push();

        [MenuItem("Assets/Git/Fetch", priority = 2)]
        public static void FetchOrigin() => Fetch();

        [MenuItem("Assets/Git/Pull", priority = 3)]
        public static void PullOrigin() => Pull();

        [MenuItem("Assets/Git/Commit", true)]
        [MenuItem("Assets/Git/Push", true)]
        public static bool ValidateGitPush()
        {
            string path = GetSelectedPath();
            if (string.IsNullOrEmpty(path)) return false;

            string gitPath = Path.Combine(path, ".git");
            if (!Directory.Exists(gitPath)) return false;

            return HasUncommittedChanges(path);
        }

        [MenuItem("Assets/Git/Fetch", true)]
        [MenuItem("Assets/Git/Pull", true)]
        public static bool ValidateGitFetchPull()
        {
            string path = GetSelectedPath();
            return !string.IsNullOrEmpty(path) && Directory.Exists(Path.Combine(path, ".git"));
        }
    }
}
