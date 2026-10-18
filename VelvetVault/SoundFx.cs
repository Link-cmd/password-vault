using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
namespace VelvetVault;
public sealed class SoundSecret {
 public bool Enabled {get;private set;}
 int taps;long last=long.MinValue;
 public bool RegisterTap(long time){if(last==long.MinValue||time-last>1200||time<last)taps=0;last=time;if(++taps<3)return false;taps=0;Enabled=!Enabled;return true;}
}
public static class SoundFx {
 public static SoundSecret Secret {get;}=new();
 static readonly List<MediaPlayer> active=new();
 static string? directory;
 public static void Attach(Window window){
  bool pending=false,allowed=false;
  window.Loaded+=(_,_)=>Play(true);
  if(window is MainWindow){window.Closing+=(_,e)=>{if(allowed||!Secret.Enabled)return;e.Cancel=true;if(pending)return;pending=true;Play(false,()=>{allowed=true;window.Close();});};}
  else window.Closed+=(_,_)=>Play(false);
 }
 public static void Play(bool opening,Action? completed=null){
  if(!Secret.Enabled){completed?.Invoke();return;}
  try{
   directory??=Path.Combine(Path.GetTempPath(),"VaultSounds",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);string name=opening?"open.mp3":"close.mp3",path=Path.Combine(directory,name);
   if(!File.Exists(path)){var asset=Application.GetResourceStream(new Uri("pack://application:,,,/VelvetVault;component/Sounds/"+name));if(asset==null){completed?.Invoke();return;}using var input=asset.Stream;using var output=File.Create(path);input.CopyTo(output);}
   var player=new MediaPlayer{Volume=.04};active.Add(player);bool finished=false;var timeout=new DispatcherTimer{Interval=TimeSpan.FromSeconds(4)};
   void Finish(){if(finished)return;finished=true;timeout.Stop();player.Close();active.Remove(player);completed?.Invoke();}
   player.MediaOpened+=(_,_)=>player.Play();player.MediaEnded+=(_,_)=>Finish();player.MediaFailed+=(_,_)=>Finish();timeout.Tick+=(_,_)=>Finish();timeout.Start();player.Open(new Uri(path));
  }catch{completed?.Invoke();}
 }
}
