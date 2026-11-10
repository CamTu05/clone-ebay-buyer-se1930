using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
namespace Buyer.Infrastructure.BuyerSchema;
public static class ReceiptCipher
{
    public static string Protect(string value, IConfiguration config)
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(config["Jwt:SigningKey"] ?? throw new InvalidOperationException("JWT signing key required")));
        var nonce = RandomNumberGenerator.GetBytes(12); var data = Encoding.UTF8.GetBytes(value); var cipher = new byte[data.Length]; var tag = new byte[16];
        using var aes = new AesGcm(key, 16); aes.Encrypt(nonce, data, cipher, tag); return "enc:" + Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }
    public static string Unprotect(string value, IConfiguration config)
    {
        if (!value.StartsWith("enc:")) return value;
        var bytes = Convert.FromBase64String(value[4..]); var data = new byte[bytes.Length - 28]; var key = SHA256.HashData(Encoding.UTF8.GetBytes(config["Jwt:SigningKey"] ?? throw new InvalidOperationException("JWT signing key required")));
        using var aes = new AesGcm(key, 16); aes.Decrypt(bytes.AsSpan(0, 12), bytes.AsSpan(28), bytes.AsSpan(12, 16), data); return Encoding.UTF8.GetString(data);
    }
}
