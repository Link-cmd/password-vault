using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using VelvetVault;
static class SoundChecks {
 public static void Run(){
  var secret=new SoundSecret();if(secret.Enabled||secret.RegisterTap(0)||secret.RegisterTap(200)||!secret.RegisterTap(400)||!secret.Enabled)throw new Exception("Triple-tap enable failed");
  if(secret.RegisterTap(500)||secret.RegisterTap(600)||!secret.RegisterTap(700)||secret.Enabled)throw new Exception("Triple-tap disable failed");
  var slow=new SoundSecret();slow.RegisterTap(0);slow.RegisterTap(2000);if(slow.RegisterTap(4000)||slow.Enabled||SoundFx.Secret.Enabled)throw new Exception("Sound default or tap timeout failed");
  foreach(string name in new[]{"open","close"}){
   if(!File.Exists("VelvetVault/Sounds/"+name+".mp3"))continue;
   var asset=Application.GetResourceStream(new Uri("pack://application:,,,/VelvetVault;component/Sounds/"+name+".mp3"));if(asset==null)throw new Exception("Sound resource missing");using var input=asset.Stream;using var embedded=new MemoryStream();input.CopyTo(embedded);if(!embedded.ToArray().SequenceEqual(File.ReadAllBytes("VelvetVault/Sounds/"+name+".mp3")))throw new Exception("Embedded sound differs from supplied clip");
   var player=new MediaPlayer{Volume=0};var frame=new DispatcherFrame();bool decoded=false;var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(4)};timer.Tick+=(_,_)=>{timer.Stop();frame.Continue=false;};player.MediaOpened+=(_,_)=>{decoded=player.NaturalDuration.HasTimeSpan;frame.Continue=false;};player.MediaFailed+=(_,_)=>frame.Continue=false;timer.Start();player.Open(new Uri(Path.GetFullPath("VelvetVault/Sounds/"+name+".mp3")));Dispatcher.PushFrame(frame);timer.Stop();player.Close();if(!decoded)throw new Exception("Sound could not be decoded");
  }
  Console.WriteLine("PASS: sounds default off, triple-tap toggle and timeout, optional supplied MP3s decode when present");
 }
}

