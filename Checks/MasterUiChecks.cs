using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VelvetVault;
static class MasterUiChecks {
 static void Drive(Window window,Action action){var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(550)};timer.Tick+=(_,_)=>{timer.Stop();action();};timer.Start();window.ShowDialog();}
 public static void Run(){string path="master-ui-check.tmp";try{
  File.WriteAllBytes(path,new byte[32]);VaultStorage.Save(path,new List<Entry>());var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
  var setup=MasterPasswordUI.Create(null,path,true);Drive(setup,()=>{((Button)setup.FindName("MasterGenerate")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));string generated=((PasswordBox)setup.FindName("NewMaster")).Password;if(generated.Length!=32||generated!=((PasswordBox)setup.FindName("ConfirmMaster")).Password||VaultStorage.IsMasterProtected(path))throw new Exception("Master generator did not fill both fields without enabling protection");((PasswordBox)setup.FindName("NewMaster")).Password="Synthetic master password";((PasswordBox)setup.FindName("ConfirmMaster")).Password="different";((Button)setup.FindName("MasterSave")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));if(VaultStorage.IsMasterProtected(path))throw new Exception("Mismatched master password accepted");((PasswordBox)setup.FindName("ConfirmMaster")).Password="Synthetic master password";((Button)setup.FindName("MasterSave")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));});if(setup.DialogResult!=true||!VaultStorage.IsMasterProtected(path))throw new Exception("Master enable UI failed");
  VaultStorage.LockMaster(path);var unlock=MasterPasswordUI.Create(null,path,false);Drive(unlock,()=>{((PasswordBox)unlock.FindName("CurrentMaster")).Password="Synthetic master password";((Button)unlock.FindName("MasterSave")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));});if(unlock.DialogResult!=true)throw new Exception("Master unlock UI failed");
  var settings=MasterPasswordUI.Create(null,path,true);Drive(settings,()=>{settings.UpdateLayout();var shot=new RenderTargetBitmap((int)settings.ActualWidth,(int)settings.ActualHeight,96,96,PixelFormats.Pbgra32);shot.Render(settings);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(shot));using(var file=File.Create("master-settings-preview.png"))png.Save(file);((Button)settings.FindName("MasterCancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));});if(!VaultStorage.IsMasterProtected(path))throw new Exception("Cancel removed password protection");app.Shutdown();Console.WriteLine("PASS: optional master-password UI, mismatched confirmation rejection, enable, unlock, cancel without changes");
 }finally{VaultStorage.ClearMasterSessions();if(File.Exists(path))File.Delete(path);}}
}
