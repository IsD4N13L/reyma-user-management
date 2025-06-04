using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserManagement.Services;

namespace UserManagement.Domain.Users.Services
{
    public interface IUserSecurityService : IUserManagementScopedService
    {
        Task<string> HashPasswordAsync(string password);
        Task<bool> VerifyPasswordAsync(string password, string hashedPassword);
        Task<string> GenerateJwtTokenAsync(User user, CancellationToken cancellationToken = default);
        Task<string> EncryptPersonalDataAsync(string data, CancellationToken cancellationToken = default);
        Task<string> DecryptPersonalDataAsync(string encryptedData, CancellationToken cancellationToken = default);
    }
    public class UserSecurityService(IPasswordHasher<User> passwordHasher,
        IKeyVaultService keyVaultService,
        IConfiguration configuration, IConfigurationService configurationService) : IUserSecurityService
    {
        public Task<string> HashPasswordAsync(string password)
        {
            var user = User.Create(new Models.UserForCreation { Username = "temp", Email = "temp@example.com" }); // Usuario temporal para el hashing  
            var hashedPassword = passwordHasher.HashPassword(user, password);
            return Task.FromResult(hashedPassword);
        }

        public Task<bool> VerifyPasswordAsync(string password, string hashedPassword)
        {
            var user = User.Create(new Models.UserForCreation { Username = "temp", Email = "temp@example.com" }); // Usuario temporal para la verificación  
            var result = passwordHasher.VerifyHashedPassword(user, hashedPassword, password);
            return Task.FromResult(result == PasswordVerificationResult.Success);
        }

        public async Task<string> GenerateJwtTokenAsync(User user, CancellationToken cancellationToken = default)
        {
            try
            {
                var secretKey = await configurationService.GetJwtSecretKeyAsync();
                var issuer = await configurationService.GetJwtIssuerAsync();
                var audience = await configurationService.GetJwtAudienceAsync();

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("user_id", user.Id.ToString()),
                new Claim("issued_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(24),
                    signingCredentials: credentials
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                Log.Information("JWT token generado para usuario {UserId}", user.Id);

                return tokenString;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al generar JWT token para usuario {UserId}", user.Id);
                throw;
            }
        }

        public async Task<string> EncryptPersonalDataAsync(string data, CancellationToken cancellationToken = default)
        {
            if (keyVaultService == null)
            {
                Log.Warning("Key Vault no disponible, datos no cifrados");
                return data;
            }

            return await keyVaultService.EncryptSensitiveDataAsync(data, cancellationToken);
        }

        public async Task<string> DecryptPersonalDataAsync(string encryptedData, CancellationToken cancellationToken = default)
        {
            if (keyVaultService == null)
            {
                Log.Warning("Key Vault no disponible, asumiendo datos no cifrados");
                return encryptedData;
            }

            return await keyVaultService.DecryptSensitiveDataAsync(encryptedData, cancellationToken);
        }
    }
}
