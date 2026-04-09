using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

/// <summary>
/// Logical container that groups schedules, presets, and graph-like planning artifacts.
/// Container names are unique because the UI and export flows often reference them as stable human-readable identifiers.
/// </summary>
[Table("container")]
[Index(nameof(Name), IsUnique = true)]
public class ContainerModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("note")]
    public string? Note { get; set; }

    /// <summary>
    /// Schedules stored under this container.
    /// </summary>
    public ICollection<ScheduleModel> Schedules { get; set; } = new List<ScheduleModel>();
}
