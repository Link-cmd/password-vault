using System.Diagnostics;
using System.IO;
using VelvetVault;
static class PackagedAccessChecks {
 public static void Run(bool master=false){
  string fixture=Path.Combine(Environment.CurrentDirectory,"startup-access-check-"+Guid.NewGuid().ToString("N")+".tmp");Process process=null;
  try{
   File.WriteAllBytes(fixture,new byte[32]);VaultStorage.Save(fixture,new List<Entry>(),accessKey:VaultAccess.NewKey());if(master)VaultStorage.ChangeMaster(fixture,"","Synthetic startup password");string expected=master?"Vault - Master Unlock":"Vault - Unlock";
   var start=new ProcessStartInfo(Path.GetFullPath("App/VelvetVault.exe")){UseShellExecute=false,WorkingDirectory=Environment.CurrentDirectory};start.ArgumentList.Add("--vault-session");start.ArgumentList.Add(fixture);process=Process.Start(start);
   string title="";var watch=Stopwatch.StartNew();while(watch.Elapsed<TimeSpan.FromSeconds(12)){process.Refresh();if(process.HasExited)throw new Exception("Packaged app exited before unlock screen");title=process.MainWindowTitle;if(title==expected)break;Thread.Sleep(100);}
   if(title!=expected)throw new Exception("Packaged startup did not show unlock screen; observed title: "+title);
   if(VaultStorage.LoadAccessKey(fixture).Length==0)throw new Exception("Startup removed saved setup");Console.WriteLine(master?"PASS: packaged app requires the master password before the authenticator screen":"PASS: fresh packaged process with saved setup starts at unlock screen and preserves setup");
  }finally{if(process!=null&&!process.HasExited){process.Kill();process.WaitForExit(5000);}process?.Dispose();VaultStorage.ClearMasterSessions();foreach(string path in new[]{fixture,fixture+".session"})if(File.Exists(path))File.Delete(path);}
 }
}
