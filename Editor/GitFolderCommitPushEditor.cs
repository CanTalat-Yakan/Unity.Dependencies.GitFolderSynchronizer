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
        public EditorWindowDrawer Window;
        public Action Repaint;
        public Action Close;

        public string Token;
        public List<string> ChangedFiles = new();
        public List<string> UnpushedCommits = new();

        private string _commitMessage = string.Empty;
        private string _tokenPlaceholder = string.Empty;

        [MenuItem("Assets/Git Commit and Push", true)]
        public static bool ValidateGitFolderSynchronizer()
        {
            string path = GetSelectedPath();
            return !string.IsNullOrEmpty(path)
                   && Directory.Exists(Path.Combine(path, ".git"))
                   && (HasUncommittedChanges(path) || HasUnpushedCommits(path));
        }

        [MenuItem("Assets/Git Commit and Push", priority = -100)]
        public static void ShowWindow()
        {
            string path = GetSelectedPath();
            if (!string.IsNullOrEmpty(path))
            {
                var editor = new GitFolderSynchronizer();
                editor.Token = EditorPrefs.GetString(TokenKey, "");
                editor.RefreshState(path);

                editor.Window = EditorWindowDrawer
                    .CreateInstance("Git Commit and Push Window", new(480, 340))
                    .SetHeader(editor.Header, EditorWindowStyle.Toolbar)
                    .SetBody(editor.Body, EditorWindowStyle.Margin)
                    .SetFooter(editor.Footer, EditorWindowStyle.HelpBox)
                    .GetRepaintEvent(out editor.Repaint)
                    .GetCloseEvent(out editor.Close)
                    .ShowAsUtility();
            }
        }

        private void RefreshState(string path)
        {
            ChangedFiles = GetChangedFiles(path);
            UnpushedCommits = GetUnpushedCommitSummaries(path);
        }

        public void Header()
        {
            string path = GetSelectedPath();

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

            bool hasUncommitted = ChangedFiles != null && ChangedFiles.Count > 0;
            bool hasUnpushedOnly = !hasUncommitted && (UnpushedCommits?.Count ?? 0) > 0;

            // Token exists: context-aware toolbar
            if (hasUncommitted)
            {
                GUILayout.Label("Commit Message:", EditorStyles.label, GUILayout.Width(110));
                _commitMessage = EditorGUILayout.TextField(_commitMessage, EditorStyles.toolbarTextField);
            }
            else if (hasUnpushedOnly)
            {
                GUILayout.Label($"Ahead by {(UnpushedCommits?.Count ?? 0)} commit(s)", EditorStyles.boldLabel);
            }
            else
            {
                GUILayout.Label("Working tree clean", EditorStyles.miniBoldLabel);
            }

            if (GUILayout.Button("Fetch and Pull", EditorStyles.toolbarButton))
            {
                FetchPull();
                RefreshState(path);
                Repaint?.Invoke();
            }

            if (hasUnpushedOnly)
            {
                if (GUILayout.Button("Push", EditorStyles.toolbarButton))
                {
                    Push();
                    RefreshState(path);
                    if ((UnpushedCommits?.Count ?? 0) == 0 && ChangedFiles.Count == 0)
                        Close?.Invoke();
                    else
                        Repaint?.Invoke();
                }
            }

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

            bool hasUncommitted = ChangedFiles != null && ChangedFiles.Count > 0;
            bool hasUnpushedOnly = !hasUncommitted && (UnpushedCommits?.Count ?? 0) > 0;

            if (hasUncommitted)
            {
                GUILayout.Label("Changed Files:", EditorStyles.boldLabel);

                foreach (string file in ChangedFiles)
                    EditorGUILayout.LabelField(file);
            }
            else if (hasUnpushedOnly)
            {
                GUILayout.Label("Unpushed Commits:", EditorStyles.boldLabel);
                foreach (string line in UnpushedCommits)
                    EditorGUILayout.LabelField(line);
            }
            else
            {
                EditorGUILayout.LabelField("No uncommitted changes and nothing to push.");
            }
        }

        public void Footer()
        {
            // If no token, no actions
            if (string.IsNullOrEmpty(Token))
                return;

            string path = GetSelectedPath();
            if (string.IsNullOrEmpty(path))
                return;

            bool hasUncommitted = ChangedFiles != null && ChangedFiles.Count > 0;
            bool hasUnpushedOnly = !hasUncommitted && (UnpushedCommits?.Count ?? 0) > 0;

            if (hasUncommitted)
                GUILayout.Label($"Total Changes: {ChangedFiles.Count}", EditorStyles.miniBoldLabel);
            else if (hasUnpushedOnly)
                GUILayout.Label($"Ahead by {(UnpushedCommits?.Count ?? 0)} commit(s)", EditorStyles.miniBoldLabel);

            int assetsIndex = path.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
                EditorGUILayout.HelpBox("Git Repository Path: \n" + path.Substring(assetsIndex), MessageType.Info);
            else
                EditorGUILayout.HelpBox("Git Repository Path: \n" + path, MessageType.Info);

            if (hasUncommitted)
            {
                GUI.enabled = ChangedFiles.Count > 0;
                if (GUILayout.Button("Commit and Push"))
                {
                    Commit(path, _commitMessage);
                    Push();
                    Fetch(); // update remote tracking info

                    RefreshState(path);
                    if ((UnpushedCommits?.Count ?? 0) == 0 && ChangedFiles.Count == 0)
                        Close?.Invoke();
                    else
                        Repaint?.Invoke();
                }
                GUI.enabled = true;
            }
            else if (hasUnpushedOnly)
            {
                if (GUILayout.Button($"Push {(UnpushedCommits?.Count ?? 0)} Commit(s)"))
                {
                    Push();
                    RefreshState(path);
                    if ((UnpushedCommits?.Count ?? 0) == 0)
                        Close?.Invoke();
                    else
                        Repaint?.Invoke();
                }
            }
        }
    }
}
#endif