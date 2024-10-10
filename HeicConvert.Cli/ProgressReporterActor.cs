using Akka.Actor;

namespace HeicConvert.Cli;

public class ProgressReporterActor: ReceiveActor
{
    public static Props Props() => Akka.Actor.Props.Create<ProgressReporterActor>();

    public ProgressReporterActor()
    {
        ReceiveAny(msg => Console.WriteLine(msg.ToString()));
    }
}