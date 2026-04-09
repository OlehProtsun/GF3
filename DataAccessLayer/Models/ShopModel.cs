using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

/// <summary>
/// Persistence model for a shop or location that schedules are created for.
/// </summary>
[Table("shop")]
public class ShopModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Required]
    [Column("address")]
    public string Address { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Schedules planned for this shop.
    /// </summary>
    public ICollection<ScheduleModel> Schedules { get; set; } = new List<ScheduleModel>();
}
