using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using UserManagement.Databases.EntityConfigurations;
using UserManagement.Domain;
using UserManagement.Domain.Users;
using UserManagement.Services;

namespace UserManagement.Databases
{
    public class UserManagementDbContext(DbContextOptions<UserManagementDbContext> options,
    ICurrentUserService currentUserService,
    IMediator mediator,
    TimeProvider dateTimeProvider)
    : DbContext(options)
    {
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new UserConfiguration());
        }


        public override int SaveChanges()
        {
            UpdateAuditFields();
            var result = base.SaveChanges();
            _dispatchDomainEvents().GetAwaiter().GetResult();
            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
        {
            UpdateAuditFields();
            var result = await base.SaveChangesAsync(cancellationToken);
            await _dispatchDomainEvents();
            return result;
        }

        private async Task _dispatchDomainEvents()
        {
            var domainEventEntities = ChangeTracker.Entries<BaseEntity>()
                .Select(po => po.Entity)
                .Where(po => po.DomainEvents.Any())
                .ToArray();

            foreach (var entity in domainEventEntities)
            {
                var events = entity.DomainEvents.ToArray();
                entity.DomainEvents.Clear();
                foreach (var entityDomainEvent in events)
                    await mediator.Publish(entityDomainEvent);
            }
        }

        private void UpdateAuditFields()
        {
            var now = dateTimeProvider.GetUtcNow();
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.UpdateCreationProperties(now, currentUserService?.UserId);
                        entry.Entity.UpdateModifiedProperties(now, currentUserService?.UserId);
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdateModifiedProperties(now, currentUserService?.UserId);
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.UpdateModifiedProperties(now, currentUserService?.UserId);
                        entry.Entity.UpdateIsDeleted(true);
                        break;
                }
            }
        }
    }

    public static class Extensions
    {
        public static void FilterSoftDeletedRecords(this ModelBuilder modelBuilder)
        {
            Expression<Func<BaseEntity, bool>> filterExpr = e => !e.IsDeleted;
            foreach (var mutableEntityType in modelBuilder.Model.GetEntityTypes()
                .Where(m => m.ClrType.IsAssignableTo(typeof(BaseEntity))))
            {
                // modify expression to handle correct child type
                var parameter = Expression.Parameter(mutableEntityType.ClrType);
                var body = ReplacingExpressionVisitor
                    .Replace(filterExpr.Parameters.First(), parameter, filterExpr.Body);
                var lambdaExpression = Expression.Lambda(body, parameter);

                // set filter
                mutableEntityType.SetQueryFilter(lambdaExpression);
            }
        }
    }
}
