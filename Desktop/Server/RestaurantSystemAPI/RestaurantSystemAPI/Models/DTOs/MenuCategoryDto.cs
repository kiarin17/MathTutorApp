namespace RestaurantSystemAPI.Models.DTOs
{
    public class MenuCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int DisplayOrder { get; set; }
    }
}