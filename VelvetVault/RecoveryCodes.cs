using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace VelvetVault;
public static class RecoveryCodes {
 public static List<string> Generate()=>Enumerable.Range(0,10).Select(_=>{string value=Convert.ToHexString(RandomNumberGenerator.GetBytes(16));return string.Join("-",Enumerable.Range(0,8).Select(i=>value.Substring(i*4,4)));}).ToList();
 public static string Hash(string code){string normalized=new(code.Where(c=>c!='-'&&!char.IsWhiteSpace(c)).Select(char.ToUpperInvariant).ToArray());if(normalized.Length!=32||normalized.Any(c=>!Uri.IsHexDigit(c)))return "";return Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(normalized)));}
 public static bool Consume(string path,string key,string code){
  string hash=Hash(code);if(hash.Length==0||key.Length==0||VaultStorage.LoadAccessKey(path)!=key)return false;var hashes=VaultStorage.LoadRecoveryHashes(path);int index=hashes.FindIndex(x=>CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(x),Encoding.ASCII.GetBytes(hash)));if(index<0)return false;hashes.RemoveAt(index);VaultStorage.Save(path,VaultStorage.Load(path),recoveryHashes:hashes);return true;
 }
 public static void Manage(Window owner,string path){string key=VaultStorage.LoadAccessKey(path);if(key.Length==0)return;if(!TransferPreview.Show(owner,"New recovery codes","This creates a new list and invalidates every older recovery code. You will verify your identity before continuing.",Array.Empty<Entry>(),"CONTINUE"))return;if(VaultAccess.Unlock(owner,key,path))GenerateAndShow(owner,path);}
 public static void GenerateAndShow(Window owner,string path){
  var codes=Generate();try{if(VaultStorage.LoadAccessKey(path).Length==0)throw new InvalidDataException();VaultStorage.Save(path,VaultStorage.Load(path),recoveryHashes:codes.Select(Hash).ToList());}catch{VaultPrompt.Show(owner,"Could not save recovery codes","2FA stays enabled. Try RECOVERY CODES again to generate a saved list.",false);return;}
  Show(owner,codes);
 }
 public static void Show(Window owner,List<string> codes){
  var dialog=VaultUI.Window(owner,"Vault",510,510);var root=VaultUI.Grid(new GridLength(8),new GridLength(1,GridUnitType.Star),new GridLength(48));root.Margin=new Thickness(14,2,14,6);root.Children.Add(new MotionRail{Height=3});var body=new StackPanel{Margin=new Thickness(17)};body.Children.Add(VaultUI.Label("OFFLINE / ONE-TIME RECOVERY",8));body.Children.Add(new TextBlock{Text="Save this list somewhere safe. Each code works once in this Vault file. These full codes are shown only now.",FontSize=12,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,12)});
  var list=new TextBox{Text=string.Join(Environment.NewLine,codes),IsReadOnly=true,AcceptsReturn=true,FontFamily=new FontFamily("Consolas"),FontSize=12,Foreground=Palette.Ink,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(0)};body.Children.Add(list);var status=new TextBlock{FontSize=10,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,9,0,0)};body.Children.Add(status);var frame=new RetroFrame{Child=new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden}};Grid.SetRow(frame,1);root.Children.Add(frame);
  var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};var save=new Button{Content="SAVE .TXT",FontSize=10};save.Click+=(_,_)=>{var picker=new Microsoft.Win32.SaveFileDialog{Title="Save recovery codes",Filter="Text file (*.txt)|*.txt",DefaultExt=".txt",AddExtension=true,OverwritePrompt=true,FileName="Vault-recovery-codes.txt"};if(picker.ShowDialog(dialog)!=true)return;try{File.WriteAllText(picker.FileName,"VAULT / SINGLE-USE RECOVERY CODES"+Environment.NewLine+"Keep this list safe. Each code works once in this Vault file."+Environment.NewLine+Environment.NewLine+string.Join(Environment.NewLine,codes),new UTF8Encoding(false));status.Text="Recovery codes saved.";}catch{status.Text="Could not save. Choose a writable folder.";}};var done=new Button{Content="DONE",IsCancel=true,FontSize=10,Margin=new Thickness(0)};done.Click+=(_,_)=>dialog.Close();actions.Children.Add(save);actions.Children.Add(done);Grid.SetRow(actions,2);root.Children.Add(actions);VaultChrome.Apply(dialog,root,"RECOVERY CODES");dialog.ShowDialog();
 }
}
