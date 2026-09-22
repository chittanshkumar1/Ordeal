using System;
using System.Collections.Generic;
using System.Globalization;
using Ordeal.App.Models;

namespace Ordeal.App.Services;

public static class TaskHierarchyParser
{
    public static List<Task> Parse(
        string hierarchyText,
        int projectId)
    {
        var result = TryParse(
            hierarchyText,
            projectId);

        if (!result.IsValid)
        {
            throw new FormatException(
                result.ErrorMessage);
        }

        return result.Tasks;
    }

    public static (
        bool IsValid,
        List<Task> Tasks,
        string ErrorMessage
    ) TryParse(
        string hierarchyText,
        int projectId)
    {
        var tasks = new List<Task>();

        if (string.IsNullOrWhiteSpace(hierarchyText))
        {
            return (
                true,
                tasks,
                string.Empty
            );
        }

        var lines =
            hierarchyText
                .Replace("\r\n", "\n")
                .Split('\n');

        var parents =
            new Dictionary<int, Task>();

        var lineNumber = 0;

        foreach (var rawLine in lines)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            var indentation = 0;
            var characterIndex = 0;

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

                    if (spaces % 4 != 0)
                    {
                        return Invalid(
                            tasks,
                            $"Line {lineNumber}: indentation must use tabs " +
                            $"or groups of 4 spaces.");
                    }

                    indentation += spaces / 4;
                    continue;
                }

                break;
            }

            var content =
                rawLine
                    .Substring(characterIndex)
                    .Trim();

            if (string.IsNullOrWhiteSpace(content))
                continue;

            var parsed =
                ParseTaskLine(
                    content,
                    out var errorMessage);

            if (parsed == null)
            {
                return Invalid(
                    tasks,
                    $"Line {lineNumber}: {errorMessage}");
            }

            if (
                indentation > 0 &&
                !parents.ContainsKey(indentation - 1))
            {
                return Invalid(
                    tasks,
                    $"Line {lineNumber}: task '{parsed.Value.Name}' " +
                    $"is indented under a missing parent level.");
            }

            var task = new Task
            {
                ProjectId =
                    projectId,

                Name =
                    parsed.Value.Name,

                TimeType =
                    parsed.Value.TimeType,

                DurationDays =
                    parsed.Value.DurationDays,

                DueDate =
                    parsed.Value.DueDate,

                IsCompleted =
                    false,

                IsInDailyTasks =
                    false
            };

            if (
                indentation > 0 &&
                parents.TryGetValue(
                    indentation - 1,
                    out var parent))
            {
                task.ParentTaskId =
                    parent.Id;
            }

            tasks.Add(task);

            task.Id =
                tasks.Count;

            parents[indentation] =
                task;

            var deeperLevels =
                new List<int>();

            foreach (var level in parents.Keys)
            {
                if (level > indentation)
                    deeperLevels.Add(level);
            }

            foreach (var level in deeperLevels)
            {
                parents.Remove(level);
            }
        }

        return (
            true,
            tasks,
            string.Empty
        );
    }

    private static (
        string Name,
        string TimeType,
        int? DurationDays,
        DateTime? DueDate
    )? ParseTaskLine(
        string line,
        out string errorMessage)
    {
        errorMessage = string.Empty;

        var colonIndex =
            line.LastIndexOf(':');

        if (colonIndex < 0)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                errorMessage =
                    "task name cannot be empty.";

                return null;
            }

            return (
                line.Trim(),
                string.Empty,
                null,
                null
            );
        }

        var name =
            line
                .Substring(
                    0,
                    colonIndex)
                .Trim();

        var timeValue =
            line
                .Substring(
                    colonIndex + 1)
                .Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            errorMessage =
                "task name cannot be empty.";

            return null;
        }

        if (string.IsNullOrWhiteSpace(timeValue))
        {
            errorMessage =
                $"task '{name}' has an empty time value.";

            return null;
        }

        if (
            timeValue.EndsWith(
                "d",
                StringComparison.OrdinalIgnoreCase))
        {
            var numberPart =
                timeValue
                    .Substring(
                        0,
                        timeValue.Length - 1)
                    .Trim();

            if (
                !int.TryParse(
                    numberPart,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var duration))
            {
                errorMessage =
                    $"task '{name}' has an invalid duration '{timeValue}'. " +
                    "Use a positive number followed by 'd', for example 5d.";

                return null;
            }

            if (duration <= 0)
            {
                errorMessage =
                    $"task '{name}' must have a duration greater than 0 days.";

                return null;
            }

            return (
                name,
                "Duration",
                duration,
                null
            );
        }

        if (
            DateTime.TryParseExact(
                timeValue,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return (
                name,
                "Date",
                null,
                date
            );
        }

        errorMessage =
            $"task '{name}' has an invalid time value '{timeValue}'. " +
            "Use a duration such as 5d or a date such as 01/10/2026.";

        return null;
    }

    private static (
        bool IsValid,
        List<Task> Tasks,
        string ErrorMessage
    ) Invalid(
        List<Task> tasks,
        string errorMessage)
    {
        return (
            false,
            tasks,
            errorMessage
        );
    }
}
