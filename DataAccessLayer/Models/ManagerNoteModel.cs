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

    public ManagerAccountModel ManagerAccount { get; set; } = null!;
}
