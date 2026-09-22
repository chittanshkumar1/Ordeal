using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Ordeal.App.Data;
using Ordeal.App.Models;
using Ordeal.App.Services;

namespace Ordeal.App.Views;

public partial class EditProjectWindow : Window
{
    private bool _isSavingProject;
    private bool _isRemovingProject;

    private readonly int _projectId;

    private string _originalProjectName = string.Empty;
    private string _originalProjectLocation = string.Empty;
    private string _loadedTaskHierarchy = string.Empty;

    private List<Task> _previewTasks = new();

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public EditProjectWindow()
    {
        InitializeComponent();
    }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public EditProjectWindow(int projectId)
    {
        _projectId = projectId;

        InitializeComponent();

        TaskHierarchyBox.AddHandler(
            InputElement.KeyDownEvent,
            HandleHierarchyTab,
            handledEventsToo: true);

        CreateGitHubRepositoryCheckBox.Click +=
            CreateGitHubRepositoryCheckBox_Click;

        LoadProject();
    }

    // =========================================================
    // GITHUB CHECKBOX DEPENDENCY
    // =========================================================

    private void CreateGitHubRepositoryCheckBox_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        var githubSelected =
            CreateGitHubRepositoryCheckBox.IsChecked == true;

        // Repository visibility only matters when
        // a GitHub repository is being created.
        PublicRadioButton.IsEnabled =
            githubSelected;

        PrivateRadioButton.IsEnabled =
            githubSelected;

