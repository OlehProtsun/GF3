using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("manager_notepad_state")]
public sealed class ManagerNotepadStateModel
{
    [Key]
    [Column("manager_account_id")]
    public int ManagerAccountId { get; set; }

    [Column("is_expanded")]
    public bool IsExpanded { get; set; }

    [Column("is_pinned")]
    public bool IsPinned { get; set; }

    [Column("height")]
    public int Height { get; set; } = 520;

    [Column("updated_at_utc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ManagerAccountModel ManagerAccount { get; set; } = null!;
}
