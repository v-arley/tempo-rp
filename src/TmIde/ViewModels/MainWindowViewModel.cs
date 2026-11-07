using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using TmIde.ViewModels.Editor;
using TmIde.ViewModels.Explorer;
using TmIde.ViewModels.Panel;
using TmIde.ViewModels.Particles;
using TmIde.ViewModels.Shell;
using TmIde.Services.Audio;

namespace TmIde.ViewModels;

/// <summary>
/// ViewModel raíz de la ventana principal: agrega los ViewModels de cada región
/// del shell, que llegan del contenedor de DI, y mantiene el título. Las
/// operaciones del proyecto viven en <see cref="ProjectWorkspaceViewModel"/>.
/// </summary>

public sealed partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "TMIDE";

    public MainWindowViewModel(
        ProjectWorkspaceViewModel workspace,
        MenuBarViewModel menuBar,
        FileExplorerViewModel explorer,
        EditorHostViewModel editorHost,
        BottomPanelViewModel bottomPanel,
        StatusBarViewModel statusBar,
        ParticlePanelViewModel particlePanel,
        IMessenger messenger,
        TypingPianoService? piano = null)
    {
        Workspace = workspace;
        MenuBar = menuBar;
        Explorer = explorer;
        EditorHost = editorHost;
        BottomPanel = bottomPanel;
        StatusBar = statusBar;
        ParticlePanel = particlePanel;
        Piano = piano;

        messenger.Register<MainWindowViewModel, ActiveDocumentChangedMessage>(
            this, static (window, message) => window.UpdateTitle(message.Document));
        UpdateTitle(EditorHost.SelectedDocument);
    }

    public ProjectWorkspaceViewModel Workspace { get; }

    /// <summary>Comandos del proyecto, para los atajos de teclado de la ventana.</summary>
    public ProjectCommands Commands => Workspace.Commands;

    public MenuBarViewModel MenuBar { get; }

    public FileExplorerViewModel Explorer { get; }

    public EditorHostViewModel EditorHost { get; }

    public BottomPanelViewModel BottomPanel { get; }

    public StatusBarViewModel StatusBar { get; }

    public ParticlePanelViewModel ParticlePanel { get; }

    /// Servicio compartido de sonido al escribir.
    public TypingPianoService? Piano { get; }

    private void UpdateTitle(EditorDocumentViewModel? document)
    {
        Title = document is not null
            ? document.FileName.ToUpperInvariant()
            : "TMIDE";
    }
}
