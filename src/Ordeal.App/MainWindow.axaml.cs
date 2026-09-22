using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Ordeal.App.Data;
using Ordeal.App.Models;
using Ordeal.App.Services;
using Ordeal.App.Views;

namespace Ordeal.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        UpdateGitHubStatus();

        LoadProjects();
    }

    // =========================================================
    // LOAD PROJECTS
    // =========================================================

    private void LoadProjects()
    {
        ProjectCardsPanel.Children.Clear();

        using var db =
            new OrdealDbContext();

        db.Database.EnsureCreated();

        var projects =
            db.Projects
                .OrderByDescending(
                    project => project.CreatedAt)
                .ToList();

        if (projects.Count == 0)
        {
            ShowEmptyProjectsMessage();
            return;
        }

        foreach (var project in projects)
        {
            AddProjectCard(
                project,
                db);
        }
    }

    // =========================================================
    // EMPTY PROJECT STATE
    // =========================================================

    private void ShowEmptyProjectsMessage()
    {
        var emptyBorder =
            new Border
            {
                Padding =
                    new Avalonia.Thickness(20),

                MinHeight =
                    180
            };

        var emptyPanel =
            new StackPanel
            {
                HorizontalAlignment =
                    Avalonia.Layout.HorizontalAlignment.Center,

                VerticalAlignment =
                    Avalonia.Layout.VerticalAlignment.Center,

                Spacing =
                    8
            };

        emptyPanel.Children.Add(
            new TextBlock
            {
                Text =
                    "No projects yet.",

                FontSize =
                    16,

                FontWeight =
                    FontWeight.SemiBold,

                HorizontalAlignment =
                    Avalonia.Layout.HorizontalAlignment.Center
            });

        emptyPanel.Children.Add(
            new TextBlock
            {
                Text =
                    "Create a new project to get started.",

                Foreground =
                    Brushes.Gray,

                FontSize =
                    14,

                HorizontalAlignment =
                    Avalonia.Layout.HorizontalAlignment.Center
            });

        emptyBorder.Child =
            emptyPanel;

        ProjectCardsPanel.Children.Add(
            emptyBorder);
    }

    // =========================================================
    // CREATE PROJECT CARD
    // =========================================================

    private void AddProjectCard(
        Project project,
        OrdealDbContext db)
    {
        // -----------------------------------------------------
        // GET ALL TASKS BELONGING TO THIS PROJECT
        // -----------------------------------------------------

        var tasks =
            db.Tasks
                .Where(
                    task =>
                        task.ProjectId ==
                        project.Id)
                .ToList();

        // -----------------------------------------------------
        // FIND ALL LEAF TASKS
        //
        // A task is a leaf when no other task has it
        // as its ParentTaskId.
        // -----------------------------------------------------

        var leafTasks =
            tasks
                .Where(
                    task =>
                        !tasks.Any(
                            child =>
                                child.ParentTaskId ==
                                task.Id))
                .ToList();

        // -----------------------------------------------------
        // CALCULATE PROJECT PROGRESS
        //
        // Progress uses ALL leaf tasks.
        //
        // completed leaf tasks
        // -------------------- × 100
        // total leaf tasks
        // -----------------------------------------------------

        var totalLeafTasks =
            leafTasks.Count;

        var completedLeafTasks =
            leafTasks.Count(
                task =>
                    task.IsCompleted);

        var progress =
            totalLeafTasks == 0
                ? 0
                : (double)completedLeafTasks /
                  totalLeafTasks *
                  100;

        // =====================================================
        // PROJECT CARD
        // =====================================================

        var card =
            new Border
            {
                Classes =
                {
                    "project-card"
                }
            };

        var cardContent =
            new StackPanel
            {
                Spacing =
                    14
            };

        // =====================================================
        // PROJECT HEADER
        // =====================================================

        var header =
            new Grid
            {
                ColumnDefinitions =
                    new ColumnDefinitions
                    {
                        new ColumnDefinition(
                            Avalonia.Controls.GridLength.Star),

                        new ColumnDefinition(
                            Avalonia.Controls.GridLength.Auto)
                    }
            };

        var projectName =
            new TextBlock
            {
                Text =
                    project.Name,

                Classes =
                {
                    "card-title"
                },

                VerticalAlignment =
                    Avalonia.Layout.VerticalAlignment.Center
            };

        header.Children.Add(
            projectName);

        // -----------------------------------------------------
        // EDIT PROJECT
        //
        // VISUAL PLACEHOLDER ONLY.
        // NO FUNCTIONALITY YET.
        // -----------------------------------------------------

        var editButton =
            new Button
            {
                Content =
                    "Edit Project"
            };

        editButton.Click += async (_, _) =>
        {
            var editProjectWindow =
                new EditProjectWindow(project.Id);

            await editProjectWindow.ShowDialog(
                this);

            LoadProjects();
        };

        Grid.SetColumn(
            editButton,
            1);

        header.Children.Add(
            editButton);

        cardContent.Children.Add(
            header);

        // =====================================================
        // PROGRESS BAR
        // =====================================================

        var progressRow =
            new Grid
            {
                ColumnDefinitions =
                    new ColumnDefinitions
                    {
                        new ColumnDefinition(
                            Avalonia.Controls.GridLength.Star),

                        new ColumnDefinition(
                            Avalonia.Controls.GridLength.Auto)
                    },

                ColumnSpacing =
                    12
            };

        var progressBar =
            new ProgressBar
            {
                Value =
                    progress,

                VerticalAlignment =
                    Avalonia.Layout.VerticalAlignment.Center
            };

        progressRow.Children.Add(
            progressBar);

        var progressText =
            new TextBlock
            {
                Text =
                    $"{progress:0}%",

                FontSize =
                    14,

                FontWeight =
                    FontWeight.SemiBold,

                VerticalAlignment =
                    Avalonia.Layout.VerticalAlignment.Center
            };

        Grid.SetColumn(
            progressText,
            1);

        progressRow.Children.Add(
            progressText);

        cardContent.Children.Add(
            progressRow);

        // =====================================================
        // COMPLETION COUNT
        // =====================================================

        var completionText =
            new TextBlock
            {
                Text =
                    $"{completedLeafTasks} / " +
                    $"{totalLeafTasks} leaf tasks completed",

                Classes =
                {
                    "card-muted"
                }
            };

        cardContent.Children.Add(
            completionText);

        // =====================================================
        // DAILY TASKS ONLY
        //
        // Only leaf tasks where:
        //
        // IsInDailyTasks == true
        //
        // are displayed here.
        // =====================================================

        var dailyLeafTasks =
            leafTasks
                .Where(
                    task =>
                        task.IsInDailyTasks)
                .ToList();

        // -----------------------------------------------------
        // DISPLAY DAILY TASKS
        // -----------------------------------------------------

        if (dailyLeafTasks.Count > 0)
        {
            var taskList =
                new StackPanel
                {
                    Spacing =
                        7
                };

            foreach (var task in dailyLeafTasks)
            {
                var taskRow =
                    CreateTaskRow(
                        task);

                taskList.Children.Add(
                    taskRow);
            }

            cardContent.Children.Add(
                taskList);
        }
        else
        {
            cardContent.Children.Add(
                new TextBlock
                {
                    Text =
                        "No tasks added to the task list.",

                    Classes =
                    {
                        "card-muted"
                    }
                });
        }

        // =====================================================
        // REMAINING TIME
        // =====================================================

        var remainingEntries =
            BuildRemainingTimeEntries(
                dailyLeafTasks,
                tasks);

        if (remainingEntries.Count > 0)
        {
            var remainingPanel =
                new StackPanel
                {
                    Spacing =
                        5
                };

            foreach (
                var entry
                in remainingEntries)
            {
                remainingPanel.Children.Add(
                    new TextBlock
                    {
                        Text =
                            entry,

                        Classes =
                        {
                            "card-muted"
                        }
                    });
            }

            cardContent.Children.Add(
                remainingPanel);
        }

        // =====================================================
        // DIVIDER
        // =====================================================

        cardContent.Children.Add(
            new Border
            {
                Height =
                    1,

                Background =
                    new SolidColorBrush(
                        Color.Parse(
                            "#343840")),

                Margin =
                    new Avalonia.Thickness(
                        0,
                        2)
            });

        // =====================================================
        // GIT / GITHUB
        //
        // DISPLAY ONLY.
        // =====================================================

        var bottomGrid =
            new Grid
            {
                ColumnDefinitions =
                    new ColumnDefinitions
                    {
                        new ColumnDefinition(
                            Avalonia.Controls.GridLength.Star),

                        new ColumnDefinition(
                            Avalonia.Controls.GridLength.Auto)
                    },

                ColumnSpacing =
                    20
            };

        // -----------------------------------------------------
        // GIT STATUS
        // -----------------------------------------------------

        var statusPanel =
            new StackPanel
            {
                Spacing =
                    4
            };

        if (project.InitializeGit)
        {
            statusPanel.Children.Add(
                new TextBlock
                {
                    Text =
                        "●  Git      main",

                    Classes =
                    {
                        "card-muted"
                    }
                });
        }
        else
        {
            statusPanel.Children.Add(
                new TextBlock
                {
                    Text =
                        "○  Git      Not initialized",

                    Classes =
                    {
                        "card-muted"
                    }
                });
        }

        // -----------------------------------------------------
        // GITHUB STATUS
        // -----------------------------------------------------

        if (project.CreateGitHubRepository)
        {
            statusPanel.Children.Add(
                new TextBlock
                {
                    Text =
                        "●  GitHub   Connected",

                    Classes =
                    {
                        "card-muted"
                    }
                });
        }

        Grid.SetColumn(
            statusPanel,
            0);

        bottomGrid.Children.Add(
            statusPanel);

        // =====================================================
        // ACTION BUTTON PLACEHOLDERS
        //
        // NO FUNCTIONALITY YET.
        // =====================================================

        var actionPanel =
            new StackPanel
            {
                Orientation =
                    Avalonia.Layout.Orientation.Horizontal,

                Spacing =
                    8,

                HorizontalAlignment =
                    Avalonia.Layout.HorizontalAlignment.Right
            };

        var addButton =
            new Button
            {
                Content =
                    "Add",

                Classes =
                {
                    "dashboard-action"
                },

                IsEnabled =
                    project.InitializeGit
            };

        addButton.Click += (_, _) =>
        {
            GitAdd(project);
        };

        var commitButton =
            new Button
            {
                Content =
                    "Commit",

                Classes =
                {
                    "dashboard-action"
                },

                IsEnabled =
                    project.InitializeGit
            };

        commitButton.Click += async (_, _) =>
        {
            await ShowCommitDialog(project);
        };

        var pushButton =
            new Button
            {
                Content =
                    "Push",

                Classes =
                {
                    "dashboard-action"
                },

                IsEnabled =
                    project.InitializeGit &&
                    HasGitRemote(project)
            };

        if (pushButton.IsEnabled)
        {
            pushButton.Click += (_, _) =>
            {
                GitPush(project);
            };
        }

        actionPanel.Children.Add(
            addButton);

        actionPanel.Children.Add(
            commitButton);

        actionPanel.Children.Add(
            pushButton);

        Grid.SetColumn(
            actionPanel,
            1);

        bottomGrid.Children.Add(
            actionPanel);

        cardContent.Children.Add(
            bottomGrid);

        // =====================================================
        // FINISH CARD
        // =====================================================

        card.Child =
            cardContent;

        ProjectCardsPanel.Children.Add(
            card);
    }

    // =========================================================
    // GIT ADD
    // =========================================================

    private static void GitAdd(
        Project project)
    {
        var projectPath =
            Path.Combine(
                project.Location,
                project.Name);

        if (!Directory.Exists(projectPath))
        {
            Console.Error.WriteLine(
                $"Git Add failed: project folder not found: {projectPath}");

            return;
        }

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

        startInfo.ArgumentList.Add("add");
        startInfo.ArgumentList.Add(".");

        using var process =
            Process.Start(startInfo);

        if (process == null)
        {
            Console.Error.WriteLine(
                "Git Add failed: could not start git.");

            return;
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
                    ? "Git Add failed."
                    : $"Git Add failed: {error}");

            return;
        }

        Console.WriteLine(
            $"Git Add successful for project: {project.Name}");

        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine(output);
        }
    }


    // =========================================================
    // CHECK GIT REMOTE
    // =========================================================

    private static bool HasGitRemote(
        Project project)
    {
        var projectPath =
            Path.Combine(
                project.Location,
                project.Name);

        if (!Directory.Exists(projectPath))
            return false;

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

        startInfo.ArgumentList.Add("remote");
        startInfo.ArgumentList.Add("get-url");
        startInfo.ArgumentList.Add("origin");

        using var process =
            Process.Start(startInfo);

        if (process == null)
            return false;

        process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();

        process.WaitForExit();

        return process.ExitCode == 0;
    }


    // =========================================================
    // GIT PUSH
    // =========================================================

    private static void GitPush(
        Project project)
    {
        var projectPath =
            Path.Combine(
                project.Location,
                project.Name);

        if (!Directory.Exists(projectPath))
        {
            Console.Error.WriteLine(
                $"Git Push failed: project folder not found: {projectPath}");

            return;
        }

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

        startInfo.ArgumentList.Add("push");

        using var process =
            Process.Start(startInfo);

        if (process == null)
        {
            Console.Error.WriteLine(
                "Git Push failed: could not start git.");

            return;
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
                    ? "Git Push failed."
                    : $"Git Push failed: {error}");

            return;
        }

        Console.WriteLine(
            $"Git Push successful for project: {project.Name}");

        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine(output);
        }
    }


    // =========================================================
    // COMMIT DIALOG
    // =========================================================

    private async System.Threading.Tasks.Task ShowCommitDialog(
        Project project)
    {
        var dialog =
            new Window
            {
                Title =
                    "Commit Changes",

                Width =
                    500,

                SizeToContent =
                    Avalonia.Controls.SizeToContent.Height,

                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner,

                CanResize =
                    false
            };

        var messageBox =
            new TextBox
            {
                PlaceholderText =
                    "Enter commit message...",

                AcceptsReturn =
                    false,

                MinWidth =
                    440
            };

        var cancelButton =
            new Button
            {
                Content =
                    "Cancel",

                MinWidth =
                    90
            };

        var commitButton =
            new Button
            {
                Content =
                    "Commit",

                MinWidth =
                    90
            };

        cancelButton.Click +=
            (_, _) =>
            {
                dialog.Close();
            };

        commitButton.Click +=
            (_, _) =>
            {
                var message =
                    messageBox.Text?.Trim();

                if (string.IsNullOrWhiteSpace(message))
                    return;

                GitCommit(
                    project,
                    message);

                dialog.Close();
            };

        dialog.Content =
            new StackPanel
            {
                Spacing =
                    16,

                Margin =
                    new Avalonia.Thickness(24),

                Children =
                {
                    new TextBlock
                    {
                        Text =
                            "Commit Changes",

                        FontSize =
                            20,

                        FontWeight =
                            FontWeight.Bold
                    },

                    new TextBlock
                    {
                        Text =
                            "Commit message:"
                    },

                    messageBox,

                    new StackPanel
                    {
                        Orientation =
                            Avalonia.Layout.Orientation.Horizontal,

                        HorizontalAlignment =
                            Avalonia.Layout.HorizontalAlignment.Right,

                        Spacing =
                            10,

                        Children =
                        {
                            cancelButton,
                            commitButton
                        }
                    }
                }
            };

        await dialog.ShowDialog(this);
    }


    // =========================================================
    // GIT COMMIT
    // =========================================================

    private static void GitCommit(
        Project project,
        string message)
    {
        var projectPath =
            Path.Combine(
                project.Location,
                project.Name);

        if (!Directory.Exists(projectPath))
        {
            Console.Error.WriteLine(
                $"Git Commit failed: project folder not found: {projectPath}");

            return;
        }

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

        startInfo.ArgumentList.Add("commit");
        startInfo.ArgumentList.Add("-m");
        startInfo.ArgumentList.Add(message);

        using var process =
            Process.Start(startInfo);

        if (process == null)
        {
            Console.Error.WriteLine(
                "Git Commit failed: could not start git.");

            return;
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
                    ? "Git Commit failed."
                    : $"Git Commit failed: {error}");

            return;
        }

        Console.WriteLine(
            $"Git Commit successful for project: {project.Name}");

        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine(output);
        }
    }


    // =========================================================
    // CREATE CLICKABLE TASK ROW
    // =========================================================

    private Border CreateTaskRow(
        Task task)
    {
        var checkBox =
            new CheckBox
            {
                IsChecked =
                    task.IsCompleted,

                IsEnabled =
                    true,

                Classes =
                {
                    "task-check"
                },

                VerticalAlignment =
                    Avalonia.Layout.VerticalAlignment.Center
            };

        // -----------------------------------------------------
        // CHECKBOX CLICK
        //
        // This only changes IsCompleted.
        //
        // It does NOT change:
        //
        // IsInDailyTasks
        // ParentTaskId
        // Name
        // Duration
        // DueDate
        //
        // -----------------------------------------------------

        checkBox.Click += (_, _) =>
        {
            var completed =
                checkBox.IsChecked == true;

            UpdateTaskCompletion(
                task.Id,
                completed);
        };

        var row =
            new StackPanel
            {
                Orientation =
                    Avalonia.Layout.Orientation.Horizontal,

                Spacing =
                    8,

                VerticalAlignment =
                    Avalonia.Layout.VerticalAlignment.Center
            };

        row.Children.Add(
            checkBox);

        row.Children.Add(
            new TextBlock
            {
                Text =
                    task.Name,

                FontSize =
                    14,

                Foreground =
                    task.IsCompleted
                        ? new SolidColorBrush(
                            Color.Parse("#A1A1AA"))
                        : new SolidColorBrush(
                            Color.Parse("#F4F4F5")),

                VerticalAlignment =
                    Avalonia.Layout.VerticalAlignment.Center
            });

        return new Border
        {
            Child =
                row
        };
    }

    // =========================================================
    // UPDATE TASK COMPLETION IN SQLITE
    // =========================================================

    private void UpdateTaskCompletion(
        int taskId,
        bool isCompleted)
    {
        using var db =
            new OrdealDbContext();

        var task =
            db.Tasks.FirstOrDefault(
                item =>
                    item.Id ==
                    taskId);

        if (task == null)
            return;

        task.IsCompleted =
            isCompleted;

        db.SaveChanges();

        // -----------------------------------------------------
        // Reload everything from SQLite.
        //
        // This recalculates:
        //
        // completed leaf tasks
        // total leaf tasks
        // progress percentage
        //
        // -----------------------------------------------------

        LoadProjects();
    }

    // =========================================================
    // BUILD REMAINING TIME ENTRIES
    // =========================================================

    private static List<string>
        BuildRemainingTimeEntries(
            List<Task> dailyTasks,
            List<Task> allTasks)
    {
        var entries =
            new List<string>();

        // -----------------------------------------------------
        // Keep track of which task supplied the time
        // information for an entry.
        //
        // This prevents the same parent task from being
        // displayed multiple times when multiple daily
        // leaf tasks belong to that parent.
        // -----------------------------------------------------

        var displayedTimeTaskIds =
            new HashSet<int>();

        foreach (var task in dailyTasks)
        {
            var timeTask =
                FindTaskWithTimeInformation(
                    task,
                    allTasks);

            if (timeTask == null)
                continue;

            // -------------------------------------------------
            // If this task's time information has already
            // been displayed, skip it.
            // -------------------------------------------------

            if (displayedTimeTaskIds.Contains(
                    timeTask.Id))
            {
                continue;
            }

            var remaining =
                GetRemainingTime(
                    timeTask);

            if (remaining == null)
                continue;

            var taskName =
                timeTask.Id == task.Id
                    ? task.Name
                    : timeTask.Name;

            entries.Add(
                $"Remaining days to complete " +
                $"{taskName}: {remaining}");

            displayedTimeTaskIds.Add(
                timeTask.Id);
        }

        return entries;
    }

    // =========================================================
    // FIND TASK WITH TIME INFORMATION
    // =========================================================

    private static Task?
        FindTaskWithTimeInformation(
            Task task,
            List<Task> allTasks)
    {
        // -----------------------------------------------------
        // First check the leaf itself.
        // -----------------------------------------------------

        if (HasTimeInformation(task))
            return task;

        // -----------------------------------------------------
        // Then walk upward through its parents.
        // -----------------------------------------------------

        var currentParentId =
            task.ParentTaskId;

        while (currentParentId.HasValue)
        {
            var parent =
                allTasks.FirstOrDefault(
                    candidate =>
                        candidate.Id ==
                        currentParentId.Value);

            if (parent == null)
                break;

            if (HasTimeInformation(parent))
                return parent;

            currentParentId =
                parent.ParentTaskId;
        }

        return null;
    }

    // =========================================================
    // CHECK TIME INFORMATION
    // =========================================================

    private static bool HasTimeInformation(
        Task task)
    {
        return
            (
                task.TimeType == "Duration" &&
                task.DurationDays.HasValue
            )
            ||
            (
                task.TimeType == "Date" &&
                task.DueDate.HasValue
            );
    }

    // =========================================================
    // CALCULATE REMAINING TIME
    // =========================================================

    private static string?
        GetRemainingTime(
            Task task)
    {
        // -----------------------------------------------------
        // DURATION
        // -----------------------------------------------------

        if (
            task.TimeType == "Duration" &&
            task.DurationDays.HasValue)
        {
            return
                $"{task.DurationDays.Value} days";
        }

        // -----------------------------------------------------
        // DEADLINE
        // -----------------------------------------------------

        if (
            task.TimeType == "Date" &&
            task.DueDate.HasValue)
        {
            var remaining =
                (
                    task.DueDate.Value.Date -
                    DateTime.Today
                ).Days;

            if (remaining < 0)
                return "overdue";

            if (remaining == 0)
                return "today";

            return
                $"{remaining} days";
        }

        return null;
    }

    // =========================================================
    // NEW PROJECT
    // =========================================================

    // =========================================================
    // GITHUB CONNECTION STATUS
    // =========================================================

    private void UpdateGitHubStatus()
    {
        if (!GitHubService.IsConnected())
        {
            GitHubStatusButton.Content =
                "GitHub: Not Connected";

            return;
        }

        var username =
            GitHubService.GetUsername();

        GitHubStatusButton.Content =
            string.IsNullOrWhiteSpace(username)
                ? "GitHub: Connected"
                : $"GitHub: {username}";
    }

    // =========================================================
    // GITHUB LOGIN
    // =========================================================

    private void GitHubStatusButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (GitHubService.IsConnected())
        {
            return;
        }

        GitHubStatusButton.Content =
            "GitHub: Connecting...";

        GitHubService.StartLogin(() =>
        {
            Dispatcher.UIThread.Post(
                UpdateGitHubStatus);
        });
    }

    private async void NewProjectButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var addProjectWindow =
            new AddProjectWindow();

        await addProjectWindow.ShowDialog(
            this);

        LoadProjects();
    }
}