        if (githubSelected)
        {
            // Creating a GitHub repository requires
            // a local Git repository.
            InitializeGitCheckBox.IsChecked = true;

            var projectPath =
                GetProjectPath(
                    ProjectLocationBox.Text?.Trim()
                        ?? string.Empty,
                    ProjectNameBox.Text?.Trim()
                        ?? string.Empty);

            var gitAlreadyInitialized =
                Directory.Exists(
                    Path.Combine(
                        projectPath,
                        ".git"));

            // GitHub depends on Git, so prevent the user
            // from unchecking Git while GitHub is selected.
            if (!gitAlreadyInitialized)
            {
                InitializeGitCheckBox.IsEnabled = false;
            }
        }
        else
        {
            var projectPath =
                GetProjectPath(
                    ProjectLocationBox.Text?.Trim()
                        ?? string.Empty,
                    ProjectNameBox.Text?.Trim()
                        ?? string.Empty);

            var gitAlreadyInitialized =
                Directory.Exists(
                    Path.Combine(
                        projectPath,
                        ".git"));

            // If Git already exists, keep the Git checkbox
            // disabled because Git is already initialized.
            //
            // Otherwise, allow the user to choose Git manually.
            InitializeGitCheckBox.IsEnabled =
                !gitAlreadyInitialized;
        }
    }

    // =========================================================
    // LOAD EXISTING PROJECT
    // =========================================================

    private void LoadProject()
    {
        using var db =
            new OrdealDbContext();

        db.Database.EnsureCreated();

        var project =
            db.Projects
                .FirstOrDefault(
                    item =>
                        item.Id == _projectId);

        if (project == null)
        {
            Close();
            return;
        }

        // -----------------------------------------------------
        // STORE ORIGINAL PATH INFORMATION
        // -----------------------------------------------------

        _originalProjectName =
            project.Name;

        _originalProjectLocation =
            project.Location;

        // -----------------------------------------------------
        // PROJECT FIELDS
        // -----------------------------------------------------

        ProjectNameBox.Text =
            project.Name;

        ProjectLocationBox.Text =
            project.Location;

        DescriptionBox.Text =
            project.Description;

        TaskHierarchyBox.Text =
            project.TaskHierarchy;

        _loadedTaskHierarchy =
            project.TaskHierarchy;

        // -----------------------------------------------------
        // GIT
        //
        // Actual .git folder is the source of truth for
        // whether Git has already been initialized.
        // -----------------------------------------------------

        var currentProjectPath =
            GetProjectPath(
                project.Location,
                project.Name);

        var gitPath =
            Path.Combine(
                currentProjectPath,
                ".git");

        var gitAlreadyInitialized =
            Directory.Exists(gitPath);

        InitializeGitCheckBox.IsChecked =
            gitAlreadyInitialized ||
            project.InitializeGit;

        InitializeGitCheckBox.IsEnabled =
            !gitAlreadyInitialized;

        // -----------------------------------------------------
        // GITHUB
        //
        // Existing stored DB state determines whether this
        // operation has already been completed.
        // -----------------------------------------------------

        CreateGitHubRepositoryCheckBox.IsChecked =
            project.CreateGitHubRepository;

        CreateGitHubRepositoryCheckBox.IsEnabled =
            !project.CreateGitHubRepository;

        // If GitHub has already been selected/completed,
        // Git must also be checked and locked.
        if (project.CreateGitHubRepository)
        {
            InitializeGitCheckBox.IsChecked = true;
            InitializeGitCheckBox.IsEnabled = false;
        }

        // -----------------------------------------------------
        // REPOSITORY VISIBILITY
        // -----------------------------------------------------

        if (
            string.Equals(
                project.GitHubVisibility,
                "Public",
                StringComparison.OrdinalIgnoreCase))
        {
            PublicRadioButton.IsChecked = true;
            PrivateRadioButton.IsChecked = false;
        }
        else
        {
            PrivateRadioButton.IsChecked = true;
            PublicRadioButton.IsChecked = false;
        }

        // Visibility only matters when GitHub repository
        // creation is selected.
        var githubSelected =
            CreateGitHubRepositoryCheckBox.IsChecked == true;

        PublicRadioButton.IsEnabled =
            githubSelected;

        PrivateRadioButton.IsEnabled =
            githubSelected;

        // -----------------------------------------------------
        // LOAD TASKS DIRECTLY FROM DATABASE
        // -----------------------------------------------------

        _previewTasks =
            db.Tasks
                .Where(
                    task =>
                        task.ProjectId ==
                        _projectId)
                .OrderBy(
                    task =>
                        task.Id)
                .ToList();

        // -----------------------------------------------------
        // EDIT WINDOW OPENS IN TREE MODE
        // -----------------------------------------------------

        TaskHierarchyBox.IsVisible =
            false;

        TaskTreeBorder.IsVisible =
            true;

        TextModeButton.Classes.Clear();
        TextModeButton.Classes.Add("mode");

        TreeModeButton.Classes.Clear();
        TreeModeButton.Classes.Add("mode");
        TreeModeButton.Classes.Add("active");

        BuildTaskTree(
            _previewTasks);
    }

    // =========================================================
    // PROJECT PATH
    // =========================================================

    private static string GetProjectPath(
        string location,
        string projectName)
    {
        return Path.GetFullPath(
            Path.Combine(
                location,
                projectName));
    }

    // =========================================================
    // BROWSE PROJECT LOCATION
    // =========================================================

    private async void BrowseButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        var folders =
            await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title =
                        "Select Project Location",

                    AllowMultiple =
                        false
                });

        if (folders.Count > 0)
        {
            ProjectLocationBox.Text =
                folders[0].Path.LocalPath;
        }
    }

    // =========================================================
    // TAB INSERTION FOR HIERARCHY TEXT BOX
    // =========================================================

    private static void HandleHierarchyTab(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Tab)
            return;

        if (sender is not TextBox textBox)
            return;

        var text =
            textBox.Text ??
            string.Empty;

        var caret =
            textBox.CaretIndex;

        if (caret < 0)
            caret = 0;

        if (caret > text.Length)
            caret = text.Length;

        textBox.Text =
            text.Insert(
                caret,
                "\t");

        textBox.CaretIndex =
            caret + 1;

        e.Handled = true;
    }

    // =========================================================
    // TEXTUAL / TREE MODE
    // =========================================================

    private void TextModeButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        TaskTreeBorder.IsVisible =
            false;

        TaskHierarchyBox.IsVisible =
            true;

        TextModeButton.Classes.Clear();
        TextModeButton.Classes.Add("mode");
        TextModeButton.Classes.Add("active");

        TreeModeButton.Classes.Clear();
        TreeModeButton.Classes.Add("mode");
    }

    private void TreeModeButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        BuildTaskTree(
            _previewTasks);

        TaskHierarchyBox.IsVisible =
            false;

        TaskTreeBorder.IsVisible =
            true;

        TreeModeButton.Classes.Clear();
        TreeModeButton.Classes.Add("mode");
        TreeModeButton.Classes.Add("active");

        TextModeButton.Classes.Clear();
        TextModeButton.Classes.Add("mode");
    }

    // =========================================================
    // BUILD VISUAL TASK TREE
    // =========================================================

    private void BuildTaskTree(
        List<Task> tasks)
    {
        TaskTreePanel.Children.Clear();

        if (tasks.Count == 0)
        {
            TaskTreePanel.Children.Add(
                new TextBlock
                {
                    Text =
                        "No tasks to display.",

                    FontSize =
                        14,

                    Foreground =
                        Brushes.Gray
                });

            return;
        }

        foreach (var task in tasks)
        {
            var level =
                GetTaskLevel(
                    tasks,
                    task);

            var isLeaf =
                IsLeafTask(
                    tasks,
                    task);

            // -------------------------------------------------
            // TASK ROW
            // -------------------------------------------------

            var row =
                new StackPanel
                {
                    Orientation =
                        Avalonia.Layout.Orientation.Horizontal,

                    Spacing =
                        6,

                    Margin =
                        new Avalonia.Thickness(
                            0,
                            2,
                            0,
                            2)
                };

            // -------------------------------------------------
            // COMPLETION CHECKBOX
            // -------------------------------------------------

            var checkBox =
                new CheckBox
                {
                    IsChecked =
                        GetTaskCompletion(
                            tasks,
                            task),

                    IsEnabled =
                        isLeaf,

                    VerticalAlignment =
                        Avalonia.Layout.VerticalAlignment.Center
                };

            checkBox.Click += (_, _) =>
            {
                if (!isLeaf)
                    return;

                task.IsCompleted =
                    checkBox.IsChecked == true;

                BuildTaskTree(
                    _previewTasks);
            };

            row.Children.Add(
                checkBox);

            // -------------------------------------------------
            // TASK NAME
            // -------------------------------------------------

            var connector =
                level == 0
                    ? string.Empty
                    : "└─ ";

            var taskText =
                new TextBlock
                {
                    Text =
                        connector +
                        task.Name +
                        FormatTaskTime(task),

                    FontSize =
                        14,

                    Foreground =
                        Brushes.White,

                    Margin =
                        new Avalonia.Thickness(
                            level * 24,
                            0,
                            0,
                            0),

                    VerticalAlignment =
                        Avalonia.Layout.VerticalAlignment.Center
                };

            row.Children.Add(
                taskText);

            // -------------------------------------------------
            // DAILY TASK "+"
            // ONLY LEAF TASKS
            // -------------------------------------------------

            if (isLeaf)
            {
                var dailyButton =
                    new Button
                    {
                        Content =
                            "+",

                        Width =
                            26,

                        Height =
                            26,

                        Padding =
                            new Avalonia.Thickness(
                                0),

                        FontSize =
                            16,

                        VerticalContentAlignment =
                            Avalonia.Layout.VerticalAlignment.Center,

                        HorizontalContentAlignment =
                            Avalonia.Layout.HorizontalAlignment.Center,

                        Opacity =
                            task.IsInDailyTasks
                                ? 0.45
                                : 1.0
                    };

                dailyButton.Click += (_, _) =>
                {
                    task.IsInDailyTasks =
                        !task.IsInDailyTasks;

                    BuildTaskTree(
                        _previewTasks);
                };

                row.Children.Add(
                    dailyButton);
            }

            TaskTreePanel.Children.Add(
                row);
        }
    }

    // =========================================================
    // DETERMINE WHETHER TASK IS A LEAF
    // =========================================================

    private static bool IsLeafTask(
        List<Task> tasks,
        Task task)
    {
        return !tasks.Any(
            child =>
                child.ParentTaskId ==
                task.Id);
    }

    // =========================================================
    // CALCULATE TASK COMPLETION
    // =========================================================

    private static bool GetTaskCompletion(
        List<Task> tasks,
        Task task)
    {
        if (
            IsLeafTask(
                tasks,
                task))
        {
            return task.IsCompleted;
        }

        var leafDescendants =
            GetLeafDescendants(
                tasks,
                task);

        if (leafDescendants.Count == 0)
            return false;

        return leafDescendants.All(
            child =>
                child.IsCompleted);
    }

    // =========================================================
    // FIND LEAF DESCENDANTS
    // =========================================================

    private static List<Task> GetLeafDescendants(
        List<Task> tasks,
        Task parent)
    {
        var result =
            new List<Task>();

        foreach (var task in tasks)
        {
            if (
                task.ParentTaskId !=
                parent.Id)
            {
                continue;
            }

            if (
                IsLeafTask(
                    tasks,
                    task))
            {
                result.Add(task);
            }
            else
            {
                result.AddRange(
                    GetLeafDescendants(
                        tasks,
                        task));
            }
        }

        return result;
    }

    // =========================================================
    // FORMAT TASK TIME
    // =========================================================

    private static string FormatTaskTime(
        Task task)
    {
        if (
            task.TimeType ==
                "Duration" &&
            task.DurationDays.HasValue)
        {
            return
                $" : {task.DurationDays.Value}d";
        }

        if (
            task.TimeType ==
                "Date" &&
            task.DueDate.HasValue)
        {
            return
                $" : {task.DueDate.Value:dd/MM/yyyy}";
        }

        return string.Empty;
    }

    // =========================================================
    // GET TREE LEVEL
    // =========================================================

    private static int GetTaskLevel(
        List<Task> tasks,
        Task task)
    {
        var level =
            0;

        var currentParentId =
            task.ParentTaskId;

        var safety =
            0;

        while (
            currentParentId.HasValue &&
            safety < tasks.Count)
        {
            var parent =
                tasks.Find(
                    item =>
                        item.Id ==
                        currentParentId.Value);

            if (parent == null)
                break;

            level++;

            currentParentId =
                parent.ParentTaskId;

            safety++;
        }

        return level;
    }

    // =========================================================
    // CHECK WHETHER TEXTUAL TASK HIERARCHY CHANGED
    // =========================================================

    private bool TaskHierarchyWasEdited()
    {
        var currentText =
            TaskHierarchyBox.Text ??
            string.Empty;

        return !string.Equals(
            currentText,
            _loadedTaskHierarchy,
            StringComparison.Ordinal);
    }

    // =========================================================
    // CREATE TASK PATH KEY
    //
    // Used to match existing tasks to tasks from an edited
    // textual hierarchy while preserving completion and
    // Daily Task state where the hierarchy/name still matches.
    //
    // =========================================================

    private static string GetTaskPathKey(
        List<Task> tasks,
        Task task)
    {
        var names =
            new Stack<string>();

        var current =
            task;

        var safety =
            0;

        while (
            current != null &&
            safety < tasks.Count)
        {
            names.Push(
                current.Name.Trim());

            if (!current.ParentTaskId.HasValue)
                break;

            current =
                tasks.FirstOrDefault(
                    item =>
                        item.Id ==
                        current.ParentTaskId.Value);

            safety++;
        }

        return string.Join(
            "\u001F",
            names);
    }

    // =========================================================
    // BUILD MATCHING QUEUES
    // =========================================================

    private static Dictionary<string, Queue<Task>>
        BuildTaskMatchQueues(
            List<Task> tasks)
    {
        var result =
            new Dictionary<
                string,
                Queue<Task>>();

        foreach (var task in tasks)
        {
            var key =
                GetTaskPathKey(
                    tasks,
                    task);

            if (!result.TryGetValue(
                    key,
                    out var queue))
            {
                queue =
                    new Queue<Task>();

                result[key] =
                    queue;
            }

            queue.Enqueue(
                task);
        }

        return result;
    }

    // =========================================================
    // UPDATE EXISTING TASK STATES
    //
    // Used when textual hierarchy was NOT changed.
    // This preserves the exact DB task IDs and hierarchy.
    // =========================================================

    private static void SaveExistingTaskStates(
        OrdealDbContext db,
        List<Task> previewTasks)
    {
        var databaseTasks =
            db.Tasks
                .Where(
                    task =>
                        previewTasks
                            .Select(
                                preview =>
                                    preview.Id)
                            .Contains(task.Id))
                .ToList();

        foreach (var databaseTask in databaseTasks)
        {
            var previewTask =
                previewTasks.FirstOrDefault(
                    item =>
                        item.Id ==
                        databaseTask.Id);

            if (previewTask == null)
                continue;

            databaseTask.IsCompleted =
                previewTask.IsCompleted;

            databaseTask.IsInDailyTasks =
                previewTask.IsInDailyTasks;
        }
    }

    // =========================================================
    // RECONCILE EDITED TEXTUAL TASK HIERARCHY
    // =========================================================

    private static void ReconcileTextualTasks(
        OrdealDbContext db,
        int projectId,
        string hierarchyText,
        List<Task> stateTasks)
    {
        var parsedTasks =
            TaskHierarchyParser.Parse(
                hierarchyText,
                projectId);

        var databaseTasks =
            db.Tasks
                .Where(
                    task =>
                        task.ProjectId ==
                        projectId)
                .ToList();

        var existingById =
            databaseTasks.ToDictionary(
                task =>
                    task.Id);

        var matchQueues =
            BuildTaskMatchQueues(
                stateTasks);

        var parsedToDatabase =
            new Dictionary<
                int,
                Task>();

        var usedDatabaseIds =
            new HashSet<int>();

        // -----------------------------------------------------
        // MATCH / UPDATE / ADD TASKS
        // -----------------------------------------------------

        foreach (var parsedTask in parsedTasks)
        {
            var key =
                GetTaskPathKey(
                    parsedTasks,
                    parsedTask);

            Task? stateTask =
                null;

            if (
                matchQueues.TryGetValue(
                    key,
                    out var queue) &&
                queue.Count > 0)
            {
                stateTask =
                    queue.Dequeue();
            }

            Task databaseTask;

            if (
                stateTask != null &&
                existingById.TryGetValue(
                    stateTask.Id,
                    out var existingTask) &&
                !usedDatabaseIds.Contains(
                    existingTask.Id))
            {
                databaseTask =
                    existingTask;

                usedDatabaseIds.Add(
                    databaseTask.Id);

                // Preserve current completion and
                // Daily Task state.
                databaseTask.IsCompleted =
                    stateTask.IsCompleted;

                databaseTask.IsInDailyTasks =
                    stateTask.IsInDailyTasks;
            }
            else
            {
                databaseTask =
                    new Task
                    {
                        ProjectId =
                            projectId,

                        IsCompleted =
                            false,

                        IsInDailyTasks =
                            false
                    };

                db.Tasks.Add(
                    databaseTask);
            }

            databaseTask.Name =
                parsedTask.Name;

            databaseTask.TimeType =
                parsedTask.TimeType;

            databaseTask.DurationDays =
                parsedTask.DurationDays;

            databaseTask.DueDate =
                parsedTask.DueDate;

            databaseTask.ProjectId =
                projectId;

            databaseTask.ParentTaskId =
                null;

            parsedToDatabase[
                parsedTask.Id] =
                databaseTask;
        }

        // -----------------------------------------------------
        // DETACH OLD PARENT REFERENCES FIRST
        //
        // This makes deletion of removed parent tasks safe.
        // -----------------------------------------------------

        foreach (var databaseTask in databaseTasks)
        {
            databaseTask.ParentTaskId =
                null;
        }

        // -----------------------------------------------------
        // REMOVE TASKS THAT NO LONGER EXIST
        // -----------------------------------------------------

        foreach (var databaseTask in databaseTasks)
        {
            if (
                !usedDatabaseIds.Contains(
                    databaseTask.Id))
            {
                db.Tasks.Remove(
                    databaseTask);
            }
        }

        // -----------------------------------------------------
        // SAVE SO NEW TASKS RECEIVE THEIR IDs
        // -----------------------------------------------------

        db.SaveChanges();

        // -----------------------------------------------------
        // RESTORE PARENT RELATIONSHIPS
        // -----------------------------------------------------

        foreach (var parsedTask in parsedTasks)
        {
            if (
                !parsedToDatabase.TryGetValue(
                    parsedTask.Id,
                    out var databaseTask))
            {
                continue;
            }

            if (
                parsedTask.ParentTaskId.HasValue &&
                parsedToDatabase.TryGetValue(
                    parsedTask.ParentTaskId.Value,
                    out var databaseParent))
            {
                databaseTask.ParentTaskId =
                    databaseParent.Id;
            }
            else
            {
                databaseTask.ParentTaskId =
                    null;
            }
        }

        db.SaveChanges();
    }

    // =========================================================
    // VALIDATE PROJECT NAME
    // =========================================================

    private static bool IsValidProjectName(
        string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (
            name == "." ||
            name == "..")
        {
            return false;
        }

        var invalidCharacters =
            Path.GetInvalidFileNameChars();

        return !name.Any(
            character =>
                invalidCharacters.Contains(
                    character));
    }

    // =========================================================
    // RUN GIT INIT
    // =========================================================

    private static void InitializeGitRepository(
        string projectPath)
    {
        var startInfo =
            new ProcessStartInfo
            {
                FileName =
                    "git",

                WorkingDirectory =
                    projectPath,

                UseShellExecute =
                    false,

                RedirectStandardOutput =
                    true,

                RedirectStandardError =
                    true,

                CreateNoWindow =
                    true
            };

        startInfo.ArgumentList.Add(
            "init");

        using var process =
            Process.Start(
                startInfo);

        if (process == null)
        {
            throw new InvalidOperationException(
                "Unable to start git.");
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error =
                process.StandardError
                    .ReadToEnd();

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? "git init failed."
                    : error.Trim());
        }
    }

    // =========================================================
    // CREATE GITHUB REPOSITORY
    //
    // Creates the repository and connects it as "origin".
    //
    // IMPORTANT:
    // No --push is used here.
    //
    // =========================================================


    // =========================================================
    // RUN GIT COMMAND
    // =========================================================

    private static void RunGitCommand(
        string projectPath,
        params string[] arguments)
    {
        var startInfo =
            new ProcessStartInfo
            {
                FileName = "git",

                WorkingDirectory =
                    projectPath,

                UseShellExecute =
                    false,

                RedirectStandardOutput =
                    true,

                RedirectStandardError =
                    true,

                CreateNoWindow =
                    true
            };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process =
            Process.Start(startInfo);

        if (process == null)
        {
            throw new InvalidOperationException(
                "Unable to start git.");
        }

        var output =
            process.StandardOutput
                .ReadToEnd();

        var error =
            process.StandardError
                .ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? $"git {string.Join(" ", arguments)} failed."
                    : error.Trim());
        }

        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine(output.Trim());
        }
    }

    // =========================================================
    // CHECK WHETHER GIT ALREADY HAS A COMMIT
    // =========================================================

    private static bool GitHasCommit(
        string projectPath)
    {
        var startInfo =
            new ProcessStartInfo
            {
                FileName = "git",

                WorkingDirectory =
                    projectPath,

                UseShellExecute =
                    false,

                RedirectStandardOutput =
                    true,

                RedirectStandardError =
                    true,

                CreateNoWindow =
                    true
            };

        startInfo.ArgumentList.Add("rev-parse");
        startInfo.ArgumentList.Add("--verify");
        startInfo.ArgumentList.Add("HEAD");

        using var process =
            Process.Start(startInfo);

        if (process == null)
        {
            throw new InvalidOperationException(
                "Unable to start git.");
        }

        process.WaitForExit();

        process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();

        return process.ExitCode == 0;
    }

    private static void CreateGitHubRepository(
        string projectPath,
        string projectName,
        string visibility)
    {
        // -----------------------------------------------------
        // Prepare the local Git repository.
        //
        // Add every project file.
        // If this repository has no commit yet, create the
        // initial commit.
        // -----------------------------------------------------

        RunGitCommand(
            projectPath,
            "add",
            ".");

        if (!GitHasCommit(projectPath))
        {
            RunGitCommand(
                projectPath,
                "commit",
                "-m",
                "Initial commit");
        }

        // GitHub should use main as the default local branch.
        RunGitCommand(
            projectPath,
            "branch",
            "-M",
            "main");

        var startInfo =
            new ProcessStartInfo
            {
                FileName =
                    "gh",

                WorkingDirectory =
                    projectPath,

                UseShellExecute =
                    false,

                RedirectStandardOutput =
                    true,

                RedirectStandardError =
                    true,

                CreateNoWindow =
                    true
            };

        startInfo.ArgumentList.Add(
            "repo");

        startInfo.ArgumentList.Add(
            "create");

        startInfo.ArgumentList.Add(
            projectName);

        startInfo.ArgumentList.Add(
            "--source");

        startInfo.ArgumentList.Add(
            ".");

        startInfo.ArgumentList.Add(
            "--remote");

        startInfo.ArgumentList.Add(
            "origin");

        startInfo.ArgumentList.Add(
            visibility == "public"
                ? "--public"
                : "--private");

        // Push the local main branch immediately.
        startInfo.ArgumentList.Add(
            "--push");

        using var process =
            Process.Start(
                startInfo);

        if (process == null)
        {
            throw new InvalidOperationException(
                "Unable to start GitHub CLI.");
        }

        process.WaitForExit();

        var output =
            process.StandardOutput
                .ReadToEnd();

        var error =
            process.StandardError
                .ReadToEnd();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? "GitHub repository creation failed."
                    : error.Trim());
        }
    }

