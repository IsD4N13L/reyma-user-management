namespace UserManagement.Domain.Users.Models
{
    public sealed record UserForUpdate
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool Active { get; set; }
    }
}
