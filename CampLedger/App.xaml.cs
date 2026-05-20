namespace CampLedger;

public partial class App : Application
{
    private readonly AppShell _appShell;

	public App()
	{
		InitializeComponent();
        _appShell = new AppShell();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(_appShell);
	}
}