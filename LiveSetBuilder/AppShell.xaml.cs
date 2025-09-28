using LiveSetBuilder.App.Pages;

namespace LiveSetBuilder.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(SetEditorPage), typeof(SetEditorPage));
        Routing.RegisterRoute(nameof(RunnerPage), typeof(RunnerPage));
    }
}
