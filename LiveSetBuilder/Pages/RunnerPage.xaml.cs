using LiveSetBuilder.App.ViewModels;

namespace LiveSetBuilder.App.Pages;

public partial class RunnerPage : ContentPage
{
    public RunnerPage(RunnerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
