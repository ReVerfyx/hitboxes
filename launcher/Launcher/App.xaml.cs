using System.Windows;
using Hitboxes.Launcher.Theming;

namespace Hitboxes.Launcher;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Register the shared animatable theme brushes before the
        // StartupUri window is created so its DynamicResource bindings
        // resolve immediately instead of flashing unstyled.
        ThemeResources.Register(Resources);
        base.OnStartup(e);
    }
}