// =========================================================
// GET GIT REMOTE URL
// =========================================================

private static string GetGitRemoteUrl(
    string projectPath)
{
    var startInfo =
        new ProcessStartInfo
        {
            FileName = "git",

            UseShellExecute = false,

            RedirectStandardOutput = true,

            RedirectStandardError = true,

            CreateNoWindow = true,

            WorkingDirectory = projectPath
        };

    startInfo.ArgumentList.Add(
        "remote");

    startInfo.ArgumentList.Add(
        "get-url");

    startInfo.ArgumentList.Add(
        "origin");

    using var process =
        Process.Start(startInfo);

    if (process == null)
    {
        throw new InvalidOperationException(
            "Could not start Git.");
    }

    var output =
        process.StandardOutput
            .ReadToEnd();

    var error =
        process.StandardError
            .ReadToEnd();

    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(error)
                ? "Could not read the GitHub remote URL."
                : error.Trim());
    }

    return output.Trim();
}

// =========================================================
// SET GIT REMOTE URL
// =========================================================

private static void SetGitRemoteUrl(
    string projectPath,
    string remoteUrl)
{
    RunGitCommand(
        projectPath,
        "remote",
        "set-url",
        "origin",
        remoteUrl);
}

// =========================================================
// BUILD RENAMED GITHUB REMOTE URL
// =========================================================

