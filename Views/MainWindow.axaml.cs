using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PasswordManager.ViewModels;

namespace PasswordManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Configurar eventos de teclado
        KeyDown += MainWindow_KeyDown;

        // Configurar drag and drop
        SetupDragAndDrop();
    }

    /// <summary>
    /// Maneja los atajos de teclado
    /// </summary>
    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        // Ctrl+S para guardar
        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            viewModel.SaveCommand.Execute().Subscribe();
            e.Handled = true;
        }
        // Ctrl+N para nueva entrada
        else if (e.Key == Key.N && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            viewModel.AddEntryCommand.Execute().Subscribe();
            e.Handled = true;
        }
        // Delete para eliminar entrada seleccionada
        else if (e.Key == Key.Delete && viewModel.SelectedEntry != null)
        {
            viewModel.DeleteEntryCommand.Execute().Subscribe();
            e.Handled = true;
        }
        // Ctrl+C para copiar contraseña
        else if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control) && viewModel.SelectedEntry != null)
        {
            CopyPasswordToClipboard();
            e.Handled = true;
        }
        // F5 para recargar datos
        else if (e.Key == Key.F5)
        {
            // Recargar datos (podrías implementar un comando para esto)
            e.Handled = true;
        }
    }

    /// <summary>
    /// Maneja el clic en el botón de copiar contraseña
    /// </summary>
    private async void CopyPasswordButton_Click(object? sender, RoutedEventArgs e)
    {
        await CopyPasswordToClipboard();
    }

    /// <summary>
    /// Copia la contraseña de la entrada seleccionada al portapapeles
    /// </summary>
    private async Task CopyPasswordToClipboard()
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.SelectedEntry == null)
            return;

        try
        {
            var clipboard = GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(viewModel.SelectedEntry.Password);

                // Actualizar mensaje de estado
                viewModel.StatusMessage = "Contraseña copiada al portapapeles";

                // Limpiar el portapapeles después de 30 segundos por seguridad
                _ = Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(_ =>
                {
                    try
                    {
                        // Verificar si el contenido del portapapeles sigue siendo la contraseña
                        var currentClipboard = clipboard.GetTextAsync().Result;
                        if (currentClipboard == viewModel.SelectedEntry?.Password)
                        {
                            clipboard.SetTextAsync(string.Empty);
                        }
                    }
                    catch
                    {
                        // Ignorar errores al limpiar el portapapeles
                    }
                });
            }
        }
        catch (Exception ex)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.StatusMessage = $"Error al copiar contraseña: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Maneja el clic en el botón de abrir URL
    /// </summary>
    private void OpenUrlButton_Click(object? sender, RoutedEventArgs e)
    {
        OpenUrl();
    }

    /// <summary>
    /// Abre la URL de la entrada seleccionada en el navegador predeterminado
    /// </summary>
    private void OpenUrl()
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.SelectedEntry == null)
            return;

        try
        {
            var url = viewModel.SelectedEntry.Url;

            if (string.IsNullOrWhiteSpace(url))
            {
                viewModel.StatusMessage = "No hay URL configurada para esta entrada";
                return;
            }

            // Asegurar que la URL tenga protocolo
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }

            // Validar que sea una URL válida
            if (!Uri.TryCreate(url, UriKind.Absolute, out var validUri))
            {
                viewModel.StatusMessage = "La URL no tiene un formato válido";
                return;
            }

            // Abrir en el navegador predeterminado
            var processStartInfo = new ProcessStartInfo
            {
                FileName = validUri.ToString(),
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
            viewModel.StatusMessage = $"Abriendo {validUri.Host} en el navegador";
        }
        catch (Exception ex)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.StatusMessage = $"Error al abrir URL: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Maneja el evento de cierre de la ventana
    /// </summary>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        try
        {
            // Guardar datos antes de cerrar si hay cambios pendientes
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SaveCommand.Execute().Subscribe();
            }
        }
        catch
        {
            // Ignorar errores al guardar en el cierre
        }

        base.OnClosing(e);
    }

    /// <summary>
    /// Maneja cuando la ventana se carga completamente
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Configurar el foco inicial
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.StatusMessage = "Password Manager iniciado correctamente";
        }
    }

    /// <summary>
    /// Muestra un diálogo de confirmación para acciones destructivas
    /// </summary>
    private async Task<bool> ShowConfirmationDialog(string title, string message)
    {
        try
        {
            // En Avalonia 11+, puedes usar MessageBox o crear un diálogo personalizado
            // Por simplicidad, aquí asumo que tienes una implementación de diálogo

            // Implementación básica sin diálogo visual por ahora
            // En una implementación real, mostrarías un MessageBox
            return true; // Por defecto confirmar
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Configura los eventos de arrastrar y soltar
    /// </summary>
    private void SetupDragAndDrop()
    {
        // Habilitar drag and drop
        AllowDrop = true;

        // Configurar eventos
        DragOver += MainWindow_DragOver;
        Drop += MainWindow_Drop;
    }

    /// <summary>
    /// Maneja el evento de arrastrar archivos sobre la ventana
    /// </summary>
    private void MainWindow_DragOver(object? sender, DragEventArgs e)
    {
        // Verificar si se están arrastrando archivos
        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    /// <summary>
    /// Maneja la acción de soltar archivos
    /// </summary>
    private void MainWindow_Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null && DataContext is MainWindowViewModel viewModel)
            {
                // Aquí podrías implementar la importación de archivos
                foreach (var file in files)
                {
                    viewModel.StatusMessage = $"Archivo detectado: {file.Name}";
                    // Implementar lógica de importación según el tipo de archivo
                }
            }
        }
    }
}