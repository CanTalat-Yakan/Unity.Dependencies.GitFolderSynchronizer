# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
  - Window → Package Manager
  - "+" → "Add package from git URL…"
  - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
  - Tools → Install & Update UnityEssentials
  - Install all or select individual modules; run again anytime to update

---

# Git Folder Synchronizer

> Quick overview: Git CLI commands to fetch, pull, stage, commit, and push changes in the selected folder using a stored HTTPS personal access token.

A tiny editor tool that adds two context‑aware menu actions to the Assets menu. It resolves the on‑disk path for your current selection, runs common Git operations there, and shows a lightweight review window before committing. It’s great for quick syncs without leaving Unity, while still relying on your system Git.

![screenshot](Documentation/Screenshot.png)

## Features
- Context‑aware repository detection
  - Works on the selected folder (or asset) by resolving its filesystem path and walking up to find a `.git` folder
- Fetch & Pull (safe sync)
  - Runs `git fetch`, checks if the local branch is behind, and only then performs `git pull`
- Commit & Push (review first)
  - Opens a small review window: shows `git status --porcelain` changes, lets you enter a commit message, then stages/commits/pushes
  - Uses HTTPS and an EditorPrefs‑stored token for authentication
- Global Commit & Push (batch mode)
  - Scans all Git repositories under `Assets/` and also includes the project root repository
  - Commits with an empty message (invisible characters) and pushes each repo using the saved token
  - Shows a progress bar and prints a per‑repository summary at the end
- Simple, opinionated flow
  - Stages all changes under the repo (`git add .`), commits to current HEAD, pushes to `origin`
- LFS‑friendly
  - Works with Git LFS if it’s installed and configured for the repo
- Editor‑only
  - No runtime code; pure workflow helper inside the Unity Editor

## Requirements
- Unity Editor 6000.0+ (Editor‑only)
- Git installed and available on PATH
- HTTPS Personal Access Token (PAT) saved in EditorPrefs under the key `GitToken`
- Optional: Git LFS installed for repos using LFS

Tip: After installing Git/LFS, restart Unity so your updated PATH is picked up by the Editor process.

## Usage
- Ensure Git is installed and reachable from your shell/terminal
- In the Project window, select a folder inside the repo you want to operate on
- For pulling remote changes: Assets → Git Fetch and Pull (pulls only if behind)
- For committing/pushing: Assets → Git Commit and Push → review changes → enter a message → Commit and Push
- For batch syncing everything: Tools → Git Commit & Push All Changes
  - First‑time setup: open Assets → Git Commit and Push once to save your token, then run the Tools command

### Menu Commands
- Assets → Git Fetch and Pull
  - Runs `git fetch` then performs a `git pull` only when the branch is behind
  - Enabled when the selection resolves to a path inside a Git repository

- Assets → Git Commit and Push
  - Shows changed files (`git status --porcelain`) and a commit message field
  - On confirm: `git add .` → `git commit -m "…"` → `git push`
  - Enabled only when there are changes to commit

- Tools → Git Commit & Push All Changes
  - Recursively finds all Git repositories under `Assets/` and includes the Unity project root repository
  - For each repo: stage/commit (empty message) and push using the saved token
  - Displays a progress bar with current operation and a final per‑repository summary in the Console

Selection logic: The tool resolves the selected Project asset to an OS path and finds the nearest `.git` root above it. All commands run at that repo root, so nested subfolders work fine.

### Authentication (HTTPS token)
Push uses a Personal Access Token (PAT) stored in EditorPrefs under the key `GitToken`.

Tips
- GitHub scopes: `repo` usually suffices for private repos; public repos may require fewer scopes
- Security: EditorPrefs is per‑user; treat tokens like passwords and never commit them
- SSH remotes: token‑based push supports HTTPS remotes; SSH remotes aren’t supported by this tool

## Global Sync: Progress & Reporting
- Shows a progress bar while processing all repositories (submodules first, project root last)
- End of run prints a summary like:
  - `Processed: <n>, Repositories Found: <m>, Committed: <k>, Pushed: <p>`
  - Followed by a "Per‑Repository Summary:" section
- Per‑repository lines appear as:
  - `- [Committed and Pushed] <RepoName>`
  - `- [Pushed] <RepoName>`
  - `- [No Changes] <RepoName>`
  - `- [Commit Failed] <RepoName>: <reason>`
  - `- [Push Failed] <RepoName>: <reason>`
- Log messages print only the repository’s top‑level folder name (e.g., `Unity.Dependencies.GitFolderSynchronizer`), not the full filesystem path

## How It Works
- Fetch/Pull
  - `git fetch` → parse `git status --porcelain -b` → `git pull` only if behind
- Commit/Push
  - List changes with `git status --porcelain`
  - Stage all changes via `git add .`
  - Commit with the user‑entered message (empty allowed, discouraged)
  - Push current HEAD to `origin` using HTTPS with the stored token
- Global Commit & Push
  - Finds all repositories under `Assets/` and the project root repo, then repeats the commit/push flow for each
  - Uses the same helpers as the review window, ensuring consistent behavior

## Notes and Limitations
- Staging model
  - Uses `git add .` (no partial staging UI). For granular staging, use your Git client
- Remote/branch
  - Push targets `origin` and the current branch; custom remotes/refs aren’t exposed
- HTTPS only
  - SSH remotes aren’t supported for push in this workflow
- Commit message
  - Empty messages are permitted but discouraged

## Files in This Package
- `Editor/GitFolderFetchPull.cs` – Fetch & Pull command + behind‑check
- `Editor/GitFolderCommitPushEditor.cs` – Review window (changes list + commit message)
- `Editor/GitFolderCommitPush.cs` – Stage/commit/push helpers and change listing
- `Editor/GitFolderCommitPushGlobal.cs` – Global Tools menu command to commit & push all repos
- `Editor/GitFolderSynchronizer.cs` – Shared helpers (git invocation, token, selection path)
- `Editor/UnityEssentials.GitFolderSynchronizer.Editor.asmdef`

## Tags
unity, unity-editor, git, cli, fetch, pull, stage, commit, push, https, token, pat, editor-tool, workflow
