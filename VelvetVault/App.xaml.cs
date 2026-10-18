using System.Diagnostics;
using System.IO;
using System.Windows;

namespace VelvetVault;
public partial class App : Application {
 public static string VaultPath {get;private set;}=Environment.ProcessPath!;
 FileStream? sessionLock;
 protected override void OnStartup(StartupEventArgs e){
  base.OnStartup(e);
  try {
   if(e.Args.Length==2 && e.Args[0]=="--vault-session") {
    VaultPath=Path.GetFullPath(e.Args[1]);
    sessionLock=new FileStream(VaultPath+".session",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None,1,FileOptions.DeleteOnClose);
    ShutdownMode=ShutdownMode.OnExplicitShutdown;
    if(!MasterPasswordUI.Unlock(null,VaultPath)){Shutdown();return;}
    Palette.Night=VaultStorage.ReadTheme(VaultPath)!="day";
    if(!VaultAccess.Unlock(null,VaultStorage.LoadAccessKey(VaultPath),VaultPath)){Shutdown();return;}
    MainWindow=new MainWindow();ShutdownMode=ShutdownMode.OnMainWindowClose;MainWindow.Show();
   } else {
    var source=Environment.ProcessPath!;
    // Execute a copy so the portable original can be replaced while the window is open.
    var cache=Path.Combine(Path.GetTempPath(),"PasswordRegisterRun",Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(cache);
    var runner=Path.Combine(cache,"Password Register.exe");
    VaultStorage.CreateRunner(source,runner);
    var start=new ProcessStartInfo(runner){UseShellExecute=false,WorkingDirectory=cache};
    start.ArgumentList.Add("--vault-session");start.ArgumentList.Add(source);
    Process.Start(start);
    Shutdown();
   }
  }catch(Exception ex){VaultPrompt.Show(null,"Cannot open Vault","Close any other copy, and check that the SSD is connected and writable.\n\n"+ex.Message);Shutdown();}
 }
 protected override void OnExit(ExitEventArgs e){VaultStorage.ClearMasterSessions();sessionLock?.Dispose();base.OnExit(e);}
}


