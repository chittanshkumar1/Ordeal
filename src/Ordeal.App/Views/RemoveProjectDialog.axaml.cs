using Avalonia.Controls;

namespace Ordeal.App.Views;

public partial class RemoveProjectDialog : Window
{
    public bool RemoveGitRepository { get; private set; }

    public bool Confirmed { get; private set; }

    // Required by Avalonia's runtime loader.
    public RemoveProjectDialog()
    {
        InitializeComponent();
    }

    public RemoveProjectDialog(
        string projectName,
        string projectPath,
        bool hasGitHubRepository)
    {
        InitializeComponent();

        QuestionText.Text =
            $"Are you sure you want to remove \"{projectName}\"?";

        PathText.Text =
            $"This will permanently delete the local project folder:\n\n{projectPath}";

        RemoveGitRepositoryCheckBox.IsEnabled =
            hasGitHubRepository;

        RemoveGitRepositoryCheckBox.IsChecked =
            false;

        RemoveGitRepositoryCheckBox.Click +=
            RemoveGitRepositoryCheckBox_Click;
    }

    private void RemoveGitRepositoryCheckBox_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        GitWarningText.IsVisible =
            RemoveGitRepositoryCheckBox.IsChecked == true;
    }

    private void CancelButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        Confirmed = false;

        Close();
    }

    private void RemoveProjectButton_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        RemoveGitRepository =
            RemoveGitRepositoryCheckBox.IsChecked == true;

        Confirmed = true;

        Close();
    }
}
