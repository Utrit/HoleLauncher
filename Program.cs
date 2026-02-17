using Avalonia;
using System;
using HoleLauncher.Core.Launcher;
using HoleLauncher.Core.Services;
using ReactiveUI;
using ReactiveUI.Avalonia;
using Splat;

namespace HoleLauncher;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI(factory =>
            {
                Locator.CurrentMutable.RegisterLazySingleton(() => new MessageBus(), typeof(IMessageBus));
                Locator.CurrentMutable.Register(()=> new DataProvider(), typeof(IDataProvider));
                Locator.CurrentMutable.RegisterConstant(new LauncherCore(), typeof(ILauncherCore));
            });
}