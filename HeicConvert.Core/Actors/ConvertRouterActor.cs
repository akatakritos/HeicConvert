using System.Diagnostics;
using Akka.Actor;

namespace HeicConvert.Core.Actors;

public class ConvertRouterActor: ReceiveActor
{
    record ChildState(IActorRef Child, bool Busy);
        
    public static Props Props() => Akka.Actor.Props.Create<ConvertRouterActor>();
    private readonly ChildState[] _children = new ChildState[Environment.ProcessorCount];
    private readonly Queue<object> _messages = new();

    public ConvertRouterActor()
    {
        Become(Ready);
    }

    private void Ready()
    {
        for (int i = 0; i < _children.Length; i++)
            _children[i] = new ChildState(Context.ActorOf(ConvertActor.Props(), "converter-" + i), false);

        // TODO: pause, stop

        Receive<ImageConverted>(OnImageConverted);
        ReceiveAny(Enqueue);
    }

    private void OnImageConverted(ImageConverted obj)
    {
        var child = Array.FindIndex(_children, child => child.Child.Equals(Context.Sender));
        Debug.Assert(child >= 0);

        _children[child] = _children[child] with { Busy = false };
        SendNext();
        Context.Parent.Forward(obj);
    }

    private void Enqueue(object message)
    {
        _messages.Enqueue(message);
        SendNext();
    }
    
    private void SendNext()
    {
        var child = Array.FindIndex(_children, c => !c.Busy);
        if (child >= 0) SendNext(child);
    }
    private void SendNext(int childIndex)
    {
        if (!_messages.TryDequeue(out var message)) return;
        
        _children[childIndex] = _children[childIndex] with { Busy = true };
        _children[childIndex].Child.Tell(message, Self);
    }
}