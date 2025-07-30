using System;
using System.Collections.Generic;
using System.Linq;
using PasswordManager.Models;
using PasswordManager.Services;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;

namespace PasswordManager.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<PasswordEntry> Entries { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchText, value);
            ApplyFilter();
        }
    }

    private string _searchText = string.Empty;
    private readonly DataService _dataService;
    private readonly string _masterPassword;
    private List<PasswordEntry> _allEntries = new();

    // Comandos
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> AddEntryCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteEntryCommand { get; }
    public ReactiveCommand<Unit, Unit> EditEntryCommand { get; }
    public ReactiveCommand<Unit, Unit> GeneratePasswordCommand { get; }

    private PasswordEntry? _selectedEntry;
    public PasswordEntry? SelectedEntry
    {
        get => _selectedEntry;
        set => this.RaiseAndSetIfChanged(ref _selectedEntry, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public MainWindowViewModel(DataService dataService, string masterPassword)
    {
        _dataService = dataService;
        _masterPassword = masterPassword;

        // Inicializar comandos
        SaveCommand = ReactiveCommand.Create(SaveData);
        AddEntryCommand = ReactiveCommand.Create(AddNewEntry);
        DeleteEntryCommand = ReactiveCommand.Create(DeleteSelectedEntry,
            this.WhenAnyValue(x => x.SelectedEntry).Select(x => x != null));
        EditEntryCommand = ReactiveCommand.Create(EditSelectedEntry,
            this.WhenAnyValue(x => x.SelectedEntry).Select(x => x != null));
        GeneratePasswordCommand = ReactiveCommand.Create(GeneratePassword);

        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var loadedEntries = _dataService.LoadData(_masterPassword);
            if (loadedEntries != null)
            {
                _allEntries = loadedEntries;
                ApplyFilter();
                StatusMessage = $"Cargadas {_allEntries.Count} entradas";
            }
            else
            {
                StatusMessage = "Error: Contraseña maestra incorrecta";
                _allEntries = new List<PasswordEntry>();
                ApplyFilter();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cargar datos: {ex.Message}";
            _allEntries = new List<PasswordEntry>();
        }
    }

    private void ApplyFilter()
    {
        Entries.Clear();
        var filteredEntries = string.IsNullOrWhiteSpace(SearchText)
            ? _allEntries
            : _allEntries.Where(e =>
                e.Website.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                e.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                e.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                e.Url.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var entry in filteredEntries)
        {
            Entries.Add(entry);
        }
    }

    private void SaveData()
    {
        try
        {
            _dataService.SaveData(_allEntries, _masterPassword);
            StatusMessage = "Datos guardados correctamente";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al guardar: {ex.Message}";
        }
    }

    private void AddNewEntry()
    {
        var newEntry = new PasswordEntry
        {
            Website = "Nuevo sitio web",
            Url = "https://",
            Username = "",
            Email = "",
            Password = "",
            Notes = ""
        };

        _allEntries.Add(newEntry);
        ApplyFilter();
        SelectedEntry = newEntry;
        StatusMessage = "Nueva entrada agregada";
    }

    private void DeleteSelectedEntry()
    {
        if (SelectedEntry != null)
        {
            var entryToRemove = SelectedEntry;
            _allEntries.Remove(entryToRemove);
            ApplyFilter();
            SelectedEntry = null;
            SaveData();
            StatusMessage = $"Entrada '{entryToRemove.Website}' eliminada";
        }
    }

    private void EditSelectedEntry()
    {
        if (SelectedEntry != null)
        {
            // Cuando se modifica una entrada, automáticamente se refleja en la UI
            // gracias al binding bidireccional
            StatusMessage = $"Editando entrada: {SelectedEntry.Website}";
        }
    }

    private void GeneratePassword()
    {
        if (SelectedEntry != null)
        {
            SelectedEntry.Password = GenerateSecurePassword();
            StatusMessage = "Contraseña generada automáticamente";
        }
    }

    private string GenerateSecurePassword(int length = 16)
    {
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string numbers = "0123456789";
        const string symbols = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var allChars = upperCase + lowerCase + numbers + symbols;
        var random = new Random();

        // Asegurar que tenga al menos un carácter de cada tipo
        var password = new char[length];
        password[0] = upperCase[random.Next(upperCase.Length)];
        password[1] = lowerCase[random.Next(lowerCase.Length)];
        password[2] = numbers[random.Next(numbers.Length)];
        password[3] = symbols[random.Next(symbols.Length)];

        // Llenar el resto aleatoriamente
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }

        // Mezclar la contraseña
        for (int i = 0; i < length; i++)
        {
            int randomIndex = random.Next(length);
            (password[i], password[randomIndex]) = (password[randomIndex], password[i]);
        }

        return new string(password);
    }

    // Método para actualizar la entrada actual cuando se modifiquen los campos
    public void UpdateCurrentEntry()
    {
        if (SelectedEntry != null)
        {
            // Encontrar la entrada en la lista principal y actualizarla
            var index = _allEntries.FindIndex(e => e.Id == SelectedEntry.Id);
            if (index >= 0)
            {
                _allEntries[index] = SelectedEntry;
            }

            ApplyFilter();
        }
    }
}