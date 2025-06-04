namespace UserManagement.Domain.Users.Models
{
    public sealed record UserForUpdate
    {
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
