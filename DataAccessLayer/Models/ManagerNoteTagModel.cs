using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("manager_note_tag")]
public sealed class ManagerNoteTagModel
{
    [Column("id")]
    public int Id { get; set; }

    [Column("manager_account_id")]
    public int ManagerAccountId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("created_at_utc")]
    public DateTimeOffset CreatedAtUtc { get; set; }
}
