using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Routing;

namespace HeicConvert.Core.Actors;

public record StartConverting(string SourceDirectory);
public record Pause();
public record Resume();
public record StopConverting();

public record ImageFound(string Path);

public class ConvertProcessManager: ReceiveActor
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

    private CancellationTokenSource? _cts;
    private void OnStartConverting(StartConverting obj)
    {
        Become(Converting);
        
        _cts = new CancellationTokenSource();
        Task.Run(() => RecurseDirectory(obj.SourceDirectory, _cts.Token));
        return;

        void RecurseDirectory(string current, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var heicFiles = Directory.GetFiles(current, "*.heic");
            
            foreach (var heicFile in heicFiles)
            {
                var jpgFile = Path.ChangeExtension(heicFile, "jpg");
                _router.Tell(new ConvertImageCmd(heicFile, jpgFile));
                _pending++;
                _progressReporter.Tell(new ImageFound(heicFile));
            }
            
            if (cancellationToken.IsCancellationRequested) return;
            foreach (var directory in Directory.GetDirectories(current))
            {
                RecurseDirectory(directory, cancellationToken);
            }
        }
    }

    private void Converting()
    {
        Receive<StopConverting>(OnStopConverting);
        Receive<ImageConverted>(OnImageConverted);
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
        
        if (_pending <= 0)
        {
            _cts?.Cancel();
            Context.Stop(Self);
        }
    }
}