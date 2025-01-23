using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Piero.Models;

public sealed class Captions:INotifyPropertyChanged
{
    private const string IdleTitle = "🥑 Piero: DaVinci Resolve Autoconversion & Proxy Generator 🥑";
    private string _title;

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public void SetIdle()
    {
        Title = IdleTitle;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }

    public Captions()
    {
        SetIdle();
    }
}