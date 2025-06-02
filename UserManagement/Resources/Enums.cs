namespace UserManagement.Resources
{
    public class Enums
    {
        public static class Infraestructure
        {
            public enum Environment
            {
                Development,
                Staging,
                Production
            }

            public enum DbContextType
            {
                UserManagement,
                // Agrega más según tus otros DbContext
                // Otro
            }
        }
    }
}
