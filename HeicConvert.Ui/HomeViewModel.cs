using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace HeicConvert.Ui;

public class HomeViewModel: ReactiveObject, IRoutableViewModel
{
    private readonly IStorageProvider _storageProvider;
    public string? UrlPathSegment { get; } = "home";
    public IScreen HostScreen { get; }
    public ReactiveCommand<Unit, Unit> StartCommand { get; }

    public HomeViewModel(IScreen hostScreen, IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
        HostScreen = hostScreen;
        StartCommand = ReactiveCommand.CreateFromTask(SelectFolder);
    }

    private async Task SelectFolder()
    {
        var result = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions() { Title = "Select source folder" });
        var folder = result.FirstOrDefault();
        if (folder == null) return;
        var path = folder.Path.AbsolutePath;
        HostScreen.Router.Navigate.Execute(new ConvertViewModel(HostScreen, path));
    }

}