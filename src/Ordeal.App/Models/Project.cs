using System;
namespace Ordeal.App.Models;

public class Project
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string FolderStructure { get; set; } = string.Empty;

    public string TaskHierarchy { get; set; } = string.Empty;

    public bool InitializeGit { get; set; }

    public bool CreateGitHubRepository { get; set; }

    public string GitHubVisibility { get; set; } = "Private";

    public DateTime CreatedAt { get; set; }
}
