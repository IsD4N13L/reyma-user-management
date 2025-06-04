using MediatR;
using Serilog;
using UserManagement.Domain.Users.Dtos;
using UserManagement.Domain.Users.Mappings;
using UserManagement.Domain.Users.Services;

namespace UserManagement.Domain.Users.Features
{
    public static class LogInUser
    {
        public sealed record Command(LoginDto Login) : IRequest<LoginResponseDto>;

        public sealed class Handler(IUserRepository userRepository,
            IUserSecurityService userSecurityService) : IRequestHandler<Command, LoginResponseDto>
        {

            public async Task<LoginResponseDto> Handle(Command request, CancellationToken cancellationToken)
            {
                try
                {
                    var user = await userRepository.GetByEmailAsync(request.Login.Email, cancellationToken);
                    if (user == null)
                    {
                        Log.Warning("Intento de login con email inexistente: {Email}", request.Login.Email);
                        throw new UnauthorizedAccessException("Credenciales inválidas");
                    }

                    if (!user.Active)
                    {
                        Log.Warning("Intento de login con usuario inactivo: {Email}", request.Login.Email);
                        throw new UnauthorizedAccessException("Cuenta inactiva");
                    }

                    var isValidPassword = await userSecurityService.VerifyPasswordAsync(
                        request.Login.Password, user.PasswordHash);

                    if (!isValidPassword)
                    {
                        Log.Warning("Intento de login con contraseña incorrecta: {Email}", request.Login.Email);
                        throw new UnauthorizedAccessException("Credenciales inválidas");
                    }

                    // Generar JWT token usando Key Vault
                    var token = await userSecurityService.GenerateJwtTokenAsync(user, cancellationToken);

                    // Actualizar último login
                    user.LastLoginAt = DateTime.UtcNow;
                    userRepository.Update(user);

                    Log.Information("Login exitoso para usuario: {UserId}", user.Id);

                    return new LoginResponseDto
                    {
                        Token = token,
                        User = user.ToUserDto(),
                        ExpiresAt = DateTime.UtcNow.AddHours(24)
                    };
                }
                catch (Exception ex) when (!(ex is UnauthorizedAccessException))
                {
                    Log.Error(ex, "Error durante login para email: {Email}", request.Login.Email);
                    throw;
                }
            }
        }
    }
}
