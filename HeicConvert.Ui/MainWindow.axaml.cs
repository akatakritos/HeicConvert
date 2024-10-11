using Akka.Actor;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HeicConvert.Core.Actors;

namespace HeicConvert.Ui;

public partial class MainWindow : Window, IProgressUi
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void PickFolder_Click(object? sender, RoutedEventArgs e)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions() { Title = "Source Folder" });
        SourceText.Text = result[0].Path.AbsolutePath;
    }

    private void Convert_Click(object? sender, RoutedEventArgs e)
    {
        var source = SourceText.Text;
        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        var progress = Program.ActorSystem.ActorOf(UiProgressActor.Props(this));
        var manager = Program.ActorSystem.ActorOf(ConvertProcessManager.Props(progress));
        manager.Tell(new StartConverting(source), Nobody.Instance);
    }

    public void UpdateProgress(int finished, int total)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Progress.Maximum = total;
            Progress.Value = finished;
            ProgressText.Text = $"{finished} / {total}";
        });
    }
}