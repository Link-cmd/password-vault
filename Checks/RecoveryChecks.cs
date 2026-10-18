using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VelvetVault;
static class RecoveryChecks {
 public static void Run(){
  string path=Path.Combine(Environment.CurrentDirectory,"recovery-check.tmp"),backup=path+".vaultbak";try{
   string key=VaultAccess.NewKey();var codes=RecoveryCodes.Generate();if(codes.Count!=10||codes.Distinct().Count()!=10||codes.Any(c=>c.Length!=39))throw new Exception("Recovery code generation failed");
   File.WriteAllBytes(path,new byte[32]);VaultStorage.Save(path,new List<Entry>{new(){Name="Test",Password="test-only"}},accessKey:key,recoveryHashes:codes.Select(RecoveryCodes.Hash).ToList());
   if(!RecoveryCodes.Consume(path,key,codes[0].ToLowerInvariant())||RecoveryCodes.Consume(path,key,codes[0]))throw new Exception("Recovery code was not single-use");
   if(RecoveryCodes.Consume(path,key,"not-a-code")||VaultStorage.LoadRecoveryHashes(path).Count!=9)throw new Exception("Invalid code changed recovery state");VaultStorage.Save(path,VaultStorage.Load(path),"day");if(VaultStorage.LoadRecoveryHashes(path).Count!=9)throw new Exception("Regular save reset recovery codes");VaultStorage.Backup(path,backup);if(VaultStorage.LoadRecoveryHashes(backup).Count!=9)throw new Exception("Backup lost recovery state");
   var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var window=VaultAccess.Dialog(null,key,false,null,path);var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(250)};timer.Tick+=(_,_)=>{timer.Stop();((Button)window.FindName("RecoveryMode")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));((TextBox)window.FindName("CodeInput")).Text=codes[1];((Button)window.FindName("VerifyCode")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));};timer.Start();if(window.ShowDialog()!=true||VaultStorage.LoadRecoveryHashes(path).Count!=8)throw new Exception("Recovery unlock did not consume code");
   VaultStorage.Save(path,VaultStorage.Load(path),accessKey:"");if(VaultStorage.LoadRecoveryHashes(path).Count!=0)throw new Exception("Disabling 2FA did not clear recovery codes");app.Shutdown();Console.WriteLine("PASS: 10 unique recovery codes, hashed persistence, single use, invalid-code rejection, backup, recovery unlock, clearing on disable");
  }finally{foreach(string file in new[]{path,backup})if(File.Exists(file))File.Delete(file);}
 }
}
