using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.Domain
{
    public abstract class BaseEntity
    {
        [Key]
        public Guid Id { get; private set; } = Guid.NewGuid();

        public bool IsDeleted { get; private set; }
        public DateTimeOffset CreatedOn { get; private set; }

        public string CreatedBy { get; private set; }

        public DateTimeOffset? LastModifiedOn { get; private set; }

        public string LastModifiedBy { get; private set; }


        [NotMapped]
        public List<DomainEvent> DomainEvents { get; } = new List<DomainEvent>();

        public void UpdateCreationProperties(DateTimeOffset createdOn, string createdBy)
        {
            CreatedOn = createdOn;
            CreatedBy = createdBy;
        }

        public void UpdateModifiedProperties(DateTimeOffset? lastModifiedOn, string lastModifiedBy)
        {
            LastModifiedOn = lastModifiedOn;
            LastModifiedBy = lastModifiedBy;
        }

        public void UpdateIsDeleted(bool isDeleted)
        {
            IsDeleted = isDeleted;
        }

        public void QueueDomainEvent(DomainEvent @event)
        {
            if (!DomainEvents.Contains(@event))
                DomainEvents.Add(@event);
        }
    }
}
