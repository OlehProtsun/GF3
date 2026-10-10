using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Models.Enums;

namespace DataAccessLayer.Models;

/// <summary>
/// Header record for a monthly availability dataset.
/// Each group belongs to a concrete year/month pair and contains member rows for the employees that participate in it.
/// </summary>
[Table("availability_group")]
[Index(nameof(Year), nameof(Month), nameof(Name), Name = "ix_avail_group_year_month_name")]
public class AvailabilityGroupModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("year")]
    public int Year { get; set; }

    [Required]
    [Column("month")]
    public int Month { get; set; }

    [Required]
    [Column("publication_status")]
    public AvailabilityPublicationStatus PublicationStatus { get; set; } = AvailabilityPublicationStatus.Private;

    [Column("visible_from_utc")]
    public DateTimeOffset? VisibleFromUtc { get; set; }

    [Column("visible_to_utc")]
    public DateTimeOffset? VisibleToUtc { get; set; }

    /// <summary>
    /// Employees that are tracked inside the group.
    /// </summary>
    public ICollection<AvailabilityGroupMemberModel> Members { get; set; } = new List<AvailabilityGroupMemberModel>();
}
