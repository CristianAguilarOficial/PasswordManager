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

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    private PasswordEntry? _selectedEntry;
    public PasswordEntry? SelectedEntry
    {
        get => _selectedEntry;
        set => this.RaiseAndSetIfChanged(ref _selectedEntry, value);
    }

    public MainWindowViewModel(DataService dataService, string masterPassword)
    {
        _dataService = dataService;
        _masterPassword = masterPassword;

        LoadData();
        SaveCommand = ReactiveCommand.Create(SaveData);
    }

    private void LoadData()
    {
        _allEntries = _dataService.LoadData(_masterPassword);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Entries.Clear();
        foreach (var entry in _allEntries.Where(e =>
            e.Website.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            e.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            e.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
        {
            Entries.Add(entry);
        }
    }

    private void SaveData()
    {
        _dataService.SaveData(_allEntries, _masterPassword);
    }
}
