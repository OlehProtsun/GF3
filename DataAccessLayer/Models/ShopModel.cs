using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Models
{
    [Table("shop")]
    public class ShopModel
    {
        [Key]
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

        public ICollection<ScheduleModel> Schedules { get; set; } = new List<ScheduleModel>();
    }
}
