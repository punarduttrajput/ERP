using System.Security.Cryptography;
using System.Text;
using ERP.Shared.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ERP.Shared.Infrastructure;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly ILogger<EncryptionService> _logger;

    // Base64-encoded 32-byte fallback used only in development; log a warning so it is never silently used in prod.
    private const string DevFallbackKeyBase64 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        _logger = logger;
        var keyBase64 = configuration["Encryption:Key"];
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            _logger.LogWarning("Encryption:Key is not configured. Using insecure dev fallback key. Set a proper 32-byte base64 key in production.");
            keyBase64 = DevFallbackKeyBase64;
        }
        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption:Key must be a base64-encoded 32-byte (256-bit) key.");
    }

    public string Encrypt(string plaintext)
    {
        var encrypted = EncryptBytes(Encoding.UTF8.GetBytes(plaintext));
        return Convert.ToBase64String(encrypted);
    }

    public string Decrypt(string ciphertext)
    {
        var decrypted = DecryptBytes(Convert.FromBase64String(ciphertext));
        return Encoding.UTF8.GetString(decrypted);
    }

    public byte[] EncryptBytes(byte[] data)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(data, 0, data.Length);

        // Output: IV (16 bytes) + ciphertext
        var result = new byte[aes.IV.Length + ciphertext.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(ciphertext, 0, result, aes.IV.Length, ciphertext.Length);
        return result;
    }

    public byte[] DecryptBytes(byte[] data)
    {
        const int ivLength = 16;
        var iv = new byte[ivLength];
        var ciphertext = new byte[data.Length - ivLength];
        Buffer.BlockCopy(data, 0, iv, 0, ivLength);
        Buffer.BlockCopy(data, ivLength, ciphertext, 0, ciphertext.Length);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }
}
