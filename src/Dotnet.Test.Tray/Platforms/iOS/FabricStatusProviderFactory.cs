using System.Runtime.Versioning;
using Dotnet.Test.Tray.Platforms.iOS;
using Dotnet.Test.Utilities;

namespace Dotnet.Test.Tray.Services;

[SupportedOSPlatform("ios15.0")]
public partial class FabricStatusProviderFactory
{
    public static partial IFabricStatusProvider CreateStatusProvider()
    {
        return new IOSFabricStatusProvider();
    }
}
