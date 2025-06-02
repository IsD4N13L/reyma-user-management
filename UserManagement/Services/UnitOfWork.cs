using UserManagement.Databases;

namespace UserManagement.Services
{
    public interface IUnitOfWork : IUserManagementScopedService
    {
        Task<int> CommitChanges(CancellationToken cancellationToken = default);
    }

    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly UserManagementDbContext _dbContext;

        public UnitOfWork(UserManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> CommitChanges(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
