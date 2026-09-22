# Ordeal

**Ordeal** is a desktop productivity and project-management application designed to help you organize projects, tasks, daily work, Git repositories, and optional GitHub repositories from one place.

> Current version: **v0.0.1**

---

## Table of Contents

- [What is Ordeal?](#what-is-ordeal)
- [Features](#features)
- [Installation](#installation)
- [Requirements](#requirements)
- [Git and GitHub Setup](#git-and-github-setup)
- [First Run](#first-run)
- [Creating a Project](#creating-a-project)
- [Project Name and Location](#project-name-and-location)
- [Folder Structure](#folder-structure)
- [Task Hierarchy](#task-hierarchy)
- [Task Syntax](#task-syntax)
- [Task Duration](#task-duration)
- [Task Due Dates](#task-due-dates)
- [Textual and Tree Views](#textual-and-tree-views)
- [Daily Tasks](#daily-tasks)
- [Dashboard](#dashboard)
- [Project Progress](#project-progress)
- [Git Integration](#git-integration)
- [GitHub Integration](#github-integration)
- [Editing Projects](#editing-projects)
- [Renaming and Moving Projects](#renaming-and-moving-projects)
- [Removing Projects](#removing-projects)
- [Data Storage](#data-storage)
- [Do's](#dos)
- [Don'ts](#donts)
- [Troubleshooting](#troubleshooting)
- [Developer Setup](#developer-setup)
- [Building the Application](#building-the-application)
- [Running from Source](#running-from-source)
- [Publishing](#publishing)
- [Release Workflow](#release-workflow)
- [Security and Privacy](#security-and-privacy)
- [Known Limitations](#known-limitations)
- [Project Structure](#project-structure)
- [Versioning](#versioning)
- [Bug Reports](#bug-reports)
- [License](#license)

---

# What is Ordeal?

Ordeal is a desktop application for managing software projects and their tasks.

It combines:

- Project management
- Recursive task hierarchies
- Daily task tracking
- Progress tracking
- Local Git repositories
- GitHub repository creation
- Git Add / Commit / Push
- Project folder generation
- Project editing and renaming
- Local project persistence

The goal is to keep the project itself and the work required to complete it organized in one place.

---

# Features

## Project Management

Create projects with:

- Project name
- Project location
- Description
- Folder structure
- Task hierarchy
- Git initialization
- Optional GitHub repository
- GitHub visibility

---

## Recursive Task Hierarchies

Tasks can contain child tasks.

Example:

```text
Build Project : 10d
    Asset : 4d
        Images : 2d
        Icons : 2d
    Code : 6d
        Player : 3d
        Enemy : 3d
```

This allows large projects to be broken down into smaller pieces.

---

## Daily Tasks

Tasks can be selected as Daily Tasks.

Daily Tasks appear on the project dashboard.

Only **leaf tasks** can be used as Daily Tasks.

A leaf task is a task that has no children.

---

## Progress Tracking

Project progress is calculated from leaf tasks.

```text
Progress =
Completed leaf tasks
-------------------- × 100
Total leaf tasks
```

Parent task completion is derived from its child tasks.

---

## Git Integration

Ordeal can initialize Git repositories inside projects.

The dashboard provides:

```text
[Add] [Commit] [Push]
```

These correspond to the normal Git workflow:

```text
git add
git commit
git push
```

---

## GitHub Integration

Ordeal can optionally create a GitHub repository for a project.

The GitHub repository uses the project name.

Example:

```text
Project:
SpaceWar

GitHub:
https://github.com/<username>/SpaceWar
```

---

# Installation

## Linux

Download the Linux x64 release package from the GitHub Releases page.

Extract the downloaded archive and run the published application.

The Linux release is self-contained and includes the .NET runtime.

## Windows

Download the Windows x64 release package from the GitHub Releases page.

Extract the downloaded archive and run the executable.

The Windows release is self-contained and includes the .NET runtime.

---

# Requirements

For normal use, Ordeal does not require a development environment.

For Git functionality:

```bash
git --version
```

For GitHub functionality:

```bash
gh --version
```

GitHub functionality additionally requires the GitHub CLI to be authenticated.

Check authentication:

```bash
gh auth status
```

---

# Git and GitHub Setup

## Installing Git

### Ubuntu / Debian

```bash
sudo apt update
sudo apt install git
```

Verify:

```bash
git --version
```

---

## Installing GitHub CLI

Install GitHub CLI using the official installation instructions for your operating system.

Verify:

```bash
gh --version
```

---

## GitHub Login

Authenticate GitHub CLI:

```bash
gh auth login
```

Then verify:

```bash
gh auth status
```

Ordeal uses the authenticated GitHub CLI for GitHub repository operations.

---

# First Run

When starting Ordeal, the main dashboard is displayed.

From the dashboard you can:

- Create a new project
- Open existing projects
- Edit projects
- Work with Daily Tasks
- Track progress
- Perform Git operations

---

# Creating a Project

Select:

```text
+ New Project
```

You will be asked for project information.

Typical project creation flow:

```text
Create Project
      ↓
Choose Location
      ↓
Enter Project Name
      ↓
Add Description
      ↓
Define Folder Structure
      ↓
Define Task Hierarchy
      ↓
Configure Git
      ↓
Configure GitHub
      ↓
Create Project
```

---

# Project Name and Location

The final project path is:

```text
<Project Location>/<Project Name>
```

For example:

```text
/home/arpit/Projects
```

with:

```text
SpaceWar
```

creates:

```text
/home/arpit/Projects/SpaceWar
```

The parent project location must already exist.

---

# Folder Structure

The folder structure defines the directories that Ordeal creates inside the project.

Example:

```text
Assets
Assets/Images
Assets/Models
Scripts
Documentation
```

After creation, the corresponding directories are generated inside the project folder.

---

# Task Hierarchy

Tasks use indentation to represent hierarchy.

Example:

```text
Build Game : 10d
    Create Player : 3d
    Create Enemy : 4d
    Create UI : 3d
```

The indentation determines the parent-child relationship.

## TAB Indentation

A TAB represents one hierarchy level.

Example:

```text
Build Game
    Player
        Movement
        Combat
    Enemy
        AI
        Attacks
```

Hierarchy:

```text
Build Game
├── Player
│   ├── Movement
│   └── Combat
└── Enemy
    ├── AI
    └── Attacks
```

## Four Spaces

Four spaces can also represent one hierarchy level.

Example:

```text
Build Game
    Player
        Movement
```

Partial indentation is not valid.

Use consistent indentation.

---

# Task Syntax

Tasks can be written as:

```text
Task Name
```

or:

```text
Task Name : 5d
```

or:

```text
Task Name : 25/09/2026
```

Spaces around `:` are allowed.

Examples:

```text
Build Game:5d
Build Game :5d
Build Game: 5d
Build Game : 5d
```

---

# Task Duration

Duration is written using:

```text
<number>d
```

Examples:

```text
Build UI : 5d
Create Player : 3d
Testing : 10d
```

The number must be a positive integer.

### Valid

```text
5d
1d
10d
30d
```

### Invalid

```text
0d
-5d
5
five days
5 days
```

---

# Task Due Dates

Tasks can alternatively use an exact due date.

Format:

```text
DD/MM/YYYY
```

Example:

```text
Build Game : 01/10/2026
```

Another example:

```text
Testing : 23/09/2026
```

The date must be valid.

---

# Example Task Hierarchy

A complete example:

```text
Build Project : 10d
    Asset : 4d
        Images : 2d
        Icons : 2d
    Code : 6d
        Player : 3d
        Enemy : 3d
```

Or using dates:

```text
Build Project : 01/10/2026
    Asset : 25/09/2026
        Images : 23/09/2026
        Icons : 24/09/2026
    Code : 30/09/2026
        Player : 27/09/2026
        Enemy : 30/09/2026
```

---

# Textual and Tree Views

Ordeal supports working with task hierarchies as structured text.

The hierarchy is represented through indentation.

This makes it possible to quickly create or modify large task trees without manually creating every task one at a time.

---

# Daily Tasks

Daily Tasks are tasks that you want to work on from the project dashboard.

Daily Tasks are selected from the project editing interface.

Only leaf tasks can be selected as Daily Tasks.

## Important

The dashboard checkbox is used to mark a Daily Task as **completed**.

It does **not** add the task to the Daily Task list.

To add or remove a task from Daily Tasks, use the task tree editing interface.

---

# Dashboard

A project dashboard contains information such as:

```text
Project Name

Progress Bar

Completed / Total

Daily Tasks

Remaining Days

Git Status

GitHub Status

Branch

[Add] [Commit] [Push]
```

---

# Project Progress

Progress is based on leaf tasks.

Example:

```text
Total leaf tasks: 10
Completed leaf tasks: 6
```

Progress:

```text
6 / 10 × 100 = 60%
```

Parent tasks do not need to be manually marked complete.

Their state is derived from their descendants.

---

# Remaining Time

Tasks can have durations or due dates.

Ordeal displays remaining time information for applicable tasks.

Examples include:

```text
Remaining days to complete Build Game: 5
```

or:

```text
Remaining days to complete Player: 2
```

---

# Git Integration

When Git is enabled for a project, Ordeal can initialize a repository inside the project directory.

The repository is located at:

```text
<Project Path>/.git
```

## Git Add

The dashboard:

```text
[Add]
```

performs the Git staging operation.

Conceptually:

```bash
git add .
```

This moves changes into the staging area.

## Git Commit

The:

```text
[Commit]
```

button opens a commit message input.

Example:

```text
Commit message:
[Added player movement system]

[Commit]
```

Conceptually:

```bash
git commit -m "Added player movement system"
```

A commit creates a local Git checkpoint.

## Git Push

The:

```text
[Push]
```

button pushes committed changes to the configured remote repository.

Conceptually:

```bash
git push
```

---

# Git: Add vs Commit vs Push

The basic workflow is:

```text
Working Files
     ↓
   git add
     ↓
 Staging Area
     ↓
 git commit
     ↓
 Local Git History
     ↓
  git push
     ↓
 Remote Repository
```

### Add

```bash
git add .
```

Stages changes.

### Commit

```bash
git commit -m "message"
```

Creates a local Git checkpoint.

### Push

```bash
git push
```

Uploads commits to the remote repository.

---

# GitHub Integration

GitHub integration is optional.

When enabled, Ordeal can create a GitHub repository for the project.

The repository name matches the project name.

Example:

```text
Project Name:
MyGame

GitHub:
MyGame
```

---

# Creating a GitHub Project

The GitHub project creation workflow is:

```text
Create Project
      ↓
git init
      ↓
git add .
      ↓
git commit -m "Initial commit"
      ↓
git branch -M main
      ↓
gh repo create
      ↓
Push to GitHub
```

The repository can be configured as:

```text
Private
```

or:

```text
Public
```

---

# GitHub Repository Requirements

Before enabling GitHub integration:

```bash
gh --version
```

must work.

Then:

```bash
gh auth status
```

should show an authenticated GitHub account.

If GitHub CLI is not authenticated:

```bash
gh auth login
```

---

# Editing Projects

Projects can be edited after creation.

You can modify project information such as:

- Project name
- Location
- Description
- Folder structure
- Task hierarchy
- Git configuration
- GitHub configuration

---

# Renaming Projects

When a project is renamed, Ordeal can keep the different project components synchronized.

For a project with GitHub integration, the relevant components include:

```text
Database project name
        ↓
Local project folder
        ↓
GitHub repository name
        ↓
Git remote URL
```

The Git history and branch remain associated with the repository.

---

# Moving Projects

Projects can be moved to another valid project location.

The destination must be valid and must not already contain a conflicting project directory.

Ordeal updates the stored project location accordingly.

---

# Removing Projects

Project removal is a destructive operation.

Depending on the selected options, removal can involve:

- Local project files
- Git repository
- GitHub repository
- Project database records

Be careful when using the Remove Project functionality.

---

# Data Storage

Ordeal stores its application database in the user's application-data directory.

## Linux

```text
~/.config/Ordeal/ordeal.db
```

## Windows

```text
%APPDATA%\Ordeal\ordeal.db
```

The database contains project and task information.

Project files themselves remain in the project location chosen by the user.

---

# Do's

- Keep project names simple and valid.
- Use consistent task indentation.
- Use TABs or consistent four-space indentation.
- Use positive durations such as `5d`.
- Use `DD/MM/YYYY` for dates.
- Commit meaningful changes.
- Push commits when you want them on the remote repository.
- Verify GitHub authentication before using GitHub features.
- Keep backups of important projects.
- Check the project location before creating a project.
- Be careful when removing projects.

---

# Don'ts

- Use invalid project names.
- Use inconsistent indentation in task hierarchies.
- Use `0d` or negative durations.
- Use invalid date formats.
- Assume `git add` creates a commit.
- Assume `git commit` automatically pushes to GitHub.
- Delete a project directory manually without understanding the consequences.
- Remove a GitHub repository unless you are certain you want it deleted.
- Interrupt project creation unnecessarily.
- Store important information only inside a temporary project location.

---

# Troubleshooting

## Git is not detected

Check:

```bash
git --version
```

If it is not installed, install Git using your operating system's package manager.

## GitHub CLI is not detected

Check:

```bash
gh --version
```

Install GitHub CLI if necessary.

## GitHub authentication failed

Check:

```bash
gh auth status
```

If necessary:

```bash
gh auth login
```

## GitHub repository already exists

Ordeal prevents creating a second GitHub repository with the same project name when the existing repository conflicts with the requested project.

Use a different project name or manage the existing repository separately.

## Project folder already exists

Ordeal will not silently overwrite an existing project folder.

Choose another project name or location.

## Task hierarchy validation failed

Check:

- Indentation
- Parent levels
- Duration syntax
- Date format
- Empty task definitions

Example of valid hierarchy:

```text
Project
    Code
        Player
```

## Invalid Duration

Use:

```text
5d
```

instead of:

```text
5
5 days
-5d
0d
```

## Invalid Date

Use:

```text
25/09/2026
```

instead of:

```text
09/25/2026
2026-09-25
25-09-2026
```

The expected format is:

```text
DD/MM/YYYY
```

---

# Developer Setup

## Clone the Repository

```bash
git clone git@github.com:chittanshkumar1/Ordeal.git
```

Enter the project:

```bash
cd Ordeal
```

---

# Development Requirements

The development environment currently uses:

- .NET 10
- Avalonia
- SQLite
- Git
- GitHub CLI for GitHub integration

---

# Building the Application

From the repository root:

```bash
cd ~/Ordeal
```

Build:

```bash
dotnet build Ordeal.slnx
```

---

# Running from Source

Run:

```bash
dotnet run --project src/Ordeal.App
```

---

# Development Workflow

A typical development workflow is:

```text
Edit code
   ↓
dotnet build
   ↓
Run application
   ↓
Test changes
   ↓
git add
   ↓
git commit
   ↓
git push
```

---

# Publishing

For Linux:

```bash
dotnet publish src/Ordeal.App/Ordeal.App.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o releases/v0.0.1/linux-x64
```

For Windows:

```bash
dotnet publish src/Ordeal.App/Ordeal.App.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o releases/v0.0.1/win-x64
```

Release builds should not normally be committed into the source repository.

---

# Release Workflow

A typical release process is:

```text
Finish feature
      ↓
Build
      ↓
Test
      ↓
Commit
      ↓
Push
      ↓
Create version tag
      ↓
Create GitHub Release
      ↓
Attach builds
```

Example:

```bash
git tag v0.0.1
```

Push the tag:

```bash
git push origin v0.0.1
```

---

# Security and Privacy

Ordeal is designed primarily as a local desktop application.

Project information is stored locally unless the user explicitly uses functionality that communicates with an external service such as GitHub.

GitHub operations use the GitHub CLI authentication configured on the user's system.

Do not share:

- GitHub authentication tokens
- SSH private keys
- Passwords
- Personal access credentials
- Sensitive project secrets

Do not commit secrets into Git repositories.

---

# Important Git Security Rule

Never commit secrets such as:

```text
.env
API keys
Passwords
Private keys
Access tokens
Database credentials
```

Use appropriate secret-management methods instead.

Before pushing a project to GitHub, inspect the files that will be committed.

---

# Known Limitations

Current version:

```text
v0.0.1
```

is an early release.

Some functionality may continue to evolve as the application develops.

The Windows build has been published, but current development testing has primarily been performed on Linux.

Git/GitHub functionality requires Git and GitHub CLI to be installed and configured on the user's system.

---

# Project Structure

The project is organized approximately as follows:

```text
Ordeal/
├── src/
│   └── Ordeal.App/
│       ├── Data/
│       ├── Models/
│       ├── Services/
│       ├── Views/
│       ├── App.axaml
│       ├── MainWindow.axaml
│       └── MainWindow.axaml.cs
│
├── Ordeal.slnx
├── README.md
└── ...
```

---

# Versioning

Ordeal currently uses semantic-style version numbers.

Example:

```text
v0.0.1
```

The general structure is:

```text
MAJOR.MINOR.PATCH
```

During early development, breaking changes may occur between minor releases.

---

# Bug Reports

When reporting a bug, include:

1. Operating system
2. Ordeal version
3. Steps to reproduce the problem
4. Expected behavior
5. Actual behavior
6. Relevant error messages
7. Terminal output if applicable

Example:

```text
OS:
Ubuntu 26.04 LTS

Ordeal:
v0.0.1

Steps:
1. Create a project
2. Enable Git
3. Click Commit
4. Enter a message
5. Click Commit

Expected:
The commit should be created.

Actual:
An error appears.

Error:
<paste error here>
```

---

# Development Guidelines

When modifying Ordeal:

- Keep changes focused.
- Build after meaningful changes.
- Test the feature before considering it complete.
- Avoid unrelated changes.
- Preserve existing project data.
- Be careful with destructive operations.
- Validate user input.
- Handle errors instead of silently ignoring them.
- Do not assume external services are available.
- Verify Git and GitHub operations rather than only updating the UI.

---

# Git Commit Guidelines

Use clear commit messages.

Good examples:

```text
Add GitHub repository creation
```

```text
Fix project rename synchronization
```

```text
Add task hierarchy validation
```

```text
Fix dashboard progress calculation
```

Avoid vague messages such as:

```text
stuff
```

```text
changes
```

```text
update
```

---

# Basic Git Reference

Check status:

```bash
git status
```

Stage changes:

```bash
git add .
```

Commit:

```bash
git commit -m "Describe the change"
```

Push:

```bash
git push
```

View recent commits:

```bash
git log --oneline
```

Check current branch:

```bash
git branch --show-current
```

Check remotes:

```bash
git remote -v
```

---

# Basic GitHub CLI Reference

Check authentication:

```bash
gh auth status
```

View repository:

```bash
gh repo view
```

View repository information:

```bash
gh repo view --json nameWithOwner
```

Create a repository:

```bash
gh repo create
```

---

# License

A project license has not yet been defined.

Until a license is added to the repository, the project's source code should not be assumed to be freely reusable, modified, or redistributed.

---

# Current Release

## v0.0.1

Ordeal's initial public release.

GitHub Repository:

https://github.com/chittanshkumar1/Ordeal

GitHub Release:

https://github.com/chittanshkumar1/Ordeal/releases/tag/v0.0.1

---

# Ordeal

Build your project.

Break it into tasks.

Track the work.

Commit the progress.

Push when you're ready.

**Endure the Ordeal.**
