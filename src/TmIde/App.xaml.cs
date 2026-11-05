using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Serilog;
using Serilog.Events;

using ILogger = Microsoft.Extensions.Logging.ILogger;

using TmIde.Services;
using TmIde.Services.Analysis;
using TmIde.Services.Execution;
using TmIde.Services.Navigation;
using TmIde.Services.Settings;
using TmIde.ViewModels;
using TmIde.ViewModels.Editor;
using TmIde.ViewModels.Explorer;
using TmIde.ViewModels.Panel;
using TmIde.ViewModels.Particles;
using TmIde.ViewModels.Shell;
using TmIde.ViewModels.Start;
using TmIde.Views;
using TmIde.Views.Start;

namespace TmIde;

/// <summary>
/// Punto de entrada de la aplicación y raíz de composición: arma el Generic
/// Host (DI, logging, configuración y ciclo de vida) y registra servicios,
/// ViewModels y ventanas. Al arrancar se muestra solo el diálogo de inicio; la
/// ventana principal se abre cuando en él se crea o se abre un proyecto, y
/// cerrarlo sin elegir ninguno termina la aplicación.
/// </summary>

public partial class App : Application
{
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

    // Un archivo por día (tmide-AAAAMMDD.log); se conservan los últimos 7.
    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TmIde",
        "logs",
        "tmide-.log");

    private IHost? _host;
    private ILogger _logger = NullLogger.Instance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Mientras no exista la ventana principal, cerrar el diálogo de inicio no
        // debe terminar la aplicación por sí solo; FlowService.ShowMain lo cambia.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _host = CreateHost(e.Args);
        _host.Start();

        var services = _host.Services;
        _logger = services.GetRequiredService<ILogger<App>>();
        ObserveUnhandledExceptions();
        LogStarted(Environment.Version);

        services.GetRequiredService<IThemeService>().ApplySaved();

        if (services.GetRequiredService<IStartDialogService>().Show() is not { } projectDirectory)
        {
            Shutdown();
            return;
        }

        services.GetRequiredService<IFlowService>().ShowMain(services.GetRequiredService<MainWindowViewModel>());
        services.GetRequiredService<ProjectWorkspaceViewModel>().OpenProject(projectDirectory);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            using (_host)
            {
                _host.StopAsync(ShutdownTimeout).GetAwaiter().GetResult();
            }
        }

        base.OnExit(e);
    }

    private static IHost CreateHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            // El directorio de trabajo depende de cómo se lance la app (acceso
            // directo, terminal); la configuración se busca junto al ejecutable.
            ContentRootPath = AppContext.BaseDirectory,
        });

        // El log interno va solo a archivo (Serilog).
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog(
            (_, configuration) => configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.File(
                    LogFilePath,
                    formatProvider: CultureInfo.InvariantCulture,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7));

        builder.Services.Configure<IdeOptions>(builder.Configuration.GetSection(IdeOptions.SectionName));

        ConfigureServices(builder.Services);

        // Comprueba al arrancar que todo lo registrado se puede construir: un
        // error de registro aparece aquí y no al abrir la ventana principal.
        builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        }));

        return builder.Build();
    }

    /// <summary>
    /// Servicios y ViewModels del shell son únicos (Singleton); las ventanas se
    /// crean de nuevo en cada apertura (Transient).
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IFlowService>(provider => new FlowService(provider)
            .Register<MainWindowViewModel, MainWindow>()
            .Register<StartViewModel, StartWindow>());

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IMessenger>(new WeakReferenceMessenger());
        services.AddSingleton<IUserSettingsStore>(provider => new JsonUserSettingsStore(
            JsonUserSettingsStore.DefaultFilePath,
            provider.GetRequiredService<ILogger<JsonUserSettingsStore>>()));
        services.AddSingleton<IRecentProjectsService, RecentProjectsService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ILanguageReferenceService, LanguageReferenceService>();
        services.AddSingleton<IConfirmDialogService, ConfirmDialogService>();
        services.AddSingleton<IProjectScaffolder, ProjectScaffolder>();
        services.AddSingleton<IProjectFileService, ProjectFileService>();
        services.AddSingleton<INamePromptService, NamePromptService>();
        services.AddSingleton<IStartDialogService, StartDialogService>();
        services.AddSingleton<ICodeAnalysisService, CodeAnalysisService>();

        // Regiones del shell. El panel inferior se comparte: el diálogo de inicio
        // también escribe en él.
        services.AddSingleton<BottomPanelViewModel>();
        services.AddSingleton<IOutputLog, OutputLog>();
        services.AddSingleton<EditorHostViewModel>();
        services.AddSingleton<FileExplorerViewModel>();
        services.AddSingleton<ProjectWorkspaceViewModel>();
        services.AddSingleton(provider => provider.GetRequiredService<ProjectWorkspaceViewModel>().Commands);
        services.AddSingleton<MenuBarViewModel>();
        services.AddSingleton<StatusBarViewModel>();

        // El intérprete deja las ráfagas de pulse en el buzón y el panel de
        // partículas las recoge: ambos comparten la misma instancia.
        services.AddSingleton<IdeRuntimePulse>();
        services.AddSingleton<ParticlePanelViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddTransient<MainWindow>();
        services.AddTransient<StartWindow>();
    }

    // Solo registra: el comportamiento ante el fallo (cerrar la app) no cambia.
    private void ObserveUnhandledExceptions()
    {
        DispatcherUnhandledException += (_, e) => LogUnhandledUiException(e.Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => LogUnobservedTaskException(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                LogUnhandledException(exception);
            }
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "TmIde iniciado (.NET {RuntimeVersion})")]
    private partial void LogStarted(Version runtimeVersion);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Excepción no controlada en el hilo de la UI")]
    private partial void LogUnhandledUiException(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Excepción no observada en una tarea")]
    private partial void LogUnobservedTaskException(Exception exception);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Excepción no controlada")]
    private partial void LogUnhandledException(Exception exception);
}
