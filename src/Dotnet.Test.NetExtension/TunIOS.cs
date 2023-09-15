using Dotnet.Test.Utilities;
using Enclave.FastPacket;
using Microsoft.Extensions.ObjectPool;
using NetworkExtension;

namespace Dotnet.Test.NetExtension;

// this class wraps the network extension packet tunnel
// in the interface that Fabric expects.
internal sealed class TunIOS : IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;
    private NEPacketTunnelFlow _packetFlow;

    public TunIOS(NEPacketTunnelFlow packetFlow)
    {
        _packetFlow = packetFlow;

        var poolProvider = new DefaultObjectPoolProvider();
        poolProvider.MaximumRetained = 30;
    }

    public void Bind()
    {
        // initalise CancellationTokenSource
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            Task.Factory.StartNew(DeviceRead, _cancellationTokenSource.Token, TaskCreationOptions.None, TaskScheduler.Default);
        }
        catch (Exception)
        {
            if (_cancellationTokenSource.IsCancellationRequested == false)
            {
                _cancellationTokenSource.Cancel();
            }

            throw;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_cancellationTokenSource?.IsCancellationRequested == false)
            {
                _cancellationTokenSource.Cancel();
            }

            _cancellationTokenSource?.Dispose();
        }
    }

    private void DeviceRead()
    {
        if (_cancellationTokenSource is null)
        {
            // Don't throw; nothing to catch it.
            Log.Write("Bad state: no cancellation source available when device read starts.");
            return;
        }

        var cancelToken = _cancellationTokenSource.Token;
        var manualResetEvent = new ManualResetEvent(false);
        var handleCombo = new WaitHandle[] { cancelToken.WaitHandle, manualResetEvent };

        try
        {
            while (!cancelToken.IsCancellationRequested)
            {
                manualResetEvent.Reset();

                using (new NSAutoreleasePool())
                {
                    _packetFlow.ReadPackets((packets, protocols) =>
                    {
                        // call dataReceivedAction for each packet
                        for (int idx = 0; idx < packets.Length; idx++)
                        {
                            var packet = packets[idx].ToArray();

                            var ip = new IpPacket(packet);

                            if (ip.IsIpv4)
                            {
                                Log.Write(ip.Ipv4Span.ToString());
                            }

                            packets[idx].Dispose();
                            protocols[idx].Dispose();
                        }

                        manualResetEvent.Set();
                    });
                }

                WaitHandle.WaitAny(handleCombo);
            }
        }
        catch (ObjectDisposedException)
        {
            // Stopping.
        }
    }
}
