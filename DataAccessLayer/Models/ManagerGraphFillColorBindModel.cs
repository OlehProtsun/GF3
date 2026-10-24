using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("manager_graph_fill_color_bind")]
public sealed class ManagerGraphFillColorBindModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("manager_account_id")]
    public int ManagerAccountId { get; set; }

    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Column("fill_color")]
    public string FillColor { get; set; } = string.Empty;

    public ManagerAccountModel ManagerAccount { get; set; } = null!;
}
