using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;
using Dotnet.Test.Utilities;

namespace Dotnet.Test.Tray.ViewModels;

public class FabricViewModel : ObservableObject, IDisposable
{
    private IFabricStatusProvider _statusProvider;

    private bool _fabricRunning;
    public bool FabricRunning
    {

        get => _fabricRunning;

        set
        {
            SetProperty(ref _fabricRunning, value);
            OnPropertyChanged(nameof(FabricStopped));
        }
    }

    private bool _navigationBarRunning;
    public bool NavigationBarRunning { get => _navigationBarRunning; set => SetProperty(ref _navigationBarRunning, value); }

    public bool FabricStopped => !FabricRunning;

    public FabricViewModel(IFabricStatusProvider statusProvider)
    {
        _statusProvider = statusProvider;
        _statusProvider.FabricIsRunningChanged += StateChangeAsync;
    }

    public async Task StartFabricAsync()
    {
        try
        {
            await _statusProvider.StartProfileAsync();
        }
        catch (Exception ex)
        {
            Log.Write(ex, "Failed to start profile.");

            throw;
        }
    }

    public async Task StopFabricAsync()
    {
        try
        {
            await _statusProvider.StopProfileAsync();
        }
        catch (Exception ex)
        {
            Log.Write(ex, "Error on stop");
        }
    }

    public void Dispose()
    {
        Log.Write("FabricViewModel: Dispose.");
        _statusProvider.FabricIsRunningChanged -= StateChangeAsync;
    }

    private async void StateChangeAsync(object? sender, EventArgs args)
    {
        var isRunning = await _statusProvider.IsRunningAsync();

        FabricRunning = isRunning;
        NavigationBarRunning = isRunning;
    }
}
