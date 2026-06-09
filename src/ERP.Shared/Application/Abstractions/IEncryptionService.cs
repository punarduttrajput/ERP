namespace ERP.Shared.Application.Abstractions;

public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    byte[] EncryptBytes(byte[] data);
    byte[] DecryptBytes(byte[] data);
}
