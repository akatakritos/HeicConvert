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
    private async void Convert_Click(object? sender, RoutedEventArgs e)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions() { Title = "Source Folder" });
        var source = result[0].Path.AbsolutePath;
        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        StopButton.IsVisible = true;
        ConvertButton.IsVisible = false;
        Progress.IsVisible = true;
        ProgressText.IsVisible = true;
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

    private void Stop_Click(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }
}