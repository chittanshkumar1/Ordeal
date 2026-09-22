using System;
using System.Collections.Generic;
using System.IO;

namespace Ordeal.App.Services;

public static class ProjectFileSystemService
{
    public static string CreateProjectDirectory(
        string parentLocation,
        string projectName,
        string description,
        string folderStructure)
    {
        if (string.IsNullOrWhiteSpace(parentLocation))
        {
            throw new ArgumentException(
                "Project location cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException(
                "Project name cannot be empty.");
        }

        if (!Directory.Exists(parentLocation))
        {
            throw new DirectoryNotFoundException(
                $"Project location does not exist:\n{parentLocation}");
        }

        ValidateProjectName(projectName);

        var projectPath =
            Path.Combine(
                parentLocation,
                projectName);

        if (Directory.Exists(projectPath))
        {
            throw new IOException(
                $"A project folder already exists:\n{projectPath}");
        }

        // Create the main project directory.
        Directory.CreateDirectory(projectPath);

        // Create description.txt.
        var descriptionPath =
            Path.Combine(
                projectPath,
                "description.txt");

        File.WriteAllText(
            descriptionPath,
            description ?? string.Empty);

        // Create the requested folder hierarchy.
        CreateFolderStructure(
            projectPath,
            folderStructure);

        return projectPath;
    }

    private static void ValidateProjectName(
        string projectName)
    {
        if (
            projectName == "." ||
            projectName == "..")
        {
            throw new ArgumentException(
                "Invalid project name.");
        }

        foreach (
            var invalidCharacter
            in Path.GetInvalidFileNameChars())
        {
            if (projectName.Contains(
                    invalidCharacter))
            {
                throw new ArgumentException(
                    $"Project name contains an invalid character: " +
                    $"'{invalidCharacter}'");
            }
        }
    }

    private static void CreateFolderStructure(
        string projectPath,
        string folderStructure)
    {
        if (string.IsNullOrWhiteSpace(folderStructure))
            return;

        var lines =
            folderStructure
                .Replace("\r\n", "\n")
                .Split('\n');

        var folders =
            new Dictionary<int, string>();

        foreach (var rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            var indentation = 0;
            var characterIndex = 0;

            // TAB = one level.
            // Four spaces = one level.
            while (characterIndex < rawLine.Length)
            {
                if (rawLine[characterIndex] == '\t')
                {
                    indentation++;
                    characterIndex++;
                    continue;
                }

                if (rawLine[characterIndex] == ' ')
                {
                    var spaces = 0;

                    while (
                        characterIndex < rawLine.Length &&
                        rawLine[characterIndex] == ' ')
                    {
                        spaces++;
                        characterIndex++;
                    }

                    indentation += spaces / 4;

                    continue;
                }

                break;
            }

            var folderName =
                rawLine
                    .Substring(characterIndex)
                    .Trim();

            if (string.IsNullOrWhiteSpace(folderName))
                continue;

            ValidateFolderName(folderName);

            string parentPath;

            if (indentation == 0)
            {
                parentPath = projectPath;
            }
            else
            {
                if (!folders.TryGetValue(
                        indentation - 1,
                        out var parentFolder))
                {
                    throw new InvalidOperationException(
                        $"Invalid folder indentation near '{folderName}'.");
                }

                parentPath = parentFolder;
            }

            var folderPath =
                Path.Combine(
                    parentPath,
                    folderName);

            Directory.CreateDirectory(folderPath);

            folders[indentation] = folderPath;

            // Remove deeper levels from the previous branch.
            var deeperLevels =
                new List<int>();

            foreach (var level in folders.Keys)
            {
                if (level > indentation)
                    deeperLevels.Add(level);
            }

            foreach (var level in deeperLevels)
            {
                folders.Remove(level);
            }
        }
    }

    private static void ValidateFolderName(
        string folderName)
    {
        if (
            folderName == "." ||
            folderName == "..")
        {
            throw new ArgumentException(
                $"Invalid folder name: '{folderName}'");
        }

        if (
            folderName.Contains('/') ||
            folderName.Contains('\\'))
        {
            throw new ArgumentException(
                $"Folder name cannot contain '/' or '\\': " +
                $"'{folderName}'");
        }

        foreach (
            var invalidCharacter
            in Path.GetInvalidFileNameChars())
        {
            if (folderName.Contains(
                    invalidCharacter))
            {
                throw new ArgumentException(
                    $"Folder name contains an invalid character: " +
                    $"'{invalidCharacter}'");
            }
        }
    }
}
