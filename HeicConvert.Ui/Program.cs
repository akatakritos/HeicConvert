using Avalonia;
using System;
using Akka.Actor;
using HeicConvert.Core;

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

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}