using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Services;

public class CryptoService
{
  private const int KeySize = 32; // 256 bits
  private const int IvSize = 16;  // 128 bits
  private const int SaltSize = 16; // 128 bits
  private const int Iterations = 100_000; // PBKDF2 iterations

  /// <summary>
  /// Encripta un texto plano usando AES-256 con una contraseña derivada mediante PBKDF2
  /// </summary>
  /// <param name="plainText">Texto a encriptar</param>
  /// <param name="password">Contraseña maestra</param>
  /// <returns>Array de bytes que contiene: salt + iv + datos encriptados</returns>
  public byte[] Encrypt(string plainText, string password)
  {
    try
    {
      if (string.IsNullOrEmpty(plainText))
        throw new ArgumentException("El texto a encriptar no puede estar vacío", nameof(plainText));

      if (string.IsNullOrEmpty(password))
        throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

      // Generar salt aleatorio
      var salt = RandomNumberGenerator.GetBytes(SaltSize);

      // Derivar clave de la contraseña
      var key = DeriveKey(password, salt);

      using var aes = Aes.Create();
      aes.Key = key;
      aes.GenerateIV(); // Generar IV aleatorio
      var iv = aes.IV;

      using var encryptor = aes.CreateEncryptor();
      var plainBytes = Encoding.UTF8.GetBytes(plainText);
      var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

      // Combinar salt + iv + datos encriptados
      return salt.Concat(iv).Concat(encryptedBytes).ToArray();
    }
    catch (Exception ex)
    {
      throw new CryptographicException($"Error durante la encriptación: {ex.Message}", ex);
    }
  }

  /// <summary>
  /// Desencripta datos encriptados usando la contraseña proporcionada
  /// </summary>
  /// <param name="cipherData">Datos encriptados (salt + iv + datos encriptados)</param>
  /// <param name="password">Contraseña maestra</param>
  /// <returns>Texto desencriptado o null si la contraseña es incorrecta</returns>
  public string? Decrypt(byte[] cipherData, string password)
  {
    try
    {
      if (cipherData == null || cipherData.Length == 0)
        throw new ArgumentException("Los datos encriptados no pueden estar vacíos", nameof(cipherData));

      if (string.IsNullOrEmpty(password))
        throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

      // Verificar que los datos tengan el tamaño mínimo
      if (cipherData.Length < SaltSize + IvSize + 1)
        throw new ArgumentException("Los datos encriptados tienen un formato inválido", nameof(cipherData));

      // Extraer componentes
      var salt = cipherData[..SaltSize];
      var iv = cipherData[SaltSize..(SaltSize + IvSize)];
      var ciphertext = cipherData[(SaltSize + IvSize)..];

      // Derivar clave usando el mismo salt
      var key = DeriveKey(password, salt);

      using var aes = Aes.Create();
      aes.Key = key;
      aes.IV = iv;

      using var decryptor = aes.CreateDecryptor();
      var decryptedBytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

      return Encoding.UTF8.GetString(decryptedBytes);
    }
    catch (CryptographicException)
    {
      // Error criptográfico - probablemente contraseña incorrecta
      return null;
    }
    catch (ArgumentException)
    {
      // Error en los argumentos - datos corruptos o formato inválido
      return null;
    }
    catch (Exception ex)
    {
      // Cualquier otro error durante la desencriptación
      throw new CryptographicException($"Error durante la desencriptación: {ex.Message}", ex);
    }
  }

  /// <summary>
  /// Deriva una clave criptográfica de una contraseña usando PBKDF2
  /// </summary>
  /// <param name="password">Contraseña base</param>
  /// <param name="salt">Salt para la derivación</param>
  /// <returns>Clave derivada de 256 bits</returns>
  private byte[] DeriveKey(string password, byte[] salt)
  {
    try
    {
      using var pbkdf2 = new Rfc2898DeriveBytes(
          password,
          salt,
          Iterations,
          HashAlgorithmName.SHA256);

      return pbkdf2.GetBytes(KeySize);
    }
    catch (Exception ex)
    {
      throw new CryptographicException($"Error durante la derivación de clave: {ex.Message}", ex);
    }
  }

  /// <summary>
  /// Verifica si una contraseña es correcta intentando desencriptar datos de prueba
  /// </summary>
  /// <param name="encryptedData">Datos encriptados para verificar</param>
  /// <param name="password">Contraseña a verificar</param>
  /// <returns>True si la contraseña es correcta, False en caso contrario</returns>
  public bool VerifyPassword(byte[] encryptedData, string password)
  {
    if (encryptedData == null || encryptedData.Length == 0)
      return false;

    try
    {
      var result = Decrypt(encryptedData, password);
      return result != null;
    }
    catch
    {
      return false;
    }
  }

  /// <summary>
  /// Genera un hash de la contraseña para verificación rápida (opcional)
  /// </summary>
  /// <param name="password">Contraseña a hashear</param>
  /// <param name="salt">Salt para el hash</param>
  /// <returns>Hash de la contraseña</returns>
  public string HashPassword(string password, byte[] salt)
  {
    try
    {
      using var pbkdf2 = new Rfc2898DeriveBytes(
          password,
          salt,
          Iterations,
          HashAlgorithmName.SHA256);

      var hash = pbkdf2.GetBytes(32);
      return Convert.ToBase64String(salt.Concat(hash).ToArray());
    }
    catch (Exception ex)
    {
      throw new CryptographicException($"Error durante el hash de contraseña: {ex.Message}", ex);
    }
  }
}