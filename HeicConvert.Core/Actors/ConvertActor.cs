using System.Diagnostics;
using Akka.Actor;

namespace HeicConvert.Core.Actors;

public record ConvertImageCmd(string Source, string Destination);

public record ImageConverted(string Source, string Destination, int BytesRead, int BytesWritten, TimeSpan Duration);

public class ConvertActor : ReceiveActor
{
    public static Props Props() => Akka.Actor.Props.Create<ConvertActor>();

    public ConvertActor()
    {
        Receive<ConvertImageCmd>(OnConvertImageCmd);
    }

    private void OnConvertImageCmd(ConvertImageCmd obj)
    {
        var sw = Stopwatch.StartNew();
        HeicConverter.ConvertToJpg(obj.Source, obj.Destination);
        sw.Stop();

        Context.Sender.Tell(new ImageConverted(
            Source: obj.Source, 
            Destination: obj.Destination,
            BytesRead: (int)new FileInfo(obj.Source).Length,
            BytesWritten: (int)new FileInfo(obj.Destination).Length, 
            Duration: sw.Elapsed));
    }
}