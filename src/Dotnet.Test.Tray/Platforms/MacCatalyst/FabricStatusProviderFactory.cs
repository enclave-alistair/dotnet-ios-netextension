using System.Runtime.Versioning;
using Dotnet.Test.Tray.Platforms.MacCatalyst;
using Dotnet.Test.Utilities;

namespace Dotnet.Test.Tray.Services;

[SupportedOSPlatform("maccatalyst14.0")]
public partial class FabricStatusProviderFactory
{
    public static partial IFabricStatusProvider CreateStatusProvider()
    {
        return new MacCatalystFabricStatusProvider();
    }
}
