using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("manager_graph_text_color_bind")]
public sealed class ManagerGraphTextColorBindModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("manager_account_id")]
    public int ManagerAccountId { get; set; }

    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Column("text_color")]
    public string TextColor { get; set; } = string.Empty;

    public ManagerAccountModel ManagerAccount { get; set; } = null!;
}
