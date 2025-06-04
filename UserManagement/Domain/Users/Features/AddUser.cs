using AutoMapper.Features;
using MediatR;
using Serilog;
using UserManagement.Domain.Users.Dtos;
using UserManagement.Domain.Users.Mappings;
using UserManagement.Domain.Users.Services;
using UserManagement.Services;

namespace UserManagement.Domain.Users.Features
{
    public static class AddUser
    {
        public sealed record Command(UserForCreationDto UserToAdd) : IRequest<UserDto>;
        public sealed class Handler(IUserRepository userRepository,
            IUnitOfWork unitOfWork, IUserPhotoService userPhotoService,
            IUserSecurityService userSecurityService,
            IFeatureToggleService featureToggleService,
            IAppConfigurationService appConfigurationService)
        : IRequestHandler<Command, UserDto>
        {
            public async Task<UserDto> Handle(Command request, CancellationToken cancellationToken)
            {

                // Verificar feature flags
                var userEncryptionEnabled = await featureToggleService.IsEnabledAsync("UserEncryption");
                var photoUploadEnabled = await featureToggleService.IsEnabledAsync("UserPhotoUpload");

                // Obtener configuraciones dinámicas
                var maxPhotoSize = await appConfigurationService.GetIntAsync("UserManagement:MaxPhotoSizeMB", 5);
                var allowedPhotoTypes = await appConfigurationService.GetStringAsync("UserManagement:AllowedPhotoTypes", "jpg,png,gif");

                Log.Information("Creando usuario con configuración: Cifrado={UserEncryption}, FotoUpload={PhotoUpload}, MaxSize={MaxSize}MB",
                    userEncryptionEnabled, photoUploadEnabled, maxPhotoSize);


                var userToAdd = request.UserToAdd.ToUserForCreation();

                var hashedPassword = await userSecurityService.HashPasswordAsync(userToAdd.PasswordHash);
                userToAdd.PasswordHash = hashedPassword;

                var user = User.Create(userToAdd);

                if (userEncryptionEnabled)
                {

                    // Cifrar datos sensibles si están presentes
                    if (!string.IsNullOrWhiteSpace(request.UserToAdd.PhoneNumber))
                    {
                        user.EncryptedPhoneNumber = await userSecurityService.EncryptPersonalDataAsync(
                            request.UserToAdd.PhoneNumber, cancellationToken);
                    }

                    if (!string.IsNullOrWhiteSpace(request.UserToAdd.Address))
                    {
                        user.EncryptedAddress = await userSecurityService.EncryptPersonalDataAsync(
                            request.UserToAdd.Address, cancellationToken);
                    }
                }

                if (photoUploadEnabled && !string.IsNullOrWhiteSpace(request.UserToAdd.PhotoBase64))
                {
                    Log.Information("Procesando foto para nuevo usuario {Email}", request.UserToAdd.Email);

                    // Subir la foto usando el ID del usuario
                    var blobName = await userPhotoService.UploadUserPhotoFromBase64Async(
                        user.Id,
                        request.UserToAdd.PhotoBase64,
                        cancellationToken);

                    // Obtener la URL y actualizar el usuario
                    var photoUrl = await userPhotoService.GetUserPhotoUrlAsync(blobName, cancellationToken);

                    user.PhotoBlobName = blobName;
                    user.PhotoUrl = photoUrl;

                }

                await userRepository.Add(user, cancellationToken);
                await unitOfWork.CommitChanges(cancellationToken);

                return user.ToUserDto();
            }
        }
    }
}
