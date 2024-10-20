using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Routing;

namespace HeicConvert.Core.Actors;

public record StartConverting(string SourceDirectory);

public record Pause();

public record Resume();

public record StopConverting();

public record ImageFound(string Path);

public class ConvertProcessManager : ReceiveActor
{
    public static Props Props(IActorRef reporter) => Akka.Actor.Props.Create(() => new ConvertProcessManager(reporter));

    private readonly IActorRef _progressReporter;
    private readonly IActorRef _router;
    private int _pending = 0;

    public ConvertProcessManager(IActorRef progressReporter)
    {
        _progressReporter = progressReporter;
        _router = Context.ActorOf(ConvertRouterActor.Props(), "router");
        Become(Idle);
    }

    private void Idle()
    {
        Receive<StartConverting>(OnStartConverting);
    }

    private Task? _scannerTask;
    private CancellationTokenSource? _cts;

    private void OnStartConverting(StartConverting obj)
    {
        Become(Converting);

        _cts = new CancellationTokenSource();
        
        var self = Self; // https://getakka.net/articles/debugging/rules/AK1005.html
        _scannerTask = Task.Run(() => RecurseDirectory(self, obj.SourceDirectory, _cts.Token));
        return;

        // Static to avoid capturing actor reference or any actor state
        static void RecurseDirectory(IActorRef manager, string current, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var heicFiles = Directory.GetFiles(current, "*.heic");

            foreach (var heicFile in heicFiles)
            {
                manager.Tell(new ImageFound(heicFile));
            }

            if (cancellationToken.IsCancellationRequested) return;
            foreach (var directory in Directory.GetDirectories(current))
            {
                RecurseDirectory(manager, directory, cancellationToken);
            }
        }
    }

    private void Converting()
    {
        Receive<StopConverting>(OnStopConverting);
        Receive<ImageConverted>(OnImageConverted);
        Receive<ImageFound>(OnImageFound);
    }

    private void OnImageFound(ImageFound obj)
    {
        _pending++;
        
        var jpgFile = Path.ChangeExtension(obj.Path, "jpg");
        _router.Tell(new ConvertImageCmd(obj.Path, jpgFile));
        _progressReporter.Tell(obj);
    }

    private void OnStopConverting(StopConverting obj)
    {
        _cts?.Cancel();
        Context.Stop(Self);
    }

    private void OnImageConverted(ImageConverted obj)
    {
        _progressReporter.Tell(obj);
        _pending--;

        if (_scannerTask?.IsCompleted is true && _pending <= 0)
        {
            _cts?.Cancel();
            Context.Stop(Self);
        }
    }
}