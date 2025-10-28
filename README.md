# Unity Essentials

**Unity Essentials** is a lightweight, modular utility namespace designed to streamline development in Unity. 
It provides a collection of foundational tools, extensions, and helpers to enhance productivity and maintain clean code architecture.

## 📦 This Package

This package is part of the **Unity Essentials** ecosystem.  
It integrates seamlessly with other Unity Essentials modules and follows the same lightweight, dependency-free philosophy.

## 🌐 Namespace

All utilities are under the `UnityEssentials` namespace. This keeps your project clean, consistent, and conflict-free.

```csharp
using UnityEssentials;
```

# Git Folder Pusher  
Unity Editor tool for staging, committing, and pushing changes to a Git repository directly from the Unity interface.

## Features
- Adds "Git Commit and Push" option under `Assets/` menu if the selected folder contains a `.git` directory.  
- Validates whether uncommitted changes exist before enabling the menu item.  
- On activation, opens a modal EditorWindow displaying changed files with status labels.  
- Scrollable file list dynamically fills available vertical space.  
- Allows typing a commit message and triggers `git add`, `git commit`, and `git push` operations.  
- Handles empty commit messages by inserting a zero-width character.  
- Captures and logs Git stdout/stderr to Unity's console for debugging.  
- Parses `git status --porcelain` output to extract file paths and corresponding Git status codes.  
- Status codes mapped to human-readable labels such as "Untracked", "Modified", "Deleted", etc.  
- Opens the window only when a valid Git repository folder is selected.