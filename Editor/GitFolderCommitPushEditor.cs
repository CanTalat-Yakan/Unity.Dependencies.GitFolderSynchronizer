#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    public partial class GitFolderSynchronizer
    {
        // Instance-scoped editor state (aligned with GitHubRepositoryClonerEditor pattern)
        public EditorWindowDrawer Window;
        public Action Repaint;
        public Action Close;

        public string Token;
        public List<string> ChangedFiles = new();

        private string _commitMessage = string.Empty;
        private string _tokenPlaceholder = string.Empty;

        [MenuItem("Assets/Git Commit and Push", true)]
        public static bool ValidateGitFolderSynchronizer()
        {
            string path = GetSelectedPath();
            return !string.IsNullOrEmpty(path) && Directory.Exists(Path.Combine(path, ".git"))
                                               && HasUncommittedChanges(path);
        }

        [MenuItem("Assets/Git Commit and Push", priority = -100)]
        public static void ShowWindow()
        {
            string path = GetSelectedPath();
            if (!string.IsNullOrEmpty(path))
            {
                var editor = new GitFolderSynchronizer();
                editor.Token = EditorPrefs.GetString(TokenKey, "");
                editor.ChangedFiles = GetChangedFiles(path);

                editor.Window = EditorWindowDrawer
                    .CreateInstance("Git Commit and Push Window", new(420, 300))
                    .SetHeader(editor.Header, EditorWindowStyle.Toolbar)
                    .SetBody(editor.Body, EditorWindowStyle.Margin)
                    .SetFooter(editor.Footer, EditorWindowStyle.HelpBox)
                    .GetRepaintEvent(out editor.Repaint)
                    .GetCloseEvent(out editor.Close)
                    .ShowAsUtility();
            }
        }

        public void Header()
        {
            // If no token, prompt for token entry like GitHubRepositoryClonerEditor
            if (string.IsNullOrEmpty(Token))
            {
                GUILayout.Label("Enter your Git Token:");
                _tokenPlaceholder = EditorGUILayout.PasswordField(_tokenPlaceholder, EditorStyles.toolbarTextField);

                if (GUILayout.Button("Save Token", EditorStyles.toolbarButton))
                {
                    Token = _tokenPlaceholder;
                    EditorPrefs.SetString(TokenKey, Token);
                    Repaint?.Invoke();
                }
                return;
            }

            // Token exists: normal toolbar
            GUILayout.Label("Commit Message:", EditorStyles.label, GUILayout.Width(110));
            _commitMessage = EditorGUILayout.TextField(_commitMessage, EditorStyles.toolbarTextField);

            if (GUILayout.Button("Fetch and Pull", EditorStyles.toolbarButton))
                FetchPull();

            if (GUILayout.Button("Change Token", EditorStyles.toolbarButton))
            {
                Token = string.Empty;
                EditorPrefs.DeleteKey(TokenKey);
                _tokenPlaceholder = string.Empty;
                Repaint?.Invoke();
            }
        }

        public void Body()
        {
            // If no token, show guidance (mirrors GitHubRepositoryClonerEditor behavior)
            if (string.IsNullOrEmpty(Token))
            {
                EditorGUILayout.HelpBox(
                    "To push to remote over HTTPS, you need a Personal Access Token (PAT) stored in the editor.",
                    MessageType.Info);
                EditorGUILayout.LabelField(
                    "You can create either a Fine-grained or Classic token. When creating the token:\n\n" +
                    "• Allow access to the repository you plan to push to.\n\n" +
                    "• Ensure 'Contents' permission includes 'Read and write' to allow commits and pushes.\n\n" +
                    "After generating the token, paste it in the header above and save it.", EditorStyles.wordWrappedLabel);
                if (EditorGUILayout.LinkButton("Create Personal Access Tokens"))
                    Application.OpenURL("https://github.com/settings/personal-access-tokens");
                return;
            }

            GUILayout.Label("Changed Files:", EditorStyles.boldLabel);

            if (ChangedFiles.Count == 0)
            {
                EditorGUILayout.LabelField("No uncommitted changes detected.");
                return;
            }

            foreach (string file in ChangedFiles)
                EditorGUILayout.LabelField(file);
        }

        public void Footer()
        {
            // If no token, no actions
            if (string.IsNullOrEmpty(Token))
                return;

            string path = GetSelectedPath();
            if (string.IsNullOrEmpty(path))
                return;

            GUILayout.Label($"Total Changes: {ChangedFiles.Count}", EditorStyles.miniBoldLabel);

            int assetsIndex = path.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
                EditorGUILayout.HelpBox("Git Repository Path: \n" + path.Substring(assetsIndex), MessageType.Info);
            else
                EditorGUILayout.HelpBox("Git Repository Path: \n" + path, MessageType.Info);

            GUI.enabled = ChangedFiles.Count > 0;
            if (GUILayout.Button("Commit and Push"))
            {
                Commit(path, _commitMessage);
                Push();
                Fetch();
                Close?.Invoke();
            }
            GUI.enabled = true;
        }
    }
}
#endif