namespace UserManagement.Domain.Users.DomainEvents
{
    public sealed class UserCreated : DomainEvent
    {
        public User User { get; set; }
    }
}
