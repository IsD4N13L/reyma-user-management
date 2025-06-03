using UserManagement.Domain.Users.DomainEvents;
using UserManagement.Domain.Users.Models;

namespace UserManagement.Domain.Users
{
    public class User : BaseEntity
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool Active { get; set; }
        public string? PhotoUrl { get; set; }
        public string? PhotoBlobName { get; set; }

        public static User Create(UserForCreation userForCreation)
        {
            var newUser = new User();

            newUser.Username = userForCreation.Username;
            newUser.Email = userForCreation.Email;
            newUser.PasswordHash = userForCreation.PasswordHash;
            newUser.LastLoginAt = userForCreation.LastLoginAt;
            newUser.Active = userForCreation.Active;
            newUser.PhotoUrl = userForCreation.PhotoUrl;
            newUser.PhotoBlobName = userForCreation.PhotoBlobName;

            newUser.QueueDomainEvent(new UserCreated() { User = newUser });
            return newUser;
        }

        public User Update(UserForUpdate userForUpdate)
        {
            Username = userForUpdate.Username;
            Email = userForUpdate.Email;
            LastLoginAt = userForUpdate.LastLoginAt;
            Active = userForUpdate.Active;
            PhotoUrl = userForUpdate.PhotoUrl ?? PhotoUrl;
            PhotoBlobName = userForUpdate.PhotoBlobName ?? PhotoBlobName;

            QueueDomainEvent(new UserUpdated() { Id = Id });
            return this;
        }


        protected User() { }
    }

}
