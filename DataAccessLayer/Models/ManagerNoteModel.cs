using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("manager_note")]
public sealed class ManagerNoteModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("manager_account_id")]
    public int ManagerAccountId { get; set; }

    [MaxLength(160)]
    [Column("title")]
    public string Title { get; set; } = "Untitled note";

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [MaxLength(16)]
    [Column("color")]
    public string Color { get; set; } = "yellow";

    [Column("created_at_utc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    [Column("updated_at_utc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }

    [Column("visibility")]
    public string Visibility { get; set; } = "private";

    [Column("is_pinned")]
    public bool IsPinned { get; set; }

    [Column("folder_id")]
    public int? FolderId { get; set; }

    [Column("is_locked")]
    public bool IsLocked { get; set; }

    [Column("revision")]
    public long Revision { get; set; } = 1;

    [Column("last_edited_by_manager_id")]
    public int? LastEditedByManagerId { get; set; }

    [Column("edit_session_id")]
    public Guid? EditSessionId { get; set; }

    [Column("edit_manager_id")]
    public int? EditManagerId { get; set; }

    [Column("edit_started_at_ms")]
    public long? EditStartedAtMs { get; set; }

    [Column("edit_expires_at_ms")]
    public long? EditExpiresAtMs { get; set; }

    public ICollection<ManagerNoteTagLinkModel> TagLinks { get; set; } = [];
    public ICollection<ManagerNoteShareModel> Shares { get; set; } = [];

    public ManagerAccountModel ManagerAccount { get; set; } = null!;
}
