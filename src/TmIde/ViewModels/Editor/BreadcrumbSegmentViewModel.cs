using TmIde.ViewModels;

namespace TmIde.ViewModels.Editor;

/// <summary>Segmento de la barra de ruta (breadcrumb) sobre el editor.</summary>

public sealed class BreadcrumbSegmentViewModel : ViewModelBase
{
    public BreadcrumbSegmentViewModel(string text, bool isLast)
    {
        Text = text;
        IsLast = isLast;
    }

    public string Text { get; }

    public bool IsLast { get; }
}
