namespace UserManagement.Domain.Users.Dtos
{
    public record UserDto
    {
        Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool Active { get; set; }
    }
}
