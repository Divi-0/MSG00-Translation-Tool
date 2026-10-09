using System.Runtime.CompilerServices;
using System.Text;

namespace MSG00.Translation.Application.Tests;

internal static class TestSetup
{
    /// <summary>
    /// The readers and writers use Shift-JIS; in the app this is registered by AddApplicationServices.
    /// </summary>
    [ModuleInitializer]
    internal static void RegisterCodePages() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
}