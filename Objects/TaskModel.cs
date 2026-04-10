namespace MareJira.Objects;

public class TaskModel {
    public string TaskName     { get; set; }
    public string AssignedId   { get; set; }
    public string AssigneeId   { get; set; }
    public string DateAssigned { get; set; }
    public string Deadline     { get; set; }
    public string? Description { get; set; }
    public string Priority     { get; set; }
    public string Progress     { get; set; }
}