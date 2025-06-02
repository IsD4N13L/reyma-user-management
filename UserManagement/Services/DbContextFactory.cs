using Microsoft.EntityFrameworkCore;
using UserManagement.Databases;
using UserManagement.Resources;

namespace UserManagement.Services
{

    public interface IDbContextFactory : IUserManagementScopedService
    {
        DbContext GetDbContext(Enums.Infraestructure.DbContextType type);
    }
    public class DbContextFactory : IDbContextFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DbContextFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public DbContext GetDbContext(Enums.Infraestructure.DbContextType type)
        {

            return type switch
            {
                Enums.Infraestructure.DbContextType.UserManagement => _serviceProvider.GetRequiredService<UserManagementDbContext>(),
                // Agrega más según tus otros DbContext
                // Enums.Infraestructure.DbContextType.Otro => _serviceProvider.GetRequiredService<OtroDbContext>(),
                _ => throw new ArgumentException($"DbContext no encontrado para el tipo: {type}")
            };
        }
    }
}
