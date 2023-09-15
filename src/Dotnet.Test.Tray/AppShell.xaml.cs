using System;
using System.ComponentModel;
using Dotnet.Test.Utilities;
using Microsoft.Maui.Controls;

namespace Dotnet.Test.Tray;

public partial class AppShell : Shell, INotifyPropertyChanged
{
    private readonly IFabricStatusProvider _statusProvider;


    public AppShell(IFabricStatusProvider statusProvider)
    {
        _statusProvider = statusProvider;

        InitializeComponent();

        BindingContext = this;
    }

    private void StatusProvider_StateChange(object? sender, EventArgs e)
    {
        Log.Write("Shell: StatusProvider_StateChange");
    }

    private async void Shell_Loaded(object sender, System.EventArgs e)
    {
        Log.Write("Shell_Loaded");

        _statusProvider.FabricIsRunningChanged += StatusProvider_StateChange;

        await Shell.Current.GoToAsync("//FabricPage");
    }
}