using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace HeicConvert.Ui;

public partial class ConvertView : ReactiveUserControl<ConvertViewModel>
{
    public ConvertView()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}