private static string BuildRenamedGitRemoteUrl(
    string oldRemoteUrl,
    string newRepositoryName)
{
    if (oldRemoteUrl.StartsWith(
            "git@github.com:",
            StringComparison.Ordinal))
    {
        var prefix =
            "git@github.com:";

        var repositoryPath =
            oldRemoteUrl[prefix.Length..];

        var slashIndex =
            repositoryPath.IndexOf('/');

        if (slashIndex < 0)
        {
            throw new InvalidOperationException(
                "The GitHub SSH remote URL is invalid.");
        }

        var owner =
            repositoryPath[..slashIndex];

        return
            $"{prefix}{owner}/{newRepositoryName}.git";
    }

    if (oldRemoteUrl.StartsWith(
            "https://github.com/",
            StringComparison.Ordinal))
    {
        var prefix =
            "https://github.com/";

        var repositoryPath =
            oldRemoteUrl[prefix.Length..];

        var slashIndex =
            repositoryPath.IndexOf('/');

        if (slashIndex < 0)
        {
            throw new InvalidOperationException(
                "The GitHub HTTPS remote URL is invalid.");
        }

        var owner =
            repositoryPath[..slashIndex];

        return
            $"{prefix}{owner}/{newRepositoryName}.git";
    }

    throw new InvalidOperationException(
        "The project does not use a supported GitHub remote URL.");
}

