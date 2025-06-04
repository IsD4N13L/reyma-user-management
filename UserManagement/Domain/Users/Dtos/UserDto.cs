namespace UserManagement.Domain.Users.Dtos
{
    public record UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool Active { get; set; }
        public string? PhotoUrl { get; set; }
        public string? PhotoBlobName { get; set; }
        public string? EncryptedPhoneNumber { get; set; }
        public string? EncryptedAddress { get; set; }
        public string? EncryptedPersonalData { get; set; }
    }
}
