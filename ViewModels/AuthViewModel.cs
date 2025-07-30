using System;
using ReactiveUI;
using System.Reactive;
using PasswordManager.Views;

namespace PasswordManager.ViewModels;

public class AuthViewModel : ViewModelBase
{
  private string _password = string.Empty;
  public string Password
  {
    get => _password;
    set => this.RaiseAndSetIfChanged(ref _password, value);
  }

  // Comando para validar login
  public ReactiveCommand<Unit, Unit> LoginCommand { get; }

  // Acción externa que se ejecuta si el login tiene éxito
  public Action? OnLoginSuccess { get; set; }

  public AuthViewModel()
  {
    LoginCommand = ReactiveCommand.Create(ExecuteLogin);
  }

  private void ExecuteLogin()
  {
    // Puedes cambiar esta lógica según tu flujo
    if (!string.IsNullOrWhiteSpace(Password) && Password.Length >= 4)
    {
      OnLoginSuccess?.Invoke();
    }
    else
    {
      // En el futuro: mostrar un mensaje de error al usuario
      // ErrorMessage = "Contraseña inválida.";
    }
  }
}
