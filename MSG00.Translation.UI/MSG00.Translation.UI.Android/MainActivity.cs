using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace MSG00.Translation.UI.Android
{
    [Activity(Label = "MSG00.Translation.UI.Android", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon", LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : AvaloniaMainActivity
    {
    }
}