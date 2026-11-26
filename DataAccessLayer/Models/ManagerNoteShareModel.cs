using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("manager_note_share")]
public sealed class ManagerNoteShareModel
{
    [Column("note_id")]
    public int NoteId { get; set; }

    [Column("manager_account_id")]
    public int ManagerAccountId { get; set; }
}
