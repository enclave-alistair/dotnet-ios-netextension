using CoreFoundation;
using Foundation;
using NetworkExtension;
using Plugin.LocalNotification;
using System;
using System.Threading.Tasks;
using System.Threading;
using Dotnet.Test.Utilities;

namespace Dotnet.Test.Tray.Platforms.iOS;

public class IOSFabricStatusProvider : IFabricStatusProvider
{
    // TODO only have one VPN Manager instance works right now because we
    // are only supporting the default "Universe" profile at this time
    // If multiple profiles are to be run at the same time we will need
    // to keep multiple around and modify the accessors (IsConnected, IsConfigured, etc...)
    // to take a profile name parameter.
    private Lazy<Task<NETunnelProviderManager?>> _vpnManager;

    public event EventHandler? FabricIsRunningChanged;

    public IOSFabricStatusProvider()
    {
        NSNotificationCenter.DefaultCenter.AddObserver(NEVpnConnection.StatusDidChangeNotification, StatusDidChange);

        _vpnManager = new Lazy<Task<NETunnelProviderManager?>>(async () =>
        {
            return await GetTunnelProvider().ConfigureAwait(false);
        });
    }

    public async ValueTask<bool> IsRunningAsync()
    {
        var sessionStatus = await GetSessionStatusAsync().ConfigureAwait(false);

        return sessionStatus == NEVpnStatus.Connected;
    }

    public async ValueTask StartProfileAsync()
    {
        var existingManager = await GetTunnelProvider();

        if (existingManager is null)
        {
            NETunnelProviderProtocol providerProtocol = new NETunnelProviderProtocol();
            providerProtocol.ProviderBundleIdentifier = "com.dotnet.test.extension";

            providerProtocol.ServerAddress = $"Test Extension";

            var manager = new NETunnelProviderManager
            {
                ProtocolConfiguration = providerProtocol,
                LocalizedDescription = "Dotnet Test", // This value is used to identify the correct VPN profile for this profile
                Enabled = true,
            };

            await manager.SaveToPreferencesAsync().ConfigureAwait(false);

            manager.Dispose();

            // Reload the manager after saving the provider. The old one will
            // be invalid and unable to immediately connect.
            _vpnManager = new Lazy<Task<NETunnelProviderManager?>>(async () =>
            {
                var manager = await GetTunnelProvider();

                if (manager is not null)
                {
                    manager.Enabled = true;
                }

                return manager;
            });
        }

        var startCompletionSource = new TaskCompletionSource();

        DispatchQueue.DefaultGlobalQueue.DispatchAsync(async () =>
        {
            NSError? err = null;
            var session = await GetSessionAsync().ConfigureAwait(false);

            if (session is null)
            {
                startCompletionSource.TrySetException(new InvalidOperationException("Not installed"));
                return;
            }

            Log.Write("Starting tunnel");
            bool? ok = session.StartVpnTunnel(out err);
            if (ok != true)
            {
                Log.Write("Connect error: {0}", err);

                startCompletionSource.TrySetException(new InvalidOperationException($"Initial setup error: {err}"));
                return;
            }

            startCompletionSource.TrySetResult();
        });

        await startCompletionSource.Task.ConfigureAwait(false);
    }

    public async ValueTask StopProfileAsync()
    {
        var stopCompletionSource = new TaskCompletionSource();

        DispatchQueue.DefaultGlobalQueue.DispatchAsync(async () =>
        {
            var session = await GetSessionAsync();

            session?.StopVpnTunnel();

            stopCompletionSource.TrySetResult();
        });

        await stopCompletionSource.Task.ConfigureAwait(false);
    }

    public async ValueTask RestartProfileAsync()
    {
        await StopProfileAsync().ConfigureAwait(false);

        await StartProfileAsync().ConfigureAwait(false);
    }

    private async Task<NETunnelProviderSession?> GetSessionAsync()
    {
        var vpnManager = await _vpnManager.Value.ConfigureAwait(false);

        return (NETunnelProviderSession?)vpnManager?.Connection;
    }

    private async Task<NEVpnStatus> GetSessionStatusAsync()
    {
        var vpnManager = await _vpnManager.Value.ConfigureAwait(false);

        if (vpnManager is null || !vpnManager.Enabled)
        {
            return NEVpnStatus.Invalid;
        }

        var session = (NETunnelProviderSession?)vpnManager.Connection;

        return session?.Status ?? NEVpnStatus.Disconnected;
    }

    private void StatusDidChange(NSNotification notification)
    {
        // Clear the cached client so we create a new one.
        var session = notification.Object as NETunnelProviderSession;
        var status = session?.Status.ToString();

        Log.Write($"The status of the VPN has changed: {status}");

        _ = Task.Run(() =>
        {
            FabricIsRunningChanged?.Invoke(this, new());
        });
    }

    private static async Task<NETunnelProviderManager?> GetTunnelProvider()
    {
        var managers = await NETunnelProviderManager.LoadAllFromPreferencesAsync().ConfigureAwait(false);

        for (nuint i = 0; i < managers.Count; i++)
        {
            var manager = managers.GetItem<NETunnelProviderManager>(i);
            if (manager.LocalizedDescription == "Dotnet Test")
            {
                Log.Write("Manager Found");
                return manager;
            }
        }

        return null;
    }
}
