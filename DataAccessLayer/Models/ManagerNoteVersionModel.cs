using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("manager_note_version")]
public sealed class ManagerNoteVersionModel
{
    [Column("id")]
    public long Id { get; set; }

    [Column("note_id")]
    public int NoteId { get; set; }

    [Column("revision")]
    public long Revision { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("color")]
    public string Color { get; set; } = string.Empty;

    [Column("saved_at_utc")]
    public DateTimeOffset SavedAtUtc { get; set; }

    [Column("saved_by_manager_id")]
    public int SavedByManagerId { get; set; }
}
