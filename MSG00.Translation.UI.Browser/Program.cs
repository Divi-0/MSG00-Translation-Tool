using Avalonia;
using Avalonia.Browser;
using Avalonia.ReactiveUI;
using MSG00.Translation.UI;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("browser")]

internal partial class Program
{
    private async static Task Main(string[] args) => await BuildAvaloniaApp()
        .UseReactiveUI()
        .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}