// =========================================================
// RENAME GITHUB REPOSITORY
// =========================================================

private static void RenameGitHubRepository(
    string repositoryName,
    string newRepositoryName)
{
    var startInfo =
        new ProcessStartInfo
        {
            FileName = "gh",

            UseShellExecute = false,

            RedirectStandardOutput = true,

            RedirectStandardError = true,

            CreateNoWindow = true
        };

    startInfo.ArgumentList.Add(
        "repo");

    startInfo.ArgumentList.Add(
        "rename");

    startInfo.ArgumentList.Add(
        newRepositoryName);

    startInfo.ArgumentList.Add(
        "--repo");

    startInfo.ArgumentList.Add(
        repositoryName);

    using var process =
        Process.Start(startInfo);

    if (process == null)
    {
        throw new InvalidOperationException(
            "Could not start GitHub CLI.");
    }

    var output =
        process.StandardOutput
            .ReadToEnd();

    var error =
        process.StandardError
            .ReadToEnd();

    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(error)
                ? "Could not rename the GitHub repository."
                : error.Trim());
    }

    if (!string.IsNullOrWhiteSpace(output))
    {
        Console.WriteLine(
            output.Trim());
    }
}

// =========================================================
// GET GITHUB REPOSITORY
// =========================================================

private static string GetGitHubRepositoryName(
    string projectPath)
{
    var startInfo =
        new ProcessStartInfo
        {
            FileName = "gh",

            WorkingDirectory =
                projectPath,

            UseShellExecute = false,

            RedirectStandardOutput = true,

            RedirectStandardError = true,

            CreateNoWindow = true
        };

    startInfo.ArgumentList.Add(
        "repo");

    startInfo.ArgumentList.Add(
        "view");

    startInfo.ArgumentList.Add(
        "--json");

    startInfo.ArgumentList.Add(
        "nameWithOwner");

    startInfo.ArgumentList.Add(
        "--jq");

    startInfo.ArgumentList.Add(
        ".nameWithOwner");

    using var process =
        Process.Start(startInfo);

    if (process == null)
    {
        throw new InvalidOperationException(
            "Could not start GitHub CLI.");
    }

    var output =
        process.StandardOutput
            .ReadToEnd()
            .Trim();

    var error =
        process.StandardError
            .ReadToEnd()
            .Trim();

    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(error)
                ? "Could not determine the GitHub repository."
                : error);
    }

    return output;
}


// =========================================================
// FIND GITHUB REPOSITORY WITHOUT LOCAL PROJECT
// =========================================================

