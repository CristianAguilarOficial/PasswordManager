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
    _filePath = filePath;
    _cryptoService = cryptoService;
  }

  public void SaveData(List<PasswordEntry> entries, string masterPassword)
  {
    var json = JsonSerializer.Serialize(entries);
    var encrypted = _cryptoService.Encrypt(json, masterPassword);
    File.WriteAllBytes(_filePath, encrypted);
  }

  public List<PasswordEntry> LoadData(string masterPassword)
  {
    if (!File.Exists(_filePath)) return new();

    var encrypted = File.ReadAllBytes(_filePath);
    var json = _cryptoService.Decrypt(encrypted, masterPassword);
    return JsonSerializer.Deserialize<List<PasswordEntry>>(json) ?? new();
  }
}
