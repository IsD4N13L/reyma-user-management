namespace UserManagement.Configurations
{
    public class ConnectionStringOptions
    {
        public const string SectionName = "ConnectionStrings";
        public const string UserManagementKey = "UserManagementApi";

        public string UserManagementApi { get; set; } = String.Empty;
    }

    public static class ConnectionStringOptionsExtensions
    {
        public static ConnectionStringOptions GetConnectionStringOptions(this IConfiguration configuration)
            => configuration.GetSection(ConnectionStringOptions.SectionName).Get<ConnectionStringOptions>();
    }
}
