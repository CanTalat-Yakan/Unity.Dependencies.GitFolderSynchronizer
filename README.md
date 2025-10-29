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

> Quick overview: Git CLI commands for the selected folder: fetch, pull, stage, commit and push using a stored HTTPS personal access token.

A tiny editor utility that adds convenient Assets menu actions to fetch/pull and commit/push a Git repository directly from the Unity Project window.

![screenshot](Documentation/Screenshot.png)

## Features
- Context-aware: works on the currently selected folder inside a Git repository
- Simple review UI: lists changed files and lets you enter a commit message
- Push via HTTPS using a token stored in EditorPrefs

## Requirements
- Unity Editor 6000.0+ (Editor-only; no runtime code)
- Git installed and available on your PATH
- Personal Access Token (PAT) for HTTPS operations (stored in EditorPrefs as `GitToken`)
- Optional: Git LFS if repositories use LFS

Tip: If the tool can’t find Git/LFS, install them and restart Unity so PATH updates are picked up.

## Usage
1) Ensure Git is installed and reachable from your shell/terminal
2) In the Project window, select a folder inside the repo you want to operate on
3) For pulling remote changes: Assets → Git Fetch and Pull (pulls only if behind)
4) For committing/pushing: Assets → Git Commit and Push → review changes → enter a message → Commit and Push

## Menu Commands
- Assets → Git Fetch and Pull
  - Runs `git fetch` then `git pull` in the selected folder’s repository
  - Enabled when the selection is inside a Git repo (a `.git` folder exists)
- Assets → Git Commit and Push
  - Opens a small window listing changed files (`git status --porcelain`)
  - Enter a commit message, then runs: `git add .`, `git commit -m "…"`, and `git push`
  - Enabled only if there are uncommitted changes

Selection logic: Select any folder (or asset) in the Project window; the tool resolves the on-disk path and runs Git commands there. Subfolders inside a repo work fine.

## Authentication (HTTPS token)
Push uses a Personal Access Token (PAT) stored in EditorPrefs under the key `GitToken`.

Tips
- GitHub scopes: `repo` is typically enough for private repos; public repos may require fewer scopes
- Security: EditorPrefs is per-user; treat tokens like passwords and never commit them
- SSH remotes: push via token is not supported for SSH remotes; use HTTPS or your Git client instead

## How It Works
- Fetch/Pull
  - Runs `git fetch`, checks “behind” status via `git status --porcelain -b`, then runs `git pull` if needed
- Commit/Push
  - Lists changes via `git status --porcelain`
  - Stages everything with `git add .`
  - Commits with your message (empty allowed, but discouraged)
  - Pushes to `origin` using an authenticated HTTPS URL with the token

## Notes and Limitations
- Staging: uses `git add .` to commit all current changes at the selected repo path
- Partial commits: not supported via this UI; use your Git client for fine-grained staging
- Remotes: push targets `origin` and current HEAD; multiple remotes/custom refs aren’t exposed
- SSH remotes: push is HTTPS-only
- Commit message: empty messages are allowed but discouraged

## Files in This Package
- `Editor/GitFolderFetchPull.cs` – Fetch & Pull command and behind-check logic
- `Editor/GitFolderCommitPushEditor.cs` – Review window (changed files + commit message)
- `Editor/GitFolderCommitPush.cs` – Commit/push helpers (stage/commit/push, change listing)
- `Editor/GitFolderSynchronizer.cs` – Shared helpers (run git, token handling, selection path)

## Tags
unity, unity-editor, git, cli, fetch, pull, stage, commit, push, https, token, pat, editor-tool, workflow
