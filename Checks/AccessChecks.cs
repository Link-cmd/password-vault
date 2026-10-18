using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VelvetVault;
static class AccessChecks {
 static void Click(Window w,string name)=>((Button)w.FindName(name)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
 static string Code(string key)=>Totp.Code(new TotpAccount{Name="Test",Secret=key},DateTimeOffset.UtcNow.ToUnixTimeSeconds());
 static void Drive(Window w,Action action){var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(150)};timer.Tick+=(_,_)=>{timer.Stop();action();};timer.Start();w.ShowDialog();}
 public static void Run(){
  string key=VaultAccess.NewKey();if(key.Length!=32)throw new Exception("Setup key entropy/length failed");var parsed=Totp.Parse("",VaultAccess.SetupUri(key));if(parsed.Secret!=key||parsed.Digits!=6||parsed.Period!=30)throw new Exception("Setup QR URI invalid");long now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();if(!VaultAccess.Verify(key,Code(key),now)||VaultAccess.Verify(key,"abcdef",now)||VaultAccess.Verify(key,Totp.Code(parsed,now-90),now))throw new Exception("Unlock verifier failed");
  string file="access-check.tmp",backup="access-check.vaultbak";try{File.WriteAllBytes(file,new byte[32]);VaultStorage.Save(file,new List<Entry>(),accessKey:key);VaultStorage.Save(file,new List<Entry>{new(){Name="Synthetic test",Password="test-only"}},"day");if(VaultStorage.LoadAccessKey(file)!=key)throw new Exception("Normal save removed access key");VaultStorage.Backup(file,backup);if(VaultStorage.LoadAccessKey(backup)!=key)throw new Exception("Backup lost access setup");VaultStorage.Save(file,VaultStorage.Load(file),accessKey:"");if(VaultStorage.LoadAccessKey(file)!="")throw new Exception("Disable failed");}finally{foreach(string path in new[]{file,backup})if(File.Exists(path))File.Delete(path);}
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
  bool committed=false;var cancel=VaultAccess.Dialog(null,key,true,_=>{committed=true;return true;});Drive(cancel,()=>Click(cancel,"CancelCode"));if(committed||cancel.DialogResult==true)throw new Exception("Cancel enabled opening check");
  var unlock=VaultAccess.Dialog(null,key,false,null);if(((System.Windows.Media.SolidColorBrush)((TextBox)unlock.FindName("CodeInput")).Foreground).Color!=((System.Windows.Media.SolidColorBrush)Palette.Ink).Color)throw new Exception("Unlock code text does not match theme");Drive(unlock,()=>{((TextBox)unlock.FindName("CodeInput")).Text="bad";Click(unlock,"VerifyCode");if(unlock.DialogResult==true||!unlock.IsVisible)throw new Exception("Invalid code opened vault");((TextBox)unlock.FindName("CodeInput")).Text=Code(key);Click(unlock,"VerifyCode");});if(unlock.DialogResult!=true)throw new Exception("Correct code did not unlock");
  var setup=VaultAccess.Dialog(null,key,true,_=>{committed=true;return true;});Drive(setup,()=>{((TextBox)setup.FindName("CodeInput")).Text=Code(key);Click(setup,"VerifyCode");});if(!committed||setup.DialogResult!=true)throw new Exception("Confirmed setup failed");
  var throttle=VaultAccess.Dialog(null,key,false,null);Drive(throttle,()=>{for(int i=0;i<5;i++){((TextBox)throttle.FindName("CodeInput")).Text="bad";Click(throttle,"VerifyCode");}if(((Button)throttle.FindName("VerifyCode")).IsEnabled)throw new Exception("Retry cooldown missing");Click(throttle,"CancelCode");});
  app.Shutdown();Console.WriteLine("PASS: setup URI, code verification, cancel/confirm, unlock denial/success, retry cooldown, persistent encrypted access setup and backup");
 }
}
