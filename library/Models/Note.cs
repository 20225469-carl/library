namespace library.Models;

public class Note
{
    public long NoteId { get; set; }
    public string Title { get; set; } = "";
    public string? Body { get; set; }
    public DateOnly CreatedOn { get; set; }
}