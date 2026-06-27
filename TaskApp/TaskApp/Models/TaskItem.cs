namespace TaskApp.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public Priority Priority { get; set; } = Priority.Medium;
    public DateTime? DueDate { get; set; }
    public int OwnerId { get; set; }
    public int ProjectId { get; set; }

    public User Owner { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
