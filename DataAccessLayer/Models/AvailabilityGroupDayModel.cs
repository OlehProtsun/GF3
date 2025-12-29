using DataAccessLayer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Xml.Linq;

namespace DataAccessLayer.Models
{
    [Table("availability_group_day")]
    [Index(nameof(AvailabilityGroupMemberId), nameof(DayOfMonth), IsUnique = true, Name = "ux_avail_group_day_member_dom")]
    public class AvailabilityGroupDayModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("availability_group_member_id")]
        public int AvailabilityGroupMemberId { get; set; }

        public AvailabilityGroupMemberModel AvailabilityGroupMember { get; set; } = null!;

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
