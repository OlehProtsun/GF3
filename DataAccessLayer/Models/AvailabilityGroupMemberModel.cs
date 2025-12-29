using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Xml.Linq;

namespace DataAccessLayer.Models
{
    [Table("availability_group_member")]
    [Index(nameof(AvailabilityGroupId), nameof(EmployeeId), IsUnique = true, Name = "ux_avail_group_member_group_emp")]
    public class AvailabilityGroupMemberModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("availability_group_id")]
        public int AvailabilityGroupId { get; set; }

        public AvailabilityGroupModel AvailabilityGroup { get; set; } = null!;

        [Required]
        [Column("employee_id")]
        public int EmployeeId { get; set; }

        public EmployeeModel Employee { get; set; } = null!;

        public ICollection<AvailabilityGroupDayModel> Days { get; set; } = new List<AvailabilityGroupDayModel>();
    }
}
