using Akka.Actor;
using HeicConvert.Core.Actors;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace HeicConvert.Cli;

public class ProgressReporterActor : ReceiveActor
{
    public static Props Props() => Akka.Actor.Props.Create<ProgressReporterActor>();

    private int _imageCount = 0;
    private int _progressCount = 0;
    private TaskCompletionSource _stopped = new();

    public ProgressReporterActor()
    {
        ProgressTask? progressTask = null;
        ProgressContext? context = null;
        AnsiConsole.Progress()
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(), // Task description
                new ProgressBarColumn(), // Progress bar
                //new PercentageColumn(), // Percentage
                new CountColumn(),
                new RemainingTimeColumn(), // Remaining time
                new SpinnerColumn(), // Spinner
            })
            .StartAsync(async ctx =>
            {
                context = ctx;
                progressTask = ctx.AddTask("Converting");

                await _stopped.Task;
            });

        Receive<ImageFound>(img =>
        {
            _imageCount++;
            if (progressTask != null)
                progressTask.MaxValue = _imageCount;
        });

        Receive<ImageConverted>(img =>
        {
            _progressCount++;
            if (progressTask != null)
                progressTask.Value = _progressCount;
        });
    }

    protected override void PostStop()
    {
        _stopped.SetResult();
    }
}

class CountColumn: ProgressColumn 
{
    public Style Style { get; set; } = Style.Plain;

    public Style CompletedStyle { get; set; } = Color.Green;
    
    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        var value = (int)task.Value;
        var count = (int)task.MaxValue;
        var style = value >= count ? CompletedStyle : Style;
        return new Text($"{value} / {count}", style).RightJustified();
    }
}