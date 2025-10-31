using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataAccessLayer.Models.Enums;

namespace DataAccessLayer.Models
{
    [Table("availability_day")]
    [Index(nameof(AvailabilityMonthId), nameof(DayOfMonth), IsUnique = true, Name = "ux_availability_day_month_day")]
    [Index(nameof(AvailabilityMonthId), nameof(DayOfMonth), Name = "ix_avail_day_month")]
    public class AvailabilityDayModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }


        [Required]
        [Column("availability_month_id")]
        public int AvailabilityMonthId { get; set; }


        public AvailabilityMonthModel AvailabilityMonth { get; set; } = null!;


        [Required]
        [Column("day_of_month")]
        public int DayOfMonth { get; set; }


        [Required]
        [Column("kind")]
        public AvailabilityKind Kind { get; set; }


        [Column("interval_str")]
        public string? IntervalStr { get; set; }
    }
}
