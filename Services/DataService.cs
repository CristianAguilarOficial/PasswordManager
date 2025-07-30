using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PasswordManager.Models;

namespace PasswordManager.Services;

public class DataService
{
  private readonly string _filePath;
  private readonly CryptoService _cryptoService;

  public DataService(string filePath, CryptoService cryptoService)
  {
    _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));

    // Asegurar que el directorio existe
    var directory = Path.GetDirectoryName(_filePath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    {
      Directory.CreateDirectory(directory);
    }
  }

  /// <summary>
  /// Guarda la lista de entradas encriptadas en el archivo
  /// </summary>
  /// <param name="entries">Lista de entradas de contraseñas</param>
  /// <param name="masterPassword">Contraseña maestra para encriptar</param>
  public void SaveData(List<PasswordEntry> entries, string masterPassword)
  {
    try
    {
      if (entries == null)
        throw new ArgumentNullException(nameof(entries));

      if (string.IsNullOrEmpty(masterPassword))
        throw new ArgumentException("La contraseña maestra no puede estar vacía", nameof(masterPassword));

      // Configurar opciones de serialización JSON
      var options = new JsonSerializerOptions
      {
        WriteIndented = false, // Compacto para ahorrar espacio
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      };

      // Serializar a JSON
      var json = JsonSerializer.Serialize(entries, options);

      // Encriptar los datos
      var encrypted = _cryptoService.Encrypt(json, masterPassword);

      // Escribir archivo con backup temporal
      var tempFilePath = _filePath + ".tmp";
      File.WriteAllBytes(tempFilePath, encrypted);

      // Reemplazar archivo original solo si la escritura fue exitosa
      if (File.Exists(_filePath))
      {
        File.Replace(tempFilePath, _filePath, _filePath + ".bak");
      }
      else
      {
        File.Move(tempFilePath, _filePath);
      }

      // Limpiar archivo de backup si es muy antiguo (opcional)
      CleanupOldBackups();
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException($"Error al guardar los datos: {ex.Message}", ex);
    }
  }

  /// <summary>
  /// Carga y desencripta la lista de entradas desde el archivo
  /// </summary>
  /// <param name="masterPassword">Contraseña maestra para desencriptar</param>
  /// <returns>Lista de entradas o null si la contraseña es incorrecta</returns>
  public List<PasswordEntry>? LoadData(string masterPassword)
  {
    try
    {
      if (string.IsNullOrEmpty(masterPassword))
        throw new ArgumentException("La contraseña maestra no puede estar vacía", nameof(masterPassword));

      // Si el archivo no existe, devolver lista vacía
      if (!File.Exists(_filePath))
      {
        return new List<PasswordEntry>();
      }

      // Verificar que el archivo no esté vacío
      var fileInfo = new FileInfo(_filePath);
      if (fileInfo.Length == 0)
      {
        return new List<PasswordEntry>();
      }

      // Leer datos encriptados
      var encrypted = File.ReadAllBytes(_filePath);

      // Intentar desencriptar
      var json = _cryptoService.Decrypt(encrypted, masterPassword);

      // Si la desencriptación falló (contraseña incorrecta)
      if (json == null)
      {
        return null;
      }

      // Deserializar JSON
      var options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      };

      var entries = JsonSerializer.Deserialize<List<PasswordEntry>>(json, options);

      // Validar integridad de los datos cargados
      return ValidateAndCleanEntries(entries ?? new List<PasswordEntry>());
    }
    catch (JsonException ex)
    {
      throw new InvalidOperationException($"Error al deserializar los datos: archivo corrupto. {ex.Message}", ex);
    }
    catch (FileNotFoundException)
    {
      // Archivo no encontrado, devolver lista vacía
      return new List<PasswordEntry>();
    }
    catch (UnauthorizedAccessException ex)
    {
      throw new InvalidOperationException($"No se tiene acceso al archivo de datos: {ex.Message}", ex);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException($"Error al cargar los datos: {ex.Message}", ex);
    }
  }

  /// <summary>
  /// Verifica si existe un archivo de datos
  /// </summary>
  /// <returns>True si existe el archivo de datos</returns>
  public bool DataFileExists()
  {
    return File.Exists(_filePath);
  }

  /// <summary>
  /// Obtiene información sobre el archivo de datos
  /// </summary>
  /// <returns>Información del archivo o null si no existe</returns>
  public FileInfo? GetDataFileInfo()
  {
    if (!File.Exists(_filePath))
      return null;

    return new FileInfo(_filePath);
  }

  /// <summary>
  /// Crea una copia de seguridad manual del archivo de datos
  /// </summary>
  /// <param name="backupPath">Ruta donde crear la copia de seguridad</param>
  public void CreateBackup(string backupPath)
  {
    try
    {
      if (!File.Exists(_filePath))
        throw new FileNotFoundException("No existe archivo de datos para respaldar");

      if (string.IsNullOrEmpty(backupPath))
        throw new ArgumentException("La ruta de backup no puede estar vacía", nameof(backupPath));

      var backupDirectory = Path.GetDirectoryName(backupPath);
      if (!string.IsNullOrEmpty(backupDirectory) && !Directory.Exists(backupDirectory))
      {
        Directory.CreateDirectory(backupDirectory);
      }

      File.Copy(_filePath, backupPath, overwrite: true);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException($"Error al crear backup: {ex.Message}", ex);
    }
  }

  /// <summary>
  /// Restaura datos desde una copia de seguridad
  /// </summary>
  /// <param name="backupPath">Ruta del archivo de backup</param>
  /// <param name="masterPassword">Contraseña para verificar el backup</param>
  /// <returns>True si la restauración fue exitosa</returns>
  public bool RestoreFromBackup(string backupPath, string masterPassword)
  {
    try
    {
      if (!File.Exists(backupPath))
        throw new FileNotFoundException($"Archivo de backup no encontrado: {backupPath}");

      // Verificar que el backup sea válido intentando cargarlo
      var tempDataService = new DataService(backupPath, _cryptoService);
      var testLoad = tempDataService.LoadData(masterPassword);

      if (testLoad == null)
        return false; // Contraseña incorrecta o archivo corrupto

      // Si la verificación fue exitosa, copiar el backup
      File.Copy(backupPath, _filePath, overwrite: true);
      return true;
    }
    catch
    {
      return false;
    }
  }

  /// <summary>
  /// Valida y limpia las entradas cargadas
  /// </summary>
  /// <param name="entries">Lista de entradas a validar</param>
  /// <returns>Lista de entradas válidas</returns>
  private List<PasswordEntry> ValidateAndCleanEntries(List<PasswordEntry> entries)
  {
    var validEntries = new List<PasswordEntry>();

    foreach (var entry in entries)
    {
      // Verificar que la entrada tenga un ID válido
      if (string.IsNullOrEmpty(entry.Id))
      {
        entry.Id = Guid.NewGuid().ToString();
      }

      // Asegurar que los campos no sean null
      entry.Website ??= string.Empty;
      entry.Url ??= string.Empty;
      entry.Username ??= string.Empty;
      entry.Email ??= string.Empty;
      entry.Password ??= string.Empty;
      entry.Notes ??= string.Empty;

      validEntries.Add(entry);
    }

    return validEntries;
  }

  /// <summary>
  /// Limpia archivos de backup antiguos
  /// </summary>
  private void CleanupOldBackups()
  {
    try
    {
      var backupFile = _filePath + ".bak";
      if (File.Exists(backupFile))
      {
        var backupInfo = new FileInfo(backupFile);
        // Eliminar backups de más de 7 días
        if (DateTime.Now - backupInfo.CreationTime > TimeSpan.FromDays(7))
        {
          File.Delete(backupFile);
        }
      }
    }
    catch
    {
      // Ignorar errores de limpieza de backup
    }
  }

  /// <summary>
  /// Verifica la integridad del archivo de datos
  /// </summary>
  /// <param name="masterPassword">Contraseña maestra</param>
  /// <returns>True si el archivo es válido</returns>
  public bool VerifyDataIntegrity(string masterPassword)
  {
    try
    {
      var data = LoadData(masterPassword);
      return data != null;
    }
    catch
    {
      return false;
    }
  }

  /// <summary>
  /// Obtiene estadísticas del archivo de datos
  /// </summary>
  /// <returns>Diccionario con estadísticas</returns>
  public Dictionary<string, object> GetDataStatistics()
  {
    var stats = new Dictionary<string, object>();

    try
    {
      if (File.Exists(_filePath))
      {
        var fileInfo = new FileInfo(_filePath);
        stats["FileExists"] = true;
        stats["FileSizeBytes"] = fileInfo.Length;
        stats["LastModified"] = fileInfo.LastWriteTime;
        stats["Created"] = fileInfo.CreationTime;
      }
      else
      {
        stats["FileExists"] = false;
        stats["FileSizeBytes"] = 0;
      }
    }
    catch (Exception ex)
    {
      stats["Error"] = ex.Message;
    }

    return stats;
  }
}