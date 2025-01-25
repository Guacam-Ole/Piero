using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Piero.Models;

public partial class Captions : ObservableObject
{
    private const string IdleTitle = "🥑 Piero: DaVinci Resolve Autoconversion & Proxy Generator 🥑";
    [ObservableProperty] private string _title;

    public void SetIdle()
    {
        _title = IdleTitle;
    }

    public Captions()
    {
        SetIdle();
    }
}