private static string? GetGitHubRepositoryNameFromGitHub(
    string projectName)
{
    var startInfo =
        new ProcessStartInfo
        {
            FileName = "gh",

            UseShellExecute = false,

            RedirectStandardOutput = true,

            RedirectStandardError = true,

            CreateNoWindow = true
        };

    startInfo.ArgumentList.Add(
        "repo");

    startInfo.ArgumentList.Add(
        "view");

    startInfo.ArgumentList.Add(
        projectName);

    startInfo.ArgumentList.Add(
        "--json");

    startInfo.ArgumentList.Add(
        "nameWithOwner");

    startInfo.ArgumentList.Add(
        "--jq");

    startInfo.ArgumentList.Add(
        ".nameWithOwner");

    try
    {
        using var process =
            Process.Start(startInfo);

        if (process == null)
        {
            return null;
        }

        var output =
            process.StandardOutput
                .ReadToEnd()
                .Trim();

        var error =
            process.StandardError
                .ReadToEnd()
                .Trim();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine(
                string.IsNullOrWhiteSpace(error)
                    ? "Could not find the GitHub repository."
                    : error);

            return null;
        }

        return string.IsNullOrWhiteSpace(output)
            ? null
            : output;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(
            $"Could not query GitHub: {ex.Message}");

        return null;
    }
}

// =========================================================
// DELETE GITHUB REPOSITORY
// =========================================================

private static void DeleteGitHubRepository(
    string repositoryName)
{
    var startInfo =
        new ProcessStartInfo
        {
            FileName = "gh",

            UseShellExecute = false,

            RedirectStandardOutput = true,

            RedirectStandardError = true,

            CreateNoWindow = true
        };

    startInfo.ArgumentList.Add(
        "repo");

    startInfo.ArgumentList.Add(
        "delete");

    startInfo.ArgumentList.Add(
        repositoryName);

    startInfo.ArgumentList.Add(
        "--yes");

    using var process =
        Process.Start(startInfo);

    if (process == null)
    {
        throw new InvalidOperationException(
            "Could not start GitHub CLI.");
    }

    var output =
        process.StandardOutput
            .ReadToEnd();

    var error =
        process.StandardError
            .ReadToEnd();

    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(error)
                ? "Could not delete the GitHub repository."
                : error.Trim());
    }

    Console.WriteLine(
        output.Trim());
}

    // =========================================================
    // REMOVE PROJECT
    //
    // Currently this ONLY opens the confirmation dialog.
    // Actual deletion will be added after we verify the dialog.
    //
    // =========================================================

