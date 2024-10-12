using Avalonia;
using System;
using System.Reflection;
using Akka.Actor;
using Avalonia.ReactiveUI;
using HeicConvert.Core;
using ReactiveUI;
using Splat;

namespace HeicConvert.Ui;

class Program
{
    public static ActorSystem ActorSystem { get; } = ActorSystem.Create("HeicConvert");

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        LibHeifSharpDllImportResolver.Register();
        // Splat uses assembly scanning here to register all views and view models.
        Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetCallingAssembly());


        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}