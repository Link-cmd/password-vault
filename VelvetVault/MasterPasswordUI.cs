using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace VelvetVault;
public static class MasterPasswordUI {
 public static bool Unlock(Window? owner,string path){if(!VaultStorage.IsMasterProtected(path))return true;return Create(owner,path,false).ShowDialog()==true;}
 public static void Manage(Window owner,string path,Action changed){if(Create(owner,path,true).ShowDialog()==true)changed();}
 public static Window Create(Window? owner,string path,bool manage){
  bool enabled=VaultStorage.IsMasterProtected(path);var window=VaultUI.Window(owner,manage?"Vault - Master Password":"Vault - Master Unlock",460,manage?480:300);window.ResizeMode=ResizeMode.NoResize;window.ShowInTaskbar=owner==null;NameScope.SetNameScope(window,new NameScope());
  var root=VaultUI.Grid(new GridLength(8),new GridLength(1,GridUnitType.Star),new GridLength(48));root.Margin=new Thickness(14,2,14,6);root.Children.Add(new MotionRail{Height=3});var body=new StackPanel{Margin=new Thickness(17)};
  body.Children.Add(VaultUI.Label(manage?(enabled?"MASTER PASSWORD / ENABLED":"MASTER PASSWORD / OPTIONAL") : "PASSWORD / ENCRYPTED VAULT",8));
  body.Children.Add(new TextBlock{Text=manage?"A master password is optional. When enabled, it encrypts your vault using password-derived keys.":"Enter your master password to decrypt this vault.",FontSize=12,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,12)});
  PasswordBox Field(string label,string name){body.Children.Add(new TextBlock{Text=label,FontSize=9,Margin=new Thickness(0,9,0,5)});var input=new PasswordBox{FontSize=15,Foreground=Palette.Ink,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(0)};window.RegisterName(name,input);body.Children.Add(new Border{Child=FieldCaret.Wrap(input),BorderBrush=Palette.Ink,BorderThickness=new Thickness(1.2),CornerRadius=new CornerRadius(6),Padding=new Thickness(9,6,9,6)});return input;}
  PasswordBox? current=enabled?Field("CURRENT MASTER PASSWORD","CurrentMaster"):null;
  PasswordBox? password=manage?Field("NEW MASTER PASSWORD / 12+ CHARACTERS","NewMaster"):null;PasswordBox? confirm=manage?Field("CONFIRM NEW PASSWORD","ConfirmMaster"):null;
  if(manage)body.Children.Add(new TextBlock{Text="Keep this password safe. Authenticator and recovery codes cannot recover a forgotten master password. Older backups keep their original password.",FontSize=10,Foreground=Palette.Muted,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0)});
  Button? generate=null,saveText=null;
  var error=new TextBlock{FontSize=10,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,0)};body.Children.Add(error);var frame=new RetroFrame{Child=new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden}};Grid.SetRow(frame,1);root.Children.Add(frame);
  if(manage){var tools=new WrapPanel{Margin=new Thickness(0,12,0,0)};generate=new Button{Content="⟳ GENERATE / 32",FontSize=10,Padding=new Thickness(10,6,10,6)};saveText=new Button{Content="SAVE .TXT",FontSize=10,Padding=new Thickness(10,6,10,6)};window.RegisterName("MasterGenerate",generate);window.RegisterName("MasterSaveText",saveText);
   generate.Click+=(_,_)=>{string generated=MasterPasswordTools.Generate();password!.Password=generated;confirm!.Password=generated;error.Text="Generated. Save the text copy if needed, then click ENABLE or CHANGE.";};
   saveText.Click+=(_,_)=>{string value=password!.Password;if(value.Length<12||string.IsNullOrWhiteSpace(value)||value!=confirm!.Password){error.Text="Generate or enter matching new passwords before saving a text copy.";return;}var picker=new Microsoft.Win32.SaveFileDialog{Title="Save master password as plain text",Filter="Text file (*.txt)|*.txt",DefaultExt=".txt",AddExtension=true,OverwritePrompt=true,FileName="Vault-master-password.txt"};if(picker.ShowDialog(window)!=true)return;try{MasterPasswordTools.SaveText(picker.FileName,value);error.Text="Text copy saved. Click ENABLE or CHANGE to apply this password.";}catch{error.Text="Could not save. Choose a writable .txt file.";}};
   tools.Children.Add(generate);tools.Children.Add(saveText);body.Children.Insert(body.Children.Count-1,tools);body.Children.Insert(body.Children.Count-1,new TextBlock{Text="The saved .txt is plain text. Keep it separate from your vault.",FontSize=9,Foreground=Palette.Muted,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,7,0,0)});
  }
  var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};var cancel=new Button{Content="CANCEL",IsCancel=true,FontSize=10};cancel.Click+=(_,_)=>window.DialogResult=false;actions.Children.Add(cancel);
  var remove=new Button{Content="REMOVE",FontSize=10,Visibility=manage&&enabled?Visibility.Visible:Visibility.Collapsed};actions.Children.Add(remove);var save=new Button{Content=manage?(enabled?"CHANGE":"ENABLE"):"UNLOCK",FontSize=10,IsDefault=true,Background=Palette.Ink,Foreground=Palette.Paper,Margin=new Thickness(0)};actions.Children.Add(save);window.RegisterName("MasterSave",save);window.RegisterName("MasterRemove",remove);window.RegisterName("MasterCancel",cancel);
  bool busy=false;window.Closing+=(_,e)=>{if(busy)e.Cancel=true;};
  async void Run(bool removing){
   if(busy)return;string old=current?.Password??"",next=password?.Password??"";if(manage&&!removing&&(next.Length<12||string.IsNullOrWhiteSpace(next)||next!=confirm?.Password)){error.Text="Use at least 12 characters and make both new passwords match.";return;}
   busy=true;body.IsEnabled=false;cancel.IsEnabled=remove.IsEnabled=save.IsEnabled=false;if(generate!=null)generate.IsEnabled=false;if(saveText!=null)saveText.IsEnabled=false;error.Text="Working…";
   try{bool success=await Task.Run(()=>{if(manage){VaultStorage.ChangeMaster(path,old,removing?null:next);return true;}return VaultStorage.UnlockMaster(path,old);});if(!success){error.Text="Master password did not match.";return;}if(current!=null)current.Clear();password?.Clear();confirm?.Clear();busy=false;window.DialogResult=true;}
   catch(System.Security.Cryptography.CryptographicException){error.Text="Master password did not match, or the vault could not be verified.";}catch{error.Text="Could not update or open the vault. Your existing file is preserved.";}finally{busy=false;body.IsEnabled=true;cancel.IsEnabled=remove.IsEnabled=save.IsEnabled=true;if(generate!=null)generate.IsEnabled=true;if(saveText!=null)saveText.IsEnabled=true;}
  }
  remove.Click+=(_,_)=>Run(true);save.Click+=(_,_)=>Run(false);Grid.SetRow(actions,2);root.Children.Add(actions);VaultChrome.Apply(window,root,manage?"MASTER PASSWORD":"UNLOCK VAULT");window.Loaded+=(_,_)=>{window.Activate();(current??password)?.Focus();};return window;
 }
}
