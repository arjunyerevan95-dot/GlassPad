using System.Windows;

namespace GlassPad;

public partial class App : Application
{
    private Mutex? _instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        _instance=new Mutex(true,"Local\\GlassPad.Queens.Pilot.v1",out var created);
        if(!created){MessageBox.Show("GlassPad Queen’s pilot is already running. Return to its launcher to customize or stop it.","GlassPad");Shutdown();return;}
        AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.DisableStylusAndTouchSupport", true);
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) => {
            try { System.IO.Directory.CreateDirectory(Queens.Settings.Root); System.IO.File.AppendAllText(System.IO.Path.Combine(Queens.Settings.Root,"crash.log"), DateTimeOffset.Now+" "+args.Exception+"\n"); } catch { }
        };
        new Queens.Launcher(e.Args).Show();
    }
    protected override void OnExit(ExitEventArgs e){try{_instance?.ReleaseMutex();}catch(ApplicationException){} _instance?.Dispose();base.OnExit(e);}
}
