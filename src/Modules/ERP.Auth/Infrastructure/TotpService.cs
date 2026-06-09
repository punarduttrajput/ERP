using System.Security.Cryptography;
using System.Text;
using ERP.Auth.Application.Services;

namespace ERP.Auth.Infrastructure;

public sealed class TotpService : ITotpService
{
    private const int TimeStepSeconds = 30;
    private const int Digits = 6;
    // Allow ±1 step (±30 s) to account for clock drift between client and server.
    private const int WindowSteps = 1;

    public string GenerateSecret()
    {
        var key = new byte[20];
        RandomNumberGenerator.Fill(key);
        return EncodeBase32(key);
    }

    public string GetQrCodeUri(string secret, string email, string issuer)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail  = Uri.EscapeDataString(email);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits={Digits}&period={TimeStepSeconds}";
    }

    public bool Verify(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != Digits) return false;

        byte[] keyBytes;
        try { keyBytes = DecodeBase32(secret); }
        catch { return false; }

        var step = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TimeStepSeconds;

        for (var delta = -WindowSteps; delta <= WindowSteps; delta++)
        {
            if (ComputeTotp(keyBytes, step + delta) == code)
                return true;
        }

        return false;
    }

    private static string ComputeTotp(byte[] key, long timeStep)
    {
        // RFC 6238 §4: message is the 8-byte big-endian counter
        var msg = new byte[8];
        for (var i = 7; i >= 0; i--)
        {
            msg[i] = (byte)(timeStep & 0xff);
            timeStep >>= 8;
        }

        using var hmac = new HMACSHA1(key);
        var hash   = hmac.ComputeHash(msg);
        var offset = hash[^1] & 0x0f;

        var code = ((hash[offset]     & 0x7f) << 24)
                 | ((hash[offset + 1] & 0xff) << 16)
                 | ((hash[offset + 2] & 0xff) << 8)
                 |  (hash[offset + 3] & 0xff);

        return (code % (int)Math.Pow(10, Digits)).ToString($"D{Digits}");
    }

    private static string EncodeBase32(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var sb       = new StringBuilder();
        var buffer   = 0;
        var bitsLeft = 0;

        foreach (var b in data)
        {
            buffer    = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                sb.Append(alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
            sb.Append(alphabet[(buffer << (5 - bitsLeft)) & 31]);

        return sb.ToString();
    }

    private static byte[] DecodeBase32(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        base32 = base32.TrimEnd('=').ToUpperInvariant();

        var byteCount = base32.Length * 5 / 8;
        var result    = new byte[byteCount];
        var buffer    = 0;
        var bitsLeft  = 0;
        var index     = 0;

        foreach (var c in base32)
        {
            var val = alphabet.IndexOf(c);
            if (val < 0) throw new FormatException($"Invalid Base32 character '{c}'.");

            buffer    = (buffer << 5) | val;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                result[index++] = (byte)(buffer >> (bitsLeft - 8));
                bitsLeft       -= 8;
            }
        }

        return result;
    }
}
