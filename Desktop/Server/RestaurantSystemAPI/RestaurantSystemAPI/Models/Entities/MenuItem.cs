using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantSystemAPI.Models.Entities
{
    public class MenuItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 10000)]
        public decimal Price { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; }

        public int? PreparationTime { get; set; } // в минутах

        // Добавьте эти свойства
        public double? Weight { get; set; } // в граммах

        [StringLength(1000)]
        public string Composition { get; set; } // состав блюда

        public bool IsPopular { get; set; } = false; // флаг популярного блюда

        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Внешний ключ
        public int CategoryId { get; set; }

        // Навигационное свойство
        [ForeignKey("CategoryId")]
        public virtual MenuCategory Category { get; set; }
    }
}