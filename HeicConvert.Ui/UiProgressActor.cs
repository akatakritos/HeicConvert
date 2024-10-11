using Akka.Actor;
using HeicConvert.Core.Actors;

namespace HeicConvert.Ui;

public interface IProgressUi
{
    void UpdateProgress(int finished, int total);
}

public class UiProgressActor: ReceiveActor
{
    public static Props Props(IProgressUi screen) => Akka.Actor.Props.Create(() => new UiProgressActor(screen));
    
    private readonly IProgressUi _screen;
    private int _images = 0;
    private int _complated = 0;

    public UiProgressActor(IProgressUi screen)
    {
        _screen = screen;
        Receive<ImageFound>(cmd =>
        {
            _images++;
            _screen.UpdateProgress(_complated, _images);
        });

        Receive<ImageConverted>(cmd =>
        {
            _complated++;
            _screen.UpdateProgress(_complated, _images);
        });
    }
    
}