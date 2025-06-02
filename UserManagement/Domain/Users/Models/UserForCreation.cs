namespace UserManagement.Domain.Users.Models
{
    public sealed record UserForCreation
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
