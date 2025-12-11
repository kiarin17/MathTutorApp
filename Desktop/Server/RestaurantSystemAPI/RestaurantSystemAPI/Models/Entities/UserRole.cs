namespace RestaurantSystemAPI.Models.Entities
{
    public static class UserRole
    {
        public const string Admin = "Admin";
        public const string Courier = "Courier";
        public const string Client = "Client";

        public static bool IsValidRole(string role)
        {
            return role == Admin || role == Courier || role == Client;
        }

        public static string[] GetAllRoles()
        {
            return new[] { Admin, Courier, Client };
        }
    }
}