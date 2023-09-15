using System.Diagnostics.CodeAnalysis;
using Dotnet.Test.Utilities;
using NetworkExtension;

namespace Dotnet.Test.NetExtension;

[Register("PacketTunnelProvider")]
[Preserve(AllMembers = true)]
public class PacketTunnelProvider : NEPacketTunnelProvider
{
    private readonly ManualResetEventSlim _stopCompleteReset = new ManualResetEventSlim();

    private TaskCompletionSource? _stopSource;
    private Action<NSError?>? _completionHandler;
    private Task? _backgroundTask;

    public override void StartTunnel(NSDictionary<NSString, NSObject>? options, Action<NSError> completionHandler)
    {
        _stopSource = new TaskCompletionSource();
        _stopCompleteReset.Reset();

        _completionHandler = completionHandler!;
        _backgroundTask = Task.Run(() => Block(_stopSource.Task));
    }

    public override void StopTunnel(NEProviderStopReason reason, Action completionHandler)
    {
        ArgumentNullException.ThrowIfNull(completionHandler);

        _stopSource?.TrySetCanceled();

        // Give it a moment to stop.
        _stopCompleteReset.Wait(TimeSpan.FromSeconds(3));

        completionHandler();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stopCompleteReset.Dispose();
        }

        base.Dispose(disposing);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "NSError is owned by completion handler")]
    private async Task Go(Task stopTask)
    {
        try
        {
            await RunProfileAsync(stopTask);
        }
        catch (Exception ex)
        {
            Log.Write(ex, "Failed to run profile");

            if (_completionHandler is null)
            {
                Log.Write("_completionHandler null in Go, unexpected");
                return;
            }

            _completionHandler(new NSError(new NSString("EnclaveFabric"), 500));
        }
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "NSError is owned by completion handler")]
    private void Block(Task stopTask)
    {
         Go(stopTask).GetAwaiter().GetResult();
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Network settings owned by platform")]
    private async Task SetupPacketTunnelNetworkSettingsAsync()
    {
        var tunnelNetworkSettings = new NEPacketTunnelNetworkSettings(address: "127.0.0.1");

        tunnelNetworkSettings.IPv4Settings = new NEIPv4Settings(addresses: new string[] { "100.64.0.5" }, subnetMasks: new string[] { "100.64.0.0/10" });

        tunnelNetworkSettings.IPv4Settings.IncludedRoutes = new[] { new NEIPv4Route("100.64.0.0", "255.192.0.0") };

        // Configure the DNS Servers for this tunnel.
        var servers = new[]
        {
            // Just use google DNS.
            "8.8.8.8"
        };

        var dnsSettings = new NEDnsSettings(servers);

        dnsSettings.MatchDomains = new[] { string.Empty };

        tunnelNetworkSettings.DnsSettings = dnsSettings;
        tunnelNetworkSettings.Mtu = 1280;

        Log.Write(
            "Configuring network settings with vip: {0}; vmask: {1}; routes: {2}; nameserver: {3}; mtu: {4}",
            tunnelNetworkSettings.IPv4Settings.Addresses[0],
            tunnelNetworkSettings.IPv4Settings.SubnetMasks[0],
            string.Join(", ", tunnelNetworkSettings.IPv4Settings.IncludedRoutes.Select(x => $"{x.DestinationAddress} ({x.DestinationSubnetMask})")),
            tunnelNetworkSettings.DnsSettings.Servers[0],
            tunnelNetworkSettings.Mtu);

        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

#pragma warning disable CA1849 // Call async methods when in an async method (cannot do that here, the iOS async method variant crashes the process).
        SetTunnelNetworkSettings(tunnelNetworkSettings, err =>
        {
            Log.Write("SetTunnelNetworkSettings: completion callback fired");
            if (err != null)
            {
                completionSource.TrySetException(new NSErrorException(err));
            }
            else
            {
                completionSource.TrySetResult();
            }
        });
#pragma warning restore CA1849

        await completionSource.Task;

        Log.Write("Completed network settings config");
    }

    private async Task RunProfileAsync(Task stopTask)
    {
        using (var adapter = new TunIOS(PacketFlow))
        {
            await SetupPacketTunnelNetworkSettingsAsync();

            adapter.Bind();

            if (_completionHandler is null)
            {
                Log.Write("_completionHandler null in RunProfileAsync, unexpected");
                return;
            }

            // Say that we're finished setting up.
            _completionHandler(null);

            try
            {
                await stopTask;
            }
            catch (System.OperationCanceledException)
            {
                // This is expected. The task we wait on for the fabric stopping
                // will be marked as cancelled when the VpnService OnDestroy is called.
                // When the task we are awaiting on
                // throws OperationCanceledException, it's just because we are
                // normally shutting down.
            }
        }
    }
}