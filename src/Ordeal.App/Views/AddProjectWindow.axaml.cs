using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Ordeal.App.Data;
using Ordeal.App.Models;
using Ordeal.App.Services;

namespace Ordeal.App.Views;

public partial class AddProjectWindow : Window
{
    private bool _isCreatingProject;

    private List<Task> _previewTasks = new();

    public AddProjectWindow()
    {
        InitializeComponent();

        // GitHub visibility is only relevant when
        // creating a GitHub repository.
        PublicRadioButton.IsEnabled = false;
        PrivateRadioButton.IsEnabled = false;

        CreateGitHubRepositoryCheckBox.Click +=
            CreateGitHubRepositoryCheckBox_Click;

        FolderStructureBox.AddHandler(
            InputElement.KeyDownEvent,
            HandleHierarchyTab,
            handledEventsToo: true);

        TaskHierarchyBox.AddHandler(
            InputElement.KeyDownEvent,
            HandleHierarchyTab,
            handledEventsToo: true);
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
                    Title = "Select Project Location",
                    AllowMultiple = false
                });

        if (folders.Count > 0)
        {
            ProjectLocationBox.Text =
                folders[0].Path.LocalPath;
        }
    }

    // =========================================================
    // TAB INSERTION FOR HIERARCHY TEXT BOXES
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
            textBox.Text ?? string.Empty;

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
        var hierarchyText =
            TaskHierarchyBox.Text ?? string.Empty;

        var tasks =
            TaskHierarchyParser.Parse(
                hierarchyText,
                0);

        BuildTaskTree(tasks);

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
        _previewTasks =
            tasks;

        TaskTreePanel.Children.Clear();

        if (tasks.Count == 0)
        {
            var emptyText =
                new TextBlock
                {
                    Text =
                        "No tasks to display.",

                    FontSize =
                        14,

                    Foreground =
                        Brushes.Gray
                };

            TaskTreePanel.Children.Add(
                emptyText);

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

            // =================================================
            // TASK ROW
            // =================================================

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

            // =================================================
            // COMPLETION CHECKBOX
            // =================================================

            var checkBox =
                new CheckBox
                {
                    IsChecked =
                        GetTaskCompletion(
                            tasks,
                            task),

                    // Only leaf tasks can be manually completed.
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

                // Rebuild tree so parent completion
                // states are recalculated.
                BuildTaskTree(
                    _previewTasks);
            };

            row.Children.Add(
                checkBox);

            // =================================================
            // TASK NAME
            // =================================================

            var connector =
                level == 0
                    ? ""
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

                    // Indent only the text.
                    // Checkbox stays aligned on the left.
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

            // =================================================
            // DAILY TASK "+"
            // ONLY LEAF TASKS GET THIS
            // =================================================

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

                        // Grey when already added
                        // to Daily Tasks.
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
            t =>
                t.ParentTaskId ==
                task.Id);
    }

    // =========================================================
    // CALCULATE TASK COMPLETION
    // =========================================================

    private static bool GetTaskCompletion(
        List<Task> tasks,
        Task task)
    {
        // Leaf task:
        // use its actual completion state.
        if (IsLeafTask(
                tasks,
                task))
        {
            return task.IsCompleted;
        }

        // Parent task:
        // completed only when ALL leaf descendants
        // are completed.
        var leafDescendants =
            GetLeafDescendants(
                tasks,
                task);

        if (leafDescendants.Count == 0)
            return false;

        return leafDescendants.All(
            t =>
                t.IsCompleted);
    }

    // =========================================================
    // FIND ALL LEAF DESCENDANTS
    // =========================================================

    private static List<Task> GetLeafDescendants(
        List<Task> tasks,
        Task parent)
    {
        var result =
            new List<Task>();

        foreach (var task in tasks)
        {
            if (task.ParentTaskId != parent.Id)
                continue;

            if (IsLeafTask(
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
            task.TimeType == "Duration" &&
            task.DurationDays.HasValue)
        {
            return
                $" : {task.DurationDays.Value}d";
        }

        if (
            task.TimeType == "Date" &&
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
        var level = 0;

        var currentParentId =
            task.ParentTaskId;

        while (currentParentId.HasValue)
        {
            var parent =
                tasks.Find(
                    t =>
                        t.Id ==
                        currentParentId.Value);

            if (parent == null)
                break;

            level++;

            currentParentId =
                parent.ParentTaskId;
        }

        return level;
    }

    // =========================================================
    // CREATE PROJECT
    // =========================================================

    private void CreateProjectButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isCreatingProject)
            return;

        // -----------------------------------------------------
        // 1. READ VALUES FROM UI
        // -----------------------------------------------------

        var projectName =
            ProjectNameBox.Text?.Trim()
            ?? string.Empty;

        var parentLocation =
            ProjectLocationBox.Text?.Trim()
            ?? string.Empty;

        var description =
            DescriptionBox.Text?.Trim()
            ?? string.Empty;

        var folderStructure =
            FolderStructureBox.Text
            ?? string.Empty;

        var taskHierarchy =
            TaskHierarchyBox.Text
            ?? string.Empty;

        // -----------------------------------------------------
        // 2. BASIC VALIDATION
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(
                projectName))
        {
            ProjectNameBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(
                parentLocation))
        {
            ProjectLocationBox.Focus();
            return;
        }

        // -----------------------------------------------------
        // 3. VALIDATE TASK HIERARCHY
        // -----------------------------------------------------

        var taskValidation =
            TaskHierarchyParser.TryParse(
                taskHierarchy,
                0);

        if (!taskValidation.IsValid)
        {
            ShowError(
                "Invalid Task Hierarchy",
                taskValidation.ErrorMessage);

            return;
        }

        // -----------------------------------------------------
        // 4. VALIDATE GITHUB PREREQUISITES
        // -----------------------------------------------------

        var createGitHub =
            CreateGitHubRepositoryCheckBox.IsChecked == true;

        if (createGitHub)
        {
            if (!GitHubService.IsCliAvailable())
            {
                ShowError(
                    "GitHub CLI Not Available",
                    "GitHub CLI (gh) is not installed or could not be started.");

                return;
            }

            if (!GitHubService.IsConnected())
            {
                ShowError(
                    "GitHub Not Connected",
                    "You are not currently authenticated with GitHub CLI. " +
                    "Run 'gh auth login' and try again.");

                return;
            }
        }

        // -----------------------------------------------------
        // 4. CREATE ACTUAL PROJECT FOLDER
        // -----------------------------------------------------

        _isCreatingProject = true;

        try
        {
            ProjectFileSystemService
                .CreateProjectDirectory(
                    parentLocation,
                    projectName,
                    description,
                    folderStructure);
        }
        catch (Exception ex)
        {
            _isCreatingProject = false;

            ShowError(
                "Could Not Create Project",
                ex.Message);

            return;
        }

        // -----------------------------------------------------
        // 4. GIT / GITHUB
        // -----------------------------------------------------

        var projectPath =
            System.IO.Path.Combine(
                parentLocation,
                projectName);

        var initializeGit =
            InitializeGitCheckBox.IsChecked == true;

        // Creating a GitHub repository requires
        // a local Git repository.
        if (createGitHub)
        {
            initializeGit = true;
        }

        try
        {
            // ---------------------------------------------
            // GIT INIT
            // ---------------------------------------------

            if (initializeGit)
            {
                InitializeGitRepository(
                    projectPath);
            }

            // ---------------------------------------------
            // GITHUB REPOSITORY
            // ---------------------------------------------

            if (createGitHub)
            {
                var visibility =
                    PublicRadioButton.IsChecked == true
                        ? "public"
                        : "private";

                CreateGitHubRepository(
                    projectPath,
                    projectName,
                    visibility);
            }
        }
        catch (Exception ex)
        {
            try
            {
                if (Directory.Exists(projectPath))
                {
                    Directory.Delete(
                        projectPath,
                        recursive: true);
                }
            }
            catch (Exception cleanupException)
            {
                _isCreatingProject = false;

                ShowError(
                    "Could Not Set Up Git",
                    $"{ex.Message}\n\n" +
                    $"The project could not be created successfully, " +
                    $"and Ordeal also could not clean up the project folder.\n\n" +
                    $"Cleanup error:\n{cleanupException.Message}");

                return;
            }

            _isCreatingProject = false;

            ShowError(
                "Could Not Set Up Git",
                ex.Message);

            return;
        }

        // -----------------------------------------------------
        // 5. CREATE PROJECT DATABASE OBJECT
        // -----------------------------------------------------

        var project =
            new Project
            {
                Name =
                    projectName,

                Location =
                    parentLocation,

                Description =
                    description,

                FolderStructure =
                    folderStructure,

                TaskHierarchy =
                    taskHierarchy,

                InitializeGit =
                    initializeGit,

                CreateGitHubRepository =
                    createGitHub,

                GitHubVisibility =
                    PublicRadioButton.IsChecked == true
                        ? "Public"
                        : "Private",

                CreatedAt =
                    DateTime.Now
            };

        // -----------------------------------------------------
        // 6. OPEN SQLITE DATABASE
        //
        // The database operation is wrapped in a try/catch so
        // failures can trigger cleanup of resources created by
        // this Add Project operation.
        // -----------------------------------------------------

        try
        {
            using var db =
                new OrdealDbContext();

        // -----------------------------------------------------
        // 7. SAVE PROJECT
        //
        // Keep the entire project + task save inside one
        // SQLite transaction. If anything fails later,
        // the database changes are rolled back together.
        // -----------------------------------------------------

        using var transaction =
            db.Database.BeginTransaction();

        db.Projects.Add(
            project);

        db.SaveChanges();

        // At this point SQLite has generated
        // the real Project ID.
        //
        // Every task will use this ID.

        // -----------------------------------------------------
        // 8. GET TASKS
        //
        // If Tree mode was used, _previewTasks contains
        // the completion and Daily Task state from the UI.
        //
        // Otherwise parse the textual hierarchy directly.
        // -----------------------------------------------------

        var parsedTasks =
            _previewTasks.Count > 0
                ? _previewTasks
                : TaskHierarchyParser.Parse(
                    project.TaskHierarchy,
                    project.Id);

        // -----------------------------------------------------
        // 9. MAP TEMPORARY TASK IDs
        // TO REAL SQLITE IDs
        // -----------------------------------------------------

        var savedTaskIds =
            new Dictionary<int, int>();

        // -----------------------------------------------------
        // 10. SAVE TASKS
        // -----------------------------------------------------

        foreach (var parsedTask in parsedTasks)
        {
            var temporaryParentId =
                parsedTask.ParentTaskId;

            var task =
                new Task
                {
                    ProjectId =
                        project.Id,

                    Name =
                        parsedTask.Name,

                    TimeType =
                        parsedTask.TimeType,

                    DurationDays =
                        parsedTask.DurationDays,

                    DueDate =
                        parsedTask.DueDate,

                    IsCompleted =
                        parsedTask.IsCompleted,

                    IsInDailyTasks =
                        parsedTask.IsInDailyTasks
                };

            // -------------------------------------------------
            // Convert temporary parser parent ID
            // into the actual SQLite parent ID.
            // -------------------------------------------------

            if (
                temporaryParentId.HasValue &&
                savedTaskIds.TryGetValue(
                    temporaryParentId.Value,
                    out var realParentId))
            {
                task.ParentTaskId =
                    realParentId;
            }

            db.Tasks.Add(
                task);

            db.SaveChanges();

            // -------------------------------------------------
            // Remember the relationship:
            //
            // Temporary parser ID
            //          ↓
            // Actual SQLite ID
            // -------------------------------------------------

            savedTaskIds[
                parsedTask.Id] =
                task.Id;
        }

        // -----------------------------------------------------
        // 11. COMMIT DATABASE TRANSACTION
        // -----------------------------------------------------

        transaction.Commit();

        // -----------------------------------------------------
        // 12. CLOSE WINDOW
        // -----------------------------------------------------

        _isCreatingProject = false;

        Close();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(
            $"Could not save project to database: {ex}");

        string cleanupError =
            string.Empty;

        // -----------------------------------------------------
        // GITHUB ROLLBACK
        //
        // Only delete the repository if THIS Add operation
        // created it.
        // -----------------------------------------------------

        if (createGitHub)
        {
            try
            {
                DeleteGitHubRepository(
                    projectName);
            }
            catch (Exception githubCleanupException)
            {
                cleanupError =
                    $"GitHub cleanup failed:\n" +
                    $"{githubCleanupException.Message}";
            }
        }

        // -----------------------------------------------------
        // LOCAL FOLDER ROLLBACK
        // -----------------------------------------------------

        try
        {
            if (Directory.Exists(projectPath))
            {
                Directory.Delete(
                    projectPath,
                    recursive: true);
            }
        }
        catch (Exception folderCleanupException)
        {
            if (!string.IsNullOrWhiteSpace(cleanupError))
            {
                cleanupError += "\n\n";
            }

            cleanupError +=
                $"Local project cleanup failed:\n" +
                $"{folderCleanupException.Message}";
        }

        _isCreatingProject = false;

        if (string.IsNullOrWhiteSpace(cleanupError))
        {
            ShowError(
                "Could Not Save Project",
                $"The project could not be saved. " +
                $"All database changes were rolled back.\n\n" +
                $"{ex.Message}");
        }
        else
        {
            ShowError(
                "Project Cleanup Incomplete",
                $"The project could not be saved and the " +
                $"database changes were rolled back.\n\n" +
                $"{ex.Message}\n\n" +
                $"{cleanupError}");
        }

        return;
    }

    // End of CreateProjectButton_Click
    }

    // =========================================================
    // INITIALIZE GIT REPOSITORY
    // =========================================================

    private static void InitializeGitRepository(
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

        startInfo.ArgumentList.Add(
            "init");

        using var process =
            Process.Start(startInfo);

        if (process == null)
        {
            throw new InvalidOperationException(
                "Unable to start git.");
        }

        process.WaitForExit();

        var output =
            process.StandardOutput.ReadToEnd();

        var error =
            process.StandardError.ReadToEnd();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? "git init failed."
                    : error.Trim());
        }
    }

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
            startInfo.ArgumentList.Add(
                argument);
        }

        using var process =
            Process.Start(
                startInfo);

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
            Console.WriteLine(
                output.Trim());
        }
    }

    // =========================================================
    // CREATE GITHUB REPOSITORY
    // =========================================================

    private static void CreateGitHubRepository(
        string projectPath,
        string projectName,
        string visibility)
    {
        var startInfo =
            new ProcessStartInfo
            {
                FileName = "gh",

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

        // Repository name is exactly the project name.
        // -----------------------------------------------------
        // STAGE LOCAL PROJECT FILES
        // -----------------------------------------------------

        RunGitCommand(
            projectPath,
            "add",
            ".");

        // -----------------------------------------------------
        // CREATE INITIAL COMMIT
        // -----------------------------------------------------

        RunGitCommand(
            projectPath,
            "commit",
            "-m",
            "Initial commit");

        // -----------------------------------------------------
        // USE MAIN AS THE DEFAULT BRANCH
        // -----------------------------------------------------

        RunGitCommand(
            projectPath,
            "branch",
            "-M",
            "main");

        // -----------------------------------------------------
        // CREATE GITHUB REPOSITORY
        // -----------------------------------------------------

        startInfo.ArgumentList.Add(
            "repo");

        startInfo.ArgumentList.Add(
            "create");

        startInfo.ArgumentList.Add(
            projectName);

        // Use the existing local Git repository.
        startInfo.ArgumentList.Add(
            "--source");

        startInfo.ArgumentList.Add(
            ".");

        // Add GitHub as the local "origin" remote.
        startInfo.ArgumentList.Add(
            "--remote");

        startInfo.ArgumentList.Add(
            "origin");

        // Visibility.
        startInfo.ArgumentList.Add(
            visibility == "public"
                ? "--public"
                : "--private");

        // Push the initial commit to GitHub.
        startInfo.ArgumentList.Add(
            "--push");

        using var process =
            Process.Start(startInfo);

        if (process == null)
        {
            throw new InvalidOperationException(
                "Unable to start GitHub CLI.");
        }

        process.WaitForExit();

        var output =
            process.StandardOutput.ReadToEnd();

        var error =
            process.StandardError.ReadToEnd();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? "GitHub repository creation failed."
                    : error.Trim());
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
                "Unable to start GitHub CLI while cleaning up the repository.");
        }

        var output =
            process.StandardOutput.ReadToEnd();

        var error =
            process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? "GitHub repository cleanup failed."
                    : error.Trim());
        }

        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine(
                output.Trim());
        }
    }

    // =========================================================
    // ERROR WINDOW
    // =========================================================

    private async void ShowError(
        string title,
        string message)
    {
        var errorWindow =
            new Window
            {
                Title =
                    title,

                Width =
                    500,

                Height =
                    260,

                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner
            };

        var messageText =
            new TextBlock
            {
                Text =
                    message,

                TextWrapping =
                    TextWrapping.Wrap,

                Margin =
                    new Avalonia.Thickness(
                        20)
            };

        var okButton =
            new Button
            {
                Content =
                    "OK",

                HorizontalAlignment =
                    Avalonia.Layout.HorizontalAlignment.Right,

                Padding =
                    new Avalonia.Thickness(
                        20,
                        8),

                Margin =
                    new Avalonia.Thickness(
                        20)
            };

        okButton.Click += (_, _) =>
        {
            errorWindow.Close();
        };

        errorWindow.Content =
            new StackPanel
            {
                Spacing =
                    15,

                Children =
                {
                    messageText,
                    okButton
                }
            };

        await errorWindow.ShowDialog(
            this);
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
