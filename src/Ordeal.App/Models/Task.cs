using System;
public class Task
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int? ParentTaskId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string TimeType { get; set; } = string.Empty;

    public int? DurationDays { get; set; }

    public DateTime? DueDate { get; set; }

    public bool IsCompleted { get; set; }

    public bool IsInDailyTasks { get; set; }
}
