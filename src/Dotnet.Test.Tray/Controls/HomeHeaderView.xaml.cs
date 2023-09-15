namespace Dotnet.Test.Tray.Controls;

public partial class HomeHeaderView : ContentView
{
    public event EventHandler? StopClicked;

    public static readonly BindableProperty RunningProperty = BindableProperty.Create(nameof(Running), typeof(bool), typeof(HomeHeaderView), false);

    public bool Running
    {
        get => (bool)GetValue(RunningProperty);
        set => SetValue(RunningProperty, value);
    }

    public bool Stopped
    {
        get => !(bool)GetValue(RunningProperty);
    }

    public HomeHeaderView()
    {
        InitializeComponent();

    }

    void OnStopButtonClicked(object sender, EventArgs args)
    {
        StopClicked?.Invoke(sender, args);
    }
}
