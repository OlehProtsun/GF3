using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Models
{
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


        public ICollection<ScheduleModel> Schedules { get; set; } = new List<ScheduleModel>();
    }
}
