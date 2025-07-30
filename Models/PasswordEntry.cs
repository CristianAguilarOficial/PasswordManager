using System;
namespace PasswordManager.Models;

public class PasswordEntry
{
  public string Id { get; set; } = Guid.NewGuid().ToString();
  public string Website { get; set; } = string.Empty;
  public string Url { get; set; } = string.Empty;
  public string Username { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public string Notes { get; set; } = string.Empty;
}
