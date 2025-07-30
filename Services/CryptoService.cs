using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Services;

public class CryptoService
{
  private const int KeySize = 32; // 256 bits
  private const int IvSize = 16;
  private const int SaltSize = 16;
  private const int Iterations = 100_000;

  public byte[] Encrypt(string plainText, string password)
  {
    var salt = RandomNumberGenerator.GetBytes(SaltSize);
    var key = DeriveKey(password, salt);

    using var aes = Aes.Create();
    aes.Key = key;
    aes.GenerateIV();
    var iv = aes.IV;

    using var encryptor = aes.CreateEncryptor();
    var plainBytes = Encoding.UTF8.GetBytes(plainText);
    var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

    return salt.Concat(iv).Concat(encryptedBytes).ToArray();
  }

  public string Decrypt(byte[] cipherData, string password)
  {
    var salt = cipherData[..SaltSize];
    var iv = cipherData[SaltSize..(SaltSize + IvSize)];
    var ciphertext = cipherData[(SaltSize + IvSize)..];

    var key = DeriveKey(password, salt);

    using var aes = Aes.Create();
    aes.Key = key;
    aes.IV = iv;

    using var decryptor = aes.CreateDecryptor();
    var decryptedBytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

    return Encoding.UTF8.GetString(decryptedBytes);
  }

  private byte[] DeriveKey(string password, byte[] salt)
  {
    using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
    return pbkdf2.GetBytes(KeySize);
  }
}
