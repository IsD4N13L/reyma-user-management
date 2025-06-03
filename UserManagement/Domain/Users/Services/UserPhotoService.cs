using Azure.Storage.Blobs;
using System.Text.RegularExpressions;
using UserManagement.Services;
using Serilog;
using Azure.Storage.Blobs.Models;
using System.Linq;

namespace UserManagement.Domain.Users.Services
{

    public interface IUserPhotoService : IUserManagementScopedService
    {
        Task<string> UploadUserPhotoFromBase64Async(Guid userId, string base64Image, CancellationToken cancellationToken = default);
        Task<bool> DeleteUserPhotoAsync(string blobName, CancellationToken cancellationToken = default);
        Task<string> GetUserPhotoUrlAsync(string blobName, CancellationToken cancellationToken = default);
    }
    public class UserPhotoService : IUserPhotoService
    {

        private readonly BlobServiceClient blobServiceClient;
        private readonly BlobContainerClient containerClient;
        private readonly string containerName;

        public UserPhotoService(
        BlobServiceClient blobServiceClient,
        IConfiguration configuration)
        {
            this.blobServiceClient = blobServiceClient;
            this.containerName = Environment.GetEnvironmentVariable("AzureStorage__ContainerName")!;
            this.containerClient = this.blobServiceClient.GetBlobContainerClient(this.containerName);
        }

        public async Task<string> UploadUserPhotoFromBase64Async(Guid userId, string base64Image, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validar y procesar base64
                var (imageBytes, fileExtension, contentType) = ProcessBase64Image(base64Image);

                // Validar tamaño (5MB máximo)
                if (imageBytes.Length > 5 * 1024 * 1024)
                {
                    throw new ArgumentException("El archivo es demasiado grande. Máximo 5MB");
                }

                // Crear nombre único para el blob
                var blobName = $"users/{userId}/photo_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";

                // Crear container si no existe
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

                // Subir archivo
                var blobClient = containerClient.GetBlobClient(blobName);

                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                };

                using var stream = new MemoryStream(imageBytes);
                await blobClient.UploadAsync(
                    stream,
                    new BlobUploadOptions
                    {
                        HttpHeaders = blobHttpHeaders,
                        Conditions = null
                    },
                    cancellationToken);

                Log.Information("Foto subida exitosamente para usuario {UserId}: {BlobName}", userId, blobName);

                return blobName;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al subir foto para usuario {UserId}", userId);
                throw;
            }
        }
        public async Task<bool> DeleteUserPhotoAsync(string blobName, CancellationToken cancellationToken = default)
        {
            try
            {
                var blobClient = containerClient.GetBlobClient(blobName);
                var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

                Log.Information("Foto eliminada: {BlobName}, Existía: {Existed}", blobName, response.Value);

                return response.Value;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al eliminar foto: {BlobName}", blobName);
                return false;
            }
        }
        public async Task<string> GetUserPhotoUrlAsync(string blobName, CancellationToken cancellationToken = default)
        {
            try
            {
                var blobClient = containerClient.GetBlobClient(blobName);

                // Verificar si existe el blob
                var exists = await blobClient.ExistsAsync(cancellationToken);
                if (!exists.Value)
                {
                    return string.Empty;
                }

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al obtener URL de foto: {BlobName}", blobName);
                return string.Empty;
            }
        }

        private static (byte[] imageBytes, string fileExtension, string contentType) ProcessBase64Image(string base64Image)
        {
            if (string.IsNullOrWhiteSpace(base64Image))
            {
                throw new ArgumentException("La imagen base64 es requerida");
            }

            // Regex para extraer el tipo de contenido del data URL
            var dataUrlPattern = @"^data:image/(?<type>.+?);base64,(?<data>.+)$";
            var match = Regex.Match(base64Image, dataUrlPattern);

            string imageType;
            string base64Data;

            if (match.Success)
            {
                // Es un data URL completo: data:image/jpeg;base64,/9j/4AAQ...
                imageType = match.Groups["type"].Value.ToLowerInvariant();
                base64Data = match.Groups["data"].Value;
            }
            else
            {
                // Es solo el string base64 sin prefijo, asumir JPEG
                imageType = "jpeg";
                base64Data = base64Image;
            }

            // Validar tipo de imagen
            var allowedTypes = new Dictionary<string, (string extension, string contentType)>
        {
            { "jpeg", (".jpg", "image/jpeg") },
            { "jpg", (".jpg", "image/jpeg") },
            { "png", (".png", "image/png") },
            { "gif", (".gif", "image/gif") },
            { "webp", (".webp", "image/webp") }
        };

            if (!allowedTypes.ContainsKey(imageType))
            {
                throw new ArgumentException($"Tipo de imagen no válido: {imageType}. Tipos permitidos: {string.Join(", ", allowedTypes.Keys)}");
            }

            try
            {
                var imageBytes = Convert.FromBase64String(base64Data);
                var (extension, contentType) = allowedTypes[imageType];

                return (imageBytes, extension, contentType);
            }
            catch (FormatException)
            {
                throw new ArgumentException("El formato base64 de la imagen no es válido");
            }
        }
    }
}
