using System;
using System.IO;
using Avalonia.Controls;
using PasswordManager.ViewModels;
using PasswordManager.Services;

namespace PasswordManager.Views;

public partial class AuthWindow : Window
{
  public AuthWindow()
  {
    InitializeComponent();

    if (DataContext is AuthViewModel vm)
    {
      vm.OnLoginSuccess = () =>
      {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "PasswordManager");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "passwords.enc");

        var crypto = new CryptoService();
        var dataService = new DataService(path, crypto);

        var mainVM = new MainWindowViewModel(dataService, vm.Password);
        var mainWindow = new MainWindow
        {
          DataContext = mainVM
        };

        mainWindow.Show();
        Close();
      };
    }
  }
}
