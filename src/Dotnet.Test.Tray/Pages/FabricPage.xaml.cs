using Dotnet.Test.Tray.ViewModels;

namespace Dotnet.Test.Tray.Pages;

public partial class FabricPage : ContentPage, IDisposable
{
    public FabricViewModel FabricViewModel { get; private set; }

    public FabricPage(FabricViewModel fabricViewModel)
    {
        FabricViewModel = fabricViewModel;

        InitializeComponent();

        BindingContext = FabricViewModel;
    }

    public void Dispose()
    {
        FabricViewModel.Dispose();
    }

    private async void StartNetworkButton_Clicked(object sender, System.EventArgs e)
    {
        try
        {
            await FabricViewModel.StartFabricAsync();
        }
        catch(Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Could not start fabric", $"Could not start the fabric.\r\n\r\n{ex.Message}", "OK.");
        }
    }

    private async void StopNetworkButton_Clicked(object sender, System.EventArgs e)
    {
        await FabricViewModel.StopFabricAsync();
    }
}
