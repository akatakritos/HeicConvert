using System.Reactive.Concurrency;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace HeicConvert.Ui;

public class MainWindowViewModel: ReactiveObject, IScreen
{
    public RoutingState Router { get; } = new(new EventLoopScheduler());

    public MainWindowViewModel(IStorageProvider storageProvider)
    {
        Router.Navigate.Execute(new HomeViewModel(this, storageProvider));
    }
}