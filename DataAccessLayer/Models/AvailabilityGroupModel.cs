using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Xml.Linq;

namespace DataAccessLayer.Models
{
    [Table("availability_group")]
    [Index(nameof(Year), nameof(Month), nameof(Name), Name = "ix_avail_group_year_month_name")]
    public class AvailabilityGroupModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; } = "";

        [Required]
        [Column("year")]
        public int Year { get; set; }

        [Required]
        [Column("month")]
        public int Month { get; set; }

        public ICollection<AvailabilityGroupMemberModel> Members { get; set; } = new List<AvailabilityGroupMemberModel>();
    }
}
