using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Services;
using PropertyManager.ViewModels;

namespace PropertyManager;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        services.AddSingleton<IVariableHandler, VariableHandler>();
        services.AddSingleton<IArduinoSerialPort, ArduinoSerialPort>();
        services.AddSingleton<IArduinoVariableProtocol, ArduinoVariableProtocol>();

        services.AddTransient<NeoPixelViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
