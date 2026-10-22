using TmIde.ViewModels.Panel;

namespace TmIde.Services;

/// <summary><see cref="IOutputLog"/> sobre el panel Output del panel inferior.</summary>

public sealed class OutputLog : IOutputLog
{
    private readonly BottomPanelViewModel _bottomPanel;

    public OutputLog(BottomPanelViewModel bottomPanel)
    {
        _bottomPanel = bottomPanel;
    }

    public void WriteLine(string message) => _bottomPanel.Log(message);
}
