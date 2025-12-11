using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantSystemAPI.Models.Entities
{
    public class ContentBlock
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string BlockKey { get; set; }

        public string? Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public string Page { get; set; }

        [Required]
        public string Section { get; set; }

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public int? LastModifiedBy { get; set; }

        // Навигационные свойства
        [ForeignKey("LastModifiedBy")]
        public virtual User? ModifiedByUser { get; set; }
    }
}