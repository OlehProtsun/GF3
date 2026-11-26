using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("manager_note_tag_link")]
public sealed class ManagerNoteTagLinkModel
{
    [Column("note_id")]
    public int NoteId { get; set; }

    [Column("tag_id")]
    public int TagId { get; set; }
}