private async void RemoveProjectButton_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
{
    if (_isRemovingProject)
        return;

    using var db =
        new OrdealDbContext();

    db.Database.EnsureCreated();

    var project =
        db.Projects
            .FirstOrDefault(
                item =>
                    item.Id == _projectId);

    if (project == null)
    {
        Close();
        return;
    }

    var projectPath =
        GetProjectPath(
            project.Location,
            project.Name);

    var dialog =
        new RemoveProjectDialog(
            project.Name,
            projectPath,
            project.CreateGitHubRepository);

    await dialog.ShowDialog(this);

    if (!dialog.Confirmed)
    {
        return;
    }

    _isRemovingProject = true;

    var removeGitHubRepository =
        dialog.RemoveGitRepository;

    string? githubRepository = null;

    try
    {
        // =====================================================
        // GITHUB REPOSITORY
        // =====================================================

        // Determine the actual GitHub repository before
        // deleting the local project.
        //
        // This gives us the owner/repository name needed
        // by `gh repo delete`.
        if (removeGitHubRepository)
        {
            // If the local project still exists, read the
            // Git remote directly from the local repository.
            if (Directory.Exists(projectPath))
            {
                githubRepository =
                    GetGitHubRepositoryName(projectPath);
            }
            else
            {
                // The local folder may have been deleted or
                // moved outside Ordeal. In that case, recover
                // the repository directly from GitHub using
                // the project name.
                githubRepository =
                    GetGitHubRepositoryNameFromGitHub(
                        project.Name);
            }

            if (string.IsNullOrWhiteSpace(githubRepository))
            {
                throw new InvalidOperationException(
                    "Could not determine the GitHub repository connected to this project.");
            }
        }

        // =====================================================
        // GITHUB REPOSITORY
        //
        // Delete the remote repository FIRST.
        //
        // If GitHub rejects the deletion, the local project
        // and database must remain untouched.
        // =====================================================

        if (removeGitHubRepository &&
            !string.IsNullOrWhiteSpace(githubRepository))
        {
            DeleteGitHubRepository(
                githubRepository);
        }

        // =====================================================
        // LOCAL PROJECT FOLDER
        //
        // Only remove the local project after any requested
        // GitHub deletion has succeeded.
        // =====================================================

        if (Directory.Exists(projectPath))
        {
            Directory.Delete(
                projectPath,
                recursive: true);
        }

        // =====================================================
        // DATABASE
        // =====================================================

        var tasks =
            db.Tasks
                .Where(
                    task =>
                        task.ProjectId == _projectId)
                .ToList();

        db.Tasks.RemoveRange(tasks);

        db.Projects.Remove(project);

        db.SaveChanges();

        // =====================================================
        // DONE
        // =====================================================

        Close();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(
            $"Could not remove project: {ex}");

        _isRemovingProject = false;

        await ShowRemoveError(
            ex.Message);
    }
}

    // =========================================================
    // SAVE PROJECT
    // =========================================================

    private async void SaveProjectButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        var projectName =
            ProjectNameBox.Text?.Trim()
            ?? string.Empty;

        var projectLocation =
            ProjectLocationBox.Text?.Trim()
            ?? string.Empty;

        var description =
            DescriptionBox.Text
            ?? string.Empty;

        var taskHierarchy =
            TaskHierarchyBox.Text
            ?? string.Empty;

        // -----------------------------------------------------
        // VALIDATE TASK HIERARCHY
        // -----------------------------------------------------

        var taskValidation =
            TaskHierarchyParser.TryParse(
                taskHierarchy,
                0);

        if (!taskValidation.IsValid)
        {
            await ShowError(
                "Invalid Task Hierarchy",
                taskValidation.ErrorMessage);

            return;
        }

        // -----------------------------------------------------
        // VALIDATION
        // -----------------------------------------------------

        if (!IsValidProjectName(projectName))
        {
            Console.Error.WriteLine(
                "Invalid project name.");

            return;
        }

        if (string.IsNullOrWhiteSpace(
                projectLocation))
        {
            Console.Error.WriteLine(
                "Project location cannot be empty.");

            return;
        }

        if (!Directory.Exists(
                projectLocation))
        {
            Console.Error.WriteLine(
                "Project location does not exist.");

            return;
        }

        var originalProjectPath =
            GetProjectPath(
                _originalProjectLocation,
                _originalProjectName);

        var finalProjectPath =
            GetProjectPath(
                projectLocation,
                projectName);

        var pathsAreDifferent =
            !string.Equals(
                originalProjectPath,
                finalProjectPath,
                StringComparison.Ordinal);

        // Prevent moving the project inside itself.
        var originalWithSeparator =
            originalProjectPath
            + Path.DirectorySeparatorChar;

        if (
            pathsAreDifferent &&
            finalProjectPath.StartsWith(
                originalWithSeparator,
                StringComparison.Ordinal))
        {
            Console.Error.WriteLine(
                "The project cannot be moved inside itself.");

            return;
        }

        var finalGitPath =
            Path.Combine(
                finalProjectPath,
                ".git");

        var gitAlreadyExistsAtFinalPath =
            Directory.Exists(
                finalGitPath);

        var requestedGit =
            InitializeGitCheckBox.IsChecked == true;

        var shouldInitializeGit =
            requestedGit &&
            !gitAlreadyExistsAtFinalPath;

        var visibility =
            PublicRadioButton.IsChecked == true
                ? "Public"
                : "Private";

        if (_isSavingProject)
            return;

        _isSavingProject = true;

        var textualHierarchyWasEdited =
            TaskHierarchyWasEdited();

        var moved =
            false;

        var gitInitializedBySave =
            false;

        var githubCreatedBySave =
            false;

        var githubRenamedBySave =
            false;

        string? originalGitHubRepository =
            null;

        string? originalGitRemoteUrl =
            null;

        string? renamedGitRemoteUrl =
            null;

        var gitRemoteChangedBySave =
            false;

        try
        {
            // =================================================
            // LOAD CURRENT DATABASE STATE
            // =================================================

            using var db =
                new OrdealDbContext();

            db.Database.EnsureCreated();

            var project =
                db.Projects
                    .FirstOrDefault(
                        item =>
                            item.Id ==
                            _projectId);

            if (project == null)
            {
                throw new InvalidOperationException(
                    "Project no longer exists in the database.");
            }

            // -------------------------------------------------
            // IMPORTANT:
            // Only create a GitHub repository if it has NOT
            // already been created for this project.
            // -------------------------------------------------

            var githubAlreadyCreated =
                project.CreateGitHubRepository;

            var shouldCreateGitHub =
                CreateGitHubRepositoryCheckBox.IsChecked == true &&
                !githubAlreadyCreated;

            // =================================================
            // GITHUB REPOSITORY RENAME
            // =================================================
            //
            // If the project already has a GitHub repository
            // and its name is changing, determine the actual
            // repository from the local Git configuration.
            //
            // We do NOT infer the old repository name from the
            // current project name.
            // =================================================

            var projectNameChanged =
                !string.Equals(
                    _originalProjectName,
                    projectName,
                    StringComparison.Ordinal);

            if (
                projectNameChanged &&
                githubAlreadyCreated)
            {
                if (!Directory.Exists(
                        originalProjectPath))
                {
                    throw new DirectoryNotFoundException(
                        $"Project folder not found: {originalProjectPath}");
                }

                originalGitHubRepository =
                    GetGitHubRepositoryName(
                        originalProjectPath);

                if (string.IsNullOrWhiteSpace(
                        originalGitHubRepository))
                {
                    throw new InvalidOperationException(
                        "Could not determine the GitHub repository connected to this project.");
                }

                // The new repository name must not already exist
                // on GitHub.
                if (
                    GitHubService.RepositoryExists(
                        projectName))
                {
                    throw new InvalidOperationException(
                        $"A GitHub repository named '{projectName}' already exists.");
                }

                originalGitRemoteUrl =
                    GetGitRemoteUrl(
                        originalProjectPath);

                renamedGitRemoteUrl =
                    BuildRenamedGitRemoteUrl(
                        originalGitRemoteUrl,
                        projectName);

                RenameGitHubRepository(
                    originalGitHubRepository,
                    projectName);

                githubRenamedBySave =
                    true;

                SetGitRemoteUrl(
                    originalProjectPath,
                    renamedGitRemoteUrl);

                gitRemoteChangedBySave =
                    true;
            }

            // =================================================
            // FILESYSTEM MOVE / RENAME
            // =================================================

            if (pathsAreDifferent)
            {
                if (!Directory.Exists(
                        originalProjectPath))
                {
                    throw new DirectoryNotFoundException(
                        $"Project folder not found: {originalProjectPath}");
                }

                if (Directory.Exists(
                        finalProjectPath))
                {
                    throw new IOException(
                        $"Destination project folder already exists: {finalProjectPath}");
                }

                Directory.CreateDirectory(
                    projectLocation);

                Directory.Move(
                    originalProjectPath,
                    finalProjectPath);

                moved = true;
            }

            // =================================================
            // GIT INITIALIZATION
            // =================================================

            if (shouldInitializeGit)
            {
                if (!Directory.Exists(
                        finalProjectPath))
                {
                    throw new DirectoryNotFoundException(
                        $"Project folder not found: {finalProjectPath}");
                }

                InitializeGitRepository(
                    finalProjectPath);

                gitInitializedBySave =
                    true;
            }

            // =================================================
            // GITHUB REPOSITORY CREATION
            // =================================================

            if (shouldCreateGitHub)
            {
                if (!Directory.Exists(
                        finalProjectPath))
                {
                    throw new DirectoryNotFoundException(
                        $"Project folder not found: {finalProjectPath}");
                }

                var finalGitDirectory =
                    Path.Combine(
                        finalProjectPath,
                        ".git");

                if (!Directory.Exists(
                        finalGitDirectory))
                {
                    throw new InvalidOperationException(
                        "GitHub repository creation requires a local Git repository.");
                }

                CreateGitHubRepository(
                    finalProjectPath,
                    projectName,
                    visibility);

                githubCreatedBySave =
                    true;
            }

            // =================================================
            // DATABASE
            // =================================================

            using var transaction =
                db.Database.BeginTransaction();

            // -------------------------------------------------
            // PROJECT FIELDS
            // -------------------------------------------------

            project.Name =
                projectName;

            project.Location =
                projectLocation;

            project.Description =
                description;

            project.GitHubVisibility =
                visibility;

            // The actual .git directory represents the
            // real Git state.
            project.InitializeGit =
                Directory.Exists(
                    Path.Combine(
                        finalProjectPath,
                        ".git"));

            // If GitHub was already created before this save,
            // keep that state.
            //
            // If GitHub was newly created during this save,
            // store the new state.
            project.CreateGitHubRepository =
                githubAlreadyCreated ||
                githubCreatedBySave;

            // -------------------------------------------------
            // TASK HIERARCHY
            // -------------------------------------------------

            project.TaskHierarchy =
                taskHierarchy;

            if (textualHierarchyWasEdited)
            {
                ReconcileTextualTasks(
                    db,
                    _projectId,
                    taskHierarchy,
                    _previewTasks);
            }
            else
            {
                SaveExistingTaskStates(
                    db,
                    _previewTasks);
            }

            db.SaveChanges();

            transaction.Commit();

            // =================================================
            // SUCCESS
            // =================================================

            _isSavingProject = false;

            Close();
        }
        catch (Exception exception)
        {
            // -------------------------------------------------
            // ROLLBACK MOVE / RENAME FIRST
            // -------------------------------------------------
            //
            // Restore the original project location before
            // touching the Git remote again.

            if (moved)
            {
                try
                {
                    if (
                        Directory.Exists(
                            finalProjectPath) &&
                        !Directory.Exists(
                            originalProjectPath))
                    {
                        Directory.Move(
                            finalProjectPath,
                            originalProjectPath);
                    }
                }
                catch (Exception moveRollbackException)
                {
                    Console.Error.WriteLine(
                        $"Project folder rollback failed: " +
                        $"{moveRollbackException.Message}");
                }
            }

            // -------------------------------------------------
            // ROLLBACK GIT REMOTE URL
            // -------------------------------------------------

            if (
                gitRemoteChangedBySave &&
                !string.IsNullOrWhiteSpace(
                    originalGitRemoteUrl))
            {
                try
                {
                    var remoteRollbackPath =
                        Directory.Exists(
                            originalProjectPath)
                            ? originalProjectPath
                            : finalProjectPath;

                    SetGitRemoteUrl(
                        remoteRollbackPath,
                        originalGitRemoteUrl);
                }
                catch (Exception remoteRollbackException)
                {
                    Console.Error.WriteLine(
                        $"Git remote rollback failed: " +
                        $"{remoteRollbackException.Message}");
                }
            }

            // -------------------------------------------------
            // ROLLBACK GITHUB REPOSITORY RENAME
            // -------------------------------------------------

            if (
                githubRenamedBySave &&
                !string.IsNullOrWhiteSpace(
                    originalGitHubRepository))
            {
                try
                {
                    RenameGitHubRepository(
                        projectName,
                        originalGitHubRepository);
                }
                catch (Exception githubRollbackException)
                {
                    Console.Error.WriteLine(
                        $"GitHub rollback failed: " +
                        $"{githubRollbackException.Message}");
                }
            }

            // -------------------------------------------------
            // ROLLBACK GIT CREATED BY THIS SAVE
            // -------------------------------------------------

            if (gitInitializedBySave)
            {
                try
                {
                    var gitPath =
                        Path.Combine(
                            originalProjectPath,
                            ".git");

                    if (Directory.Exists(gitPath))
                    {
                        Directory.Delete(
                            gitPath,
                            recursive: true);
                    }
                }
                catch
                {
                    // Do not replace the original exception.
                }
            }

            Console.Error.WriteLine(
                "Failed to save project:");

            Console.Error.WriteLine(
                exception.Message);

            _isSavingProject = false;
        }
    }

