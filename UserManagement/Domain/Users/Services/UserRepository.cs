using Microsoft.EntityFrameworkCore;
using UserManagement.Databases;
using UserManagement.Exceptions;
using UserManagement.Services;

namespace UserManagement.Domain.Users.Services
{

    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    }
    public sealed class UserRepository(IDbContextFactory dbContextFactory) : GenericRepository<User>(dbContextFactory, Resources.Enums.Infraestructure.DbContextType.UserManagement), IUserRepository
    {
        public async Task<User> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
                ?? throw new NotFoundException($"User with email '{email}' was not found.");
        }
    }
}
