using LiveSetBuilder.App.Pages;

namespace LiveSetBuilder.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
}
