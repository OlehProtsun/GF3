using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("manager_shift_correction_setting")]
public sealed class ManagerShiftCorrectionSettingModel
{
    [Key]
    [Column("manager_account_id")]
    public int ManagerAccountId { get; set; }

    [Column("highlight_color")]
    public string HighlightColor { get; set; } = "#FDE68A";

    public ManagerAccountModel ManagerAccount { get; set; } = null!;
}
