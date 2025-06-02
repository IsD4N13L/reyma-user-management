using Microsoft.EntityFrameworkCore;
using UserManagement.Domain;
using UserManagement.Exceptions;
using UserManagement.Resources;

namespace UserManagement.Services
{

    public interface IGenericRepository<TEntity> : IUserManagementScopedService where TEntity : BaseEntity
    {
        IQueryable<TEntity> Query();
        Task<TEntity?> GetByIdOrDefault(Guid id, bool withTracking = true, CancellationToken cancellationToken = default);
        Task<TEntity> GetById(Guid id, bool withTracking = true, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> ExecuteRawQueryAsync(string sql, params object[] parameters);
        Task Add(TEntity entity, CancellationToken cancellationToken = default);
        Task AddRange(IEnumerable<TEntity> entity, CancellationToken cancellationToken = default);
        void Update(TEntity entity);
        void Remove(TEntity entity);
        void RemoveRange(IEnumerable<TEntity> entity);

        Task<int> ExecuteStoredProcedureAsync(string name, object[] data, CancellationToken cancellationToken = default);
    }
    public abstract class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
    {
        protected readonly DbContext _dbContext;

        protected GenericRepository(IDbContextFactory dbContextFactory, Enums.Infraestructure.DbContextType type)
        {
            _dbContext = dbContextFactory.GetDbContext(type);
        }

        public virtual IQueryable<TEntity> Query()
        {
            return _dbContext.Set<TEntity>();
        }

        public virtual async Task<TEntity> GetByIdOrDefault(Guid id, bool withTracking = true, CancellationToken cancellationToken = default)
        {
            return withTracking
                ? await _dbContext.Set<TEntity>()
                    .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
                : await _dbContext.Set<TEntity>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        public virtual async Task<TEntity> GetById(Guid id, bool withTracking = true, CancellationToken cancellationToken = default)
        {
            var entity = await GetByIdOrDefault(id, withTracking, cancellationToken);

            if (entity == null)
                throw new NotFoundException($"{typeof(TEntity).Name} with an id '{id}' was not found.");

            return entity;
        }

        public virtual async Task<IEnumerable<TEntity>> ExecuteRawQueryAsync(string sql, params object[] parameters)
        {
            return await _dbContext.Set<TEntity>().FromSqlRaw(sql, parameters).ToListAsync();
        }

        public virtual async Task Add(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
        }

        public virtual async Task AddRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            await _dbContext.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
        }

        public virtual void Update(TEntity entity)
        {
            var entityDb = _dbContext.Set<TEntity>().Update(entity);
        }

        public virtual void Remove(TEntity entity)
        {
            var entityDb = _dbContext.Set<TEntity>().Remove(entity);
        }

        public virtual void RemoveRange(IEnumerable<TEntity> entities)
        {
            _dbContext.Set<TEntity>().RemoveRange(entities);
        }


        public virtual async Task<int> ExecuteStoredProcedureAsync(string name, object[] data, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Database.ExecuteSqlRawAsync(name, data, cancellationToken);
        }
    }
}
