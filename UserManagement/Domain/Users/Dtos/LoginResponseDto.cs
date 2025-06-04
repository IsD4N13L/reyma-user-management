namespace UserManagement.Domain.Users.Dtos
{
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public UserDto User { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
