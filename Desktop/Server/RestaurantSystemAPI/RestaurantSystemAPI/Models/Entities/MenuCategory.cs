using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantSystemAPI.Models.Entities
{
    public class MenuCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Навигационное свойство
        public virtual ICollection<MenuItem> MenuItems { get; set; }
    }
}