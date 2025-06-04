using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Secrets;
using Serilog;
using System.Text;

namespace UserManagement.Services
{
    public interface IKeyVaultService : IUserManagementScopedService
    {
        Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
        Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);
        Task<string> GetConnectionStringAsync(string name, CancellationToken cancellationToken = default);
        Task<string> GetJwtSecretKeyAsync(CancellationToken cancellationToken = default);
        Task<string> EncryptSensitiveDataAsync(string data, CancellationToken cancellationToken = default);
        Task<string> DecryptSensitiveDataAsync(string encryptedData, CancellationToken cancellationToken = default);
    }
    public class KeyVaultService : IKeyVaultService
    {
        private readonly SecretClient? secretClient;
        private readonly KeyClient? keyClient;
        private readonly bool isAvailable;

        public KeyVaultService(
            SecretClient? secretClient,
            KeyClient? keyClient)
        {
            this.secretClient = secretClient;
            this.keyClient = keyClient;
            isAvailable = secretClient != null && keyClient != null;

            if (!isAvailable)
            {
                Log.Warning("Key Vault no está disponible. Las operaciones de cifrado estarán deshabilitadas.");
            }
        }

        public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            if (!isAvailable || secretClient == null)
            {
                throw new InvalidOperationException("Key Vault no está disponible");
            }

            try
            {
                var secret = await secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
                Log.Debug("Secreto recuperado exitosamente: {SecretName}", secretName);
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al recuperar secreto: {SecretName}", secretName);
                throw;
            }
        }

        public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
        {
            if (!isAvailable || secretClient == null)
            {
                throw new InvalidOperationException("Key Vault no está disponible");
            }

            try
            {
                await secretClient.SetSecretAsync(secretName, secretValue, cancellationToken);
                Log.Information("Secreto guardado exitosamente: {SecretName}", secretName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al guardar secreto: {SecretName}", secretName);
                throw;
            }
        }

        public async Task<string> GetConnectionStringAsync(string name, CancellationToken cancellationToken = default)
        {
            return await GetSecretAsync($"ConnectionStrings--{name}", cancellationToken);
        }

        public async Task<string> GetJwtSecretKeyAsync(CancellationToken cancellationToken = default)
        {
            return await GetSecretAsync("Jwt--SecretKey", cancellationToken);
        }

        public async Task<string> EncryptSensitiveDataAsync(string data, CancellationToken cancellationToken = default)
        {
            if (!isAvailable || keyClient == null)
            {
                Log.Warning("Key Vault no disponible, devolviendo datos sin cifrar");
                return data; // En desarrollo, devolver sin cifrar
            }

            try
            {
                var key = await keyClient.GetKeyAsync("user-data-encryption-key", cancellationToken: cancellationToken);
                var cryptoClient = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());

                var dataBytes = Encoding.UTF8.GetBytes(data);
                var encryptResult = await cryptoClient.EncryptAsync(EncryptionAlgorithm.RsaOaep, dataBytes, cancellationToken);

                var encryptedBase64 = Convert.ToBase64String(encryptResult.Ciphertext);
                Log.Debug("Datos cifrados exitosamente");

                return encryptedBase64;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al cifrar datos, devolviendo sin cifrar");
                return data; // Fallback para desarrollo
            }
        }

        public async Task<string> DecryptSensitiveDataAsync(string encryptedData, CancellationToken cancellationToken = default)
        {
            if (!isAvailable || keyClient == null)
            {
                Log.Warning("Key Vault no disponible, asumiendo datos sin cifrar");
                return encryptedData; // En desarrollo, asumir que no está cifrado
            }

            try
            {
                var key = await keyClient.GetKeyAsync("user-data-encryption-key", cancellationToken: cancellationToken);
                var cryptoClient = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());

                var encryptedBytes = Convert.FromBase64String(encryptedData);
                var decryptResult = await cryptoClient.DecryptAsync(EncryptionAlgorithm.RsaOaep, encryptedBytes, cancellationToken);

                var decryptedData = Encoding.UTF8.GetString(decryptResult.Plaintext);
                Log.Debug("Datos descifrados exitosamente");

                return decryptedData;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error al descifrar datos, asumiendo que no están cifrados");
                return encryptedData; // Fallback para datos no cifrados
            }
        }
    }
}
