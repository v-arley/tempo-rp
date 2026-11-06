
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using TmIde.ViewModels;

using Xunit;

namespace TmIde.Tests.ViewModels;

public sealed class ProjectActivityStatusTests
{
    [Fact]
    public void InitialState_IsNoProject()
    {
        var messenger = new WeakReferenceMessenger();
        var status = CreateStatusBar(messenger);

        Assert.Equal(
            ProjectActivityState.NoProject,
            status.ProjectState);

        Assert.Equal(
            "○ SIN PROYECTO",
            status.ProjectStatusText);
    }

    [Theory]
    [InlineData(
        ProjectActivityState.Ready,
        "● LISTO")]
    [InlineData(
        ProjectActivityState.Compiling,
        "◉ COMPILANDO...")]
    [InlineData(
        ProjectActivityState.Compiled,
        "COMPILADO")]
    [InlineData(
        ProjectActivityState.WithErrors,
        "● CON ERRORES")]
    [InlineData(
        ProjectActivityState.Running,
        "▶ EJECUTANDO...")]
    public void ProjectStateMessage_UpdatesStatus(
        ProjectActivityState state,
        string expectedText)
    {
        var messenger = new WeakReferenceMessenger();
        var status = CreateStatusBar(messenger);

        messenger.Send(
            new ProjectActivityChangedMessage(state));

        Assert.Equal(state, status.ProjectState);
        Assert.Equal(expectedText, status.ProjectStatusText);
    }

    [Fact]
    public void StateChange_NotifiesStatusText()
    {
        var messenger = new WeakReferenceMessenger();
        var status = CreateStatusBar(messenger);

        var notified = false;

        status.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName ==
                nameof(StatusBarViewModel.ProjectStatusText))
            {
                notified = true;
            }
        };

        messenger.Send(
            new ProjectActivityChangedMessage(
                ProjectActivityState.Compiling));

        Assert.True(notified);
    }

    [Fact]
    public void MultipleChanges_KeepLatestState()
    {
        var messenger = new WeakReferenceMessenger();
        var status = CreateStatusBar(messenger);

        messenger.Send(
            new ProjectActivityChangedMessage(
                ProjectActivityState.Ready));

        messenger.Send(
            new ProjectActivityChangedMessage(
                ProjectActivityState.Compiling));

        messenger.Send(
            new ProjectActivityChangedMessage(
                ProjectActivityState.Compiled));

        Assert.Equal(
            ProjectActivityState.Compiled,
            status.ProjectState);
    }

    private static StatusBarViewModel CreateStatusBar(
        IMessenger messenger)
    {
        var command = new RelayCommand(() => { });

        var commands = new ProjectCommands(
            command,
            command,
            command,
            command,
            command);

        return new StatusBarViewModel(
            commands,
            messenger);
    }
}
