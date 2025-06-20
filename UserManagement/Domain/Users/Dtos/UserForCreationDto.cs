﻿namespace UserManagement.Domain.Users.Dtos
{
    public sealed record UserForCreationDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
