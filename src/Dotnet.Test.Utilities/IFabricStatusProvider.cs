namespace Dotnet.Test.Utilities;

public interface IFabricStatusProvider
{
    event EventHandler? FabricIsRunningChanged;

    ValueTask<bool> IsRunningAsync();

    ValueTask StartProfileAsync();

    ValueTask StopProfileAsync();

    ValueTask RestartProfileAsync();
}