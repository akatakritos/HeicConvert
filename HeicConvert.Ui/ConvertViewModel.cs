using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Akka.Actor;
using HeicConvert.Core.Actors;
using ReactiveUI;

namespace HeicConvert.Ui;

public class ConvertViewModel: ReactiveObject, IActivatableViewModel, IProgressUi, IRoutableViewModel, IDisposable
{
    private readonly string _path;

    private int _convertedCount = 0;
    public int ConvertedCount
    {
        get => _convertedCount;
        set => this.RaiseAndSetIfChanged(ref _convertedCount, value);
    }
    
    private int _totalCount = 0;
    public int TotalCount
    {
        get => _totalCount;
        set => this.RaiseAndSetIfChanged(ref _totalCount, value);
    }
    
    public IObservable<string> ProgressText => this.WhenAnyValue(x => x.ConvertedCount, x => x.TotalCount, 
        (converted, total) => $"{converted} / {total}");

    private readonly ActorSystem _actorSystem;
    public ConvertViewModel(IScreen screen, string path)
    {
        HostScreen = screen;
        _path = path;
        
       _actorSystem = ActorSystem.Create("converter");
        this.WhenActivated((CompositeDisposable d) =>
        {
            var progressActor = _actorSystem.ActorOf(UiProgressActor.Props(this), "progress");
            var converterActor = _actorSystem.ActorOf(ConvertProcessManager.Props(progressActor), "converter");
            converterActor.Tell(new StartConverting(_path));

            d.Add(Observable.FromAsync(converterActor.WatchAsync, RxApp.MainThreadScheduler)
                .Select(_ => HostScreen.Router.NavigateBack.Execute())
                .Switch()
                .Subscribe());

        });
    }

    public void Dispose()
    {
        _actorSystem.Dispose();
    }


    public ViewModelActivator Activator { get; } = new();
    public void UpdateProgress(int finished, int total)
    {
        ConvertedCount = finished;
        TotalCount = total;
    }

    public string? UrlPathSegment { get; }
    public IScreen HostScreen { get; }
}

public class DesignConvertViewModel: ConvertViewModel
{
    public DesignConvertViewModel(): base(new MainWindowViewModel(null!), "C:\\")
    {
        TotalCount = 10;
        ConvertedCount = 5;
    }
}