using CommunityToolkit.Mvvm.ComponentModel;

namespace TmIde.ViewModels;

/// <summary>
/// Base común de todos los ViewModels. Hereda de <see cref="ObservableObject"/>
/// (CommunityToolkit.Mvvm) para disponer de <c>SetProperty</c> /
/// <c>OnPropertyChanged</c> y habilitar los generadores de código
/// <c>[ObservableProperty]</c> / <c>[RelayCommand]</c>.
/// </summary>

public abstract class ViewModelBase : ObservableObject
{
}
