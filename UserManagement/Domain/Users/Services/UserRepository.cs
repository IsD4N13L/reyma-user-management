using UserManagement.Databases;
using UserManagement.Services;

namespace UserManagement.Domain.Users.Services
{

    public interface IUserRepository : IGenericRepository<User>
    {
    }
    public sealed class UserRepository(IDbContextFactory dbContextFactory) : GenericRepository<User>(dbContextFactory, Resources.Enums.Infraestructure.DbContextType.UserManagement), IUserRepository
    {
    }
}