// =========================================================
// REMOVE PROJECT ERROR DIALOG
// =========================================================

private async System.Threading.Tasks.Task ShowError(
    string title,
    string message)
{
    var dialog =
        new Window
        {
            Title = title,

            Width = 500,

            SizeToContent =
                Avalonia.Controls.SizeToContent.Height,

            WindowStartupLocation =
                WindowStartupLocation.CenterOwner,

            CanResize = false
        };

    var okButton =
        new Button
        {
            Content = "OK",

            MinWidth = 90,

            HorizontalAlignment =
                Avalonia.Layout.HorizontalAlignment.Right
        };

    okButton.Click +=
        (_, _) =>
        {
            dialog.Close();
        };

    dialog.Content =
        new StackPanel
        {
            Spacing = 16,

            Margin = new Avalonia.Thickness(24),

            Children =
            {
                new TextBlock
                {
                    Text = title,

                    FontSize = 20,

                    FontWeight =
                        Avalonia.Media.FontWeight.Bold
                },

                new TextBlock
                {
                    Text = message,

                    TextWrapping =
                        Avalonia.Media.TextWrapping.Wrap
                },

                okButton
            }
        };

    await dialog.ShowDialog(this);
}


private async System.Threading.Tasks.Task ShowRemoveError(
    string message)
{
    var dialog =
        new Window
        {
            Title =
                "Could Not Remove Project",

            Width = 500,

            SizeToContent =
                Avalonia.Controls.SizeToContent.Height,

            WindowStartupLocation =
                WindowStartupLocation.CenterOwner,

            CanResize = false
        };

    var okButton =
        new Button
        {
            Content = "OK",

            MinWidth = 90,

            HorizontalAlignment =
                Avalonia.Layout.HorizontalAlignment.Right
        };

    okButton.Click +=
        (_, _) =>
        {
            dialog.Close();
        };

    dialog.Content =
        new StackPanel
        {
            Spacing = 16,

            Margin = new Avalonia.Thickness(24),

            Children =
            {
                new TextBlock
                {
                    Text =
                        "Could Not Remove Project",

                    FontSize = 20,

                    FontWeight =
                        Avalonia.Media.FontWeight.Bold
                },

                new TextBlock
                {
                    Text = message,

                    TextWrapping =
                        Avalonia.Media.TextWrapping.Wrap
                },

                okButton
            }
        };

    await dialog.ShowDialog(this);
}


// =========================================================
// CANCEL
// =========================================================

private void CancelButton_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
{
    Close();
}

}
