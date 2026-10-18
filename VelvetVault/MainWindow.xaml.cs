using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace VelvetVault;
public class Entry {
 public Guid Id {get;set;}=Guid.NewGuid();
 public string Name {get;set;}="";
 public string Username {get;set;}="";
 public string Email {get;set;}="";
 public string Password {get;set;}="";
 public string Website {get;set;}="";
 public string Category {get;set;}="Personal";
 public string Notes {get;set;}="";
}
internal static class LegacyVaultStorage {
 static readonly byte[] Marker=Encoding.ASCII.GetBytes("PWREGISTER-V1END!");
 static readonly int FooterSize=8+Marker.Length;
 public const long DataCapacity=64L*1024*1024;
 public static long DataBytes(string path){var container=ReadContainer(path);return container.payload?.LongLength??0;}
 static (long baseLength,byte[]? payload) ReadContainer(string path){
  using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
  if(file.Length<FooterSize)return(file.Length,null);
  file.Position=file.Length-FooterSize;
  using var reader=new BinaryReader(file,Encoding.UTF8,true);
  long length=reader.ReadInt64();var marker=reader.ReadBytes(Marker.Length);
  if(!marker.SequenceEqual(Marker))return(file.Length,null);
  if(length<60||length>DataCapacity||length>file.Length-FooterSize)throw new InvalidDataException("Invalid embedded register.");
  long baseLength=file.Length-FooterSize-length;
  file.Position=baseLength;var payload=reader.ReadBytes((int)length);
  return(baseLength,payload);
 }
 public static void CreateRunner(string path,string destination){ var length=ReadContainer(path).baseLength;using var input=File.OpenRead(path);using var output=File.Create(destination);var buffer=new byte[81920];while(length>0){int count=input.Read(buffer,0,(int)Math.Min(buffer.Length,length));if(count==0)throw new EndOfStreamException();output.Write(buffer,0,count);length-=count;}}
 public static void Save(string path,List<Entry> entries,string? theme=null){
  var container=ReadContainer(path);
  var plain=JsonSerializer.SerializeToUtf8Bytes(new{Entries=entries,Theme=theme??ReadTheme(path)});
  if(plain.LongLength+60>DataCapacity){CryptographicOperations.ZeroMemory(plain);throw new InvalidDataException("Vault storage is full.");}
  var key=RandomNumberGenerator.GetBytes(32);var nonce=RandomNumberGenerator.GetBytes(12);var tag=new byte[16];var cipher=new byte[plain.Length];
  string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{
   using(var aes=new AesGcm(key,16))aes.Encrypt(nonce,plain,cipher,tag);
   using(var output=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
    using(var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){
     var buffer=new byte[81920];long remaining=container.baseLength;
     while(remaining>0){int count=input.Read(buffer,0,(int)Math.Min(buffer.Length,remaining));if(count==0)throw new EndOfStreamException();output.Write(buffer,0,count);remaining-=count;}
    }
    output.Write(key);output.Write(nonce);output.Write(tag);output.Write(cipher);
    using(var writer=new BinaryWriter(output,Encoding.UTF8,true)){writer.Write((long)(60+cipher.Length));writer.Write(Marker);}
    output.Flush(true);
   }
   // Verify the completed replacement before committing it to the SSD.
   if(Load(temp).Count!=entries.Count)throw new InvalidDataException("Write verification failed.");
   File.Move(temp,path,true);
  }finally{CryptographicOperations.ZeroMemory(plain);CryptographicOperations.ZeroMemory(key);if(File.Exists(temp))File.Delete(temp);}
 }
 public static string ReadTheme(string path){var payload=ReadContainer(path).payload;if(payload==null)return "night";var plain=new byte[payload.Length-60];try{using(var aes=new AesGcm(payload.AsSpan(0,32),16))aes.Decrypt(payload.AsSpan(32,12),payload.AsSpan(60),payload.AsSpan(44,16),plain);using var doc=JsonDocument.Parse(plain);return doc.RootElement.ValueKind==JsonValueKind.Object&&doc.RootElement.TryGetProperty("Theme",out var t)&&t.GetString()=="day"?"day":"night";}finally{CryptographicOperations.ZeroMemory(plain);CryptographicOperations.ZeroMemory(payload);}}
 public static List<Entry> Load(string path){
  var payload=ReadContainer(path).payload;if(payload==null)return new();
  var plain=new byte[payload.Length-60];
  try{using(var aes=new AesGcm(payload.AsSpan(0,32),16))aes.Decrypt(payload.AsSpan(32,12),payload.AsSpan(60),payload.AsSpan(44,16),plain);using var document=JsonDocument.Parse(plain);var root=document.RootElement;return (root.ValueKind==JsonValueKind.Array?root:root.GetProperty("Entries")).Deserialize<List<Entry>>()??throw new InvalidDataException("Invalid register.");}
  finally{CryptographicOperations.ZeroMemory(plain);CryptographicOperations.ZeroMemory(payload);}
 }
}
public partial class MainWindow : Window {
 readonly DateTime openedAt=DateTime.Now; Guid? selectedId; List<Entry> entries=new(); List<TotpAccount> authenticators=new(); string accessKey=""; bool ready=false; string? clipboardValue; DispatcherTimer clipboardTimer=new(){Interval=TimeSpan.FromSeconds(25)};
 readonly string path=App.VaultPath;
 public Button? ThemeButton;
 public MainWindow(){InitializeComponent();if(BuildProfile.HalfSize){Width=530;Height=330;MinWidth=530;MinHeight=330;MaxWidth=1060;MaxHeight=660;AspectSizer.Attach(this);}try{if(File.Exists(path)){entries=VaultStorage.Load(path);authenticators=VaultStorage.LoadAuthenticators(path);accessKey=VaultStorage.LoadAccessKey(path);Palette.Night=VaultStorage.ReadTheme(path)!="day";}}catch{VaultPrompt.Show(null,"Cannot open Vault","The embedded vault could not be opened. It has not been changed.");Close();return;}ApplyTheme();VaultChrome.Apply(this,(UIElement)Content);ready=true;RecordsViewport.SizeChanged+=(_,_)=>{if(ready)Render();};clipboardTimer.Tick+=(_,_)=>ClearClipboard();Closed+=(_,_)=>ClearClipboard();PreviewKeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.F3){Search.Focus();e.Handled=true;}};Render();}
 void ApplyTheme(){Resources["Ink"]=Palette.Ink;Resources["Paper"]=Palette.Paper;Resources["Muted"]=Palette.Muted;Resources["Faint"]=Palette.Faint;if(ThemeButton!=null)ThemeButton.Content=Palette.Night?"☀ DAY":"☾ NIGHT";}
 public void ToggleTheme(){Palette.Night=!Palette.Night;ApplyTheme();Render();Palette.Invalidate(this);try{VaultStorage.Save(path,entries,Palette.Night?"night":"day");Status.Text="SAVED IN THIS .EXE";UpdateStorage();}catch{Status.Text="THEME CHANGED / COULD NOT SAVE PREFERENCE";}}
 void ClearClipboard(){clipboardTimer.Stop();try {if(clipboardValue!=null&&Clipboard.ContainsText()&&Clipboard.GetText()==clipboardValue)Clipboard.Clear();}catch{} clipboardValue=null;}
 void Copy(string value){try {Clipboard.SetText(value);clipboardValue=value;clipboardTimer.Stop();clipboardTimer.Start();Status.Text="Copied / clears in 25 seconds";}catch {VaultPrompt.Show(this,"Clipboard busy","The clipboard is being used by another app. Please try again.");}}
 void CategoryChanged(object sender,SelectionChangedEventArgs e){if(ready)Render();}
 void SearchChanged(object sender,TextChangedEventArgs e){if(ready)Render();}
 void AddClick(object sender,RoutedEventArgs e)=>Edit(null);
 bool Save(List<Entry> next,string? theme=null,List<TotpAccount>? tokens=null,string? gateKey=null,List<string>? recoveryHashes=null){try{VaultStorage.Save(path,next,theme,tokens,gateKey,recoveryHashes);entries=next;if(gateKey!=null)accessKey=gateKey;if(tokens!=null)authenticators=tokens;if(theme!=null){Palette.Night=theme!="day";ApplyTheme();Palette.Invalidate(this);}Render();Status.Text="SAVED INSIDE THIS .EXE";return true;}catch{VaultPrompt.Show(this,"Could not save","Your previous vault has been preserved. Check that the SSD is connected, writable, and has free space.");return false;}}
 void Render(){if(!ready)return;Authenticator.Configure(accessKey.Length>0,()=>VaultAccess.Setup(this,key=>{if(!Save(entries,gateKey:key))return false;if(VaultStorage.LoadAccessKey(path)!=key)throw new InvalidDataException("Authenticator setup was not saved.");return true;},path),()=>{if(VaultAccess.Unlock(this,accessKey,path))Save(entries,gateKey:"");},()=>RecoveryCodes.Manage(this,path));UpdateStorage();Cards.Children.Clear();var category="All passwords";string q=Search.Text.Trim();var visible=entries.Where(x=>(category=="All passwords"||x.Category==category)&&(x.Name+" "+x.Username+" "+x.Email+" "+x.Website+" "+x.Notes).Contains(q,StringComparison.OrdinalIgnoreCase)).OrderBy(x=>x.Name).ToList();CountLabel.Text=$"RECORDS / {visible.Count:D3}";EmptyState.Visibility=visible.Count==0?Visibility.Visible:Visibility.Collapsed;EmptyLabel.Text=entries.Count==0?"VAULT / EMPTY":"NO MATCHES";
 if(visible.Count>0&&!visible.Any(x=>x.Id==selectedId))selectedId=visible[0].Id;
 foreach(var item in visible){
  int index=visible.IndexOf(item)+1;bool selected=selectedId==item.Id;
  var frame=new RetroFrame{MinHeight=visible.Count==1?Math.Max(265,RecordsViewport.ActualHeight-12):265,Margin=new Thickness(0,0,0,11)};
  var grid=new Grid{Margin=new Thickness(24,20,23,19)};grid.RowDefinitions.Add(new RowDefinition());grid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(51)});
  var stack=new StackPanel{Margin=new Thickness(0,0,13,0)};
  var meta=new Grid{Margin=new Thickness(0,0,0,10)};meta.Children.Add(new TextBlock{Text=$"RECORD {index:D3} // ACCOUNT CREDENTIALS",FontSize=8});meta.Children.Add(new TextBlock{Text=$"SERIAL: {index:D4}-6B",FontSize=8,HorizontalAlignment=HorizontalAlignment.Right});stack.Children.Add(meta);
  var header=new Border{CornerRadius=new CornerRadius(10),Background=Palette.Night?new SolidColorBrush(Color.FromRgb(19,20,17)):Brushes.Transparent,BorderBrush=Palette.Ink,BorderThickness=new Thickness(1.5),Cursor=System.Windows.Input.Cursors.Hand,Padding=new Thickness(9,9,12,9)};
  var titleGrid=new Grid();titleGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(56)});titleGrid.ColumnDefinitions.Add(new ColumnDefinition());titleGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(36)});
  var numberPanel=new StackPanel{Margin=new Thickness(5)};numberPanel.Children.Add(new TextBlock{Text="NO.",FontSize=7,Foreground=Palette.Ink});numberPanel.Children.Add(new TextBlock{Text=index.ToString(),FontFamily=new FontFamily("Bahnschrift"),FontSize=37,Foreground=Palette.Ink,HorizontalAlignment=HorizontalAlignment.Center});titleGrid.Children.Add(new Border{CornerRadius=new CornerRadius(7),BorderBrush=Palette.Ink,BorderThickness=new Thickness(1.5),Child=numberPanel});
  var title=new StackPanel{Margin=new Thickness(15,2,8,2),VerticalAlignment=VerticalAlignment.Center};title.Children.Add(new TextBlock{Text=item.Name.ToUpperInvariant(),FontFamily=new FontFamily(new Uri("pack://application:,,,/VelvetVault;component/"),"./Fonts/#Saira Stencil"),FontSize=27,FontWeight=FontWeights.ExtraBold,Foreground=Palette.Ink,TextWrapping=TextWrapping.Wrap});title.Children.Add(new GlitchLabel{Ink=Palette.Ink,Margin=new Thickness(0,6,0,0),Height=14,Scale=1.5});Grid.SetColumn(title,1);titleGrid.Children.Add(title);
  var arrow=new Border{BorderBrush=Palette.Ink,BorderThickness=new Thickness(1.5),CornerRadius=new CornerRadius(20),Width=31,Height=31,VerticalAlignment=VerticalAlignment.Center,Child=new TextBlock{Text="»",FontSize=24,Foreground=Palette.Ink,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center}};Grid.SetColumn(arrow,2);titleGrid.Children.Add(arrow);header.Child=titleGrid;header.MouseLeftButtonUp+=(_,_)=>{selectedId=item.Id;Edit(item);};stack.Children.Add(header);
  var actionsRow=new Grid{Margin=new Thickness(0,14,0,0)};actionsRow.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});actionsRow.ColumnDefinitions.Add(new ColumnDefinition());actionsRow.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(48)});
  var actions=new WrapPanel();
  void Btn(string label,string caption,Action action){var box=new StackPanel{Margin=new Thickness(0,0,8,0)};var b=new Button{Content=new ActionGlyph(label){Width=25,Height=25},ToolTip=label,Width=45,Height=45,Padding=new Thickness(9),Margin=new Thickness(0)};System.Windows.Automation.AutomationProperties.SetName(b,label);b.Click+=(_,_)=>action();box.Children.Add(b);box.Children.Add(new TextBlock{Text=caption,FontSize=7,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,5,0,0)});actions.Children.Add(box);}
  if(item.Username.Length>0)Btn("Copy user","USER",()=>Copy(item.Username));if(item.Email.Length>0)Btn("Copy email","EMAIL",()=>Copy(item.Email));if(item.Password.Length>0)Btn("Copy password","KEY",()=>Copy(item.Password));Btn("View / edit","EDIT",()=>Edit(item));if(item.Notes.Length>0)Btn("Details","NOTES",()=>ViewDetails(item));Btn("Delete","DELETE",()=>{if(VaultPrompt.Show(this,"Delete record?",$"Delete {item.Name}? This removes it from this vault.",true))Save(entries.Where(x=>x.Id!=item.Id).ToList());});actionsRow.Children.Add(actions);
  var readout=new StackPanel{Margin=new Thickness(12,7,7,0),VerticalAlignment=VerticalAlignment.Center};readout.Children.Add(new StripeMeter{Height=16});readout.Children.Add(new TextBlock{Text=$"{index:D2}/{visible.Count:D2}   LOCAL ARCHIVE",FontSize=8,Margin=new Thickness(0,6,0,0)});Grid.SetColumn(readout,1);actionsRow.Children.Add(readout);var dots=new DotCounter{Number=index,Height=57};Grid.SetColumn(dots,2);actionsRow.Children.Add(dots);stack.Children.Add(actionsRow);stack.Children.Add(new TextBlock{Text=$"LOCAL RECORD / NO MASTER KEY\nSESSION: {openedAt:yyyy-MM-dd / HH:mm}",FontSize=8,Foreground=Palette.Muted,Margin=new Thickness(0,12,0,0)});
  var footer=new Grid{Margin=new Thickness(0,14,0,0)};footer.Children.Add(new Border{BorderBrush=Palette.Ink,BorderThickness=new Thickness(0,1,0,0),Padding=new Thickness(0,6,0,0),Child=new TextBlock{Text="+ + +  ARCHIVE / LOCAL",FontSize=8}});var bottomCode=new Barcode{Data=item.Name,Width=130,Height=12,Margin=new Thickness(0,7,0,0),HorizontalAlignment=HorizontalAlignment.Right};footer.Children.Add(bottomCode);Grid.SetRow(footer,1);Grid.SetColumnSpan(footer,2);grid.Children.Add(footer);grid.Children.Add(stack);
  var stripePanel=new StackPanel{VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};stripePanel.Children.Add(new Barcode{Data=item.Name,Width=158,Height=30,Cursor=System.Windows.Input.Cursors.Hand,ToolTip="Click to scan saved account information",LayoutTransform=new RotateTransform(90)});stripePanel.MouseLeftButtonUp+=(_,_)=>ShowBarcode(item);stripePanel.Children.Add(new TextBlock{Text="128",FontSize=8,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,5,0,0)});Grid.SetColumn(stripePanel,1);grid.Children.Add(stripePanel);frame.Child=grid;Cards.Children.Add(frame);
 }
 }
 void ShowBarcode(Entry item){
  AccountCode.Show(this,item);
 }

 void ViewDetails(Entry item){var dialog=VaultUI.Window(this,"Vault",560,495);var root=VaultUI.Grid(new GridLength(61),new GridLength(6),new GridLength(1,GridUnitType.Star),new GridLength(47));root.Margin=new Thickness(18,8,18,8);var header=new StackPanel();header.Children.Add(VaultUI.Label("RECOVERY / RECORD DETAILS",8));header.Children.Add(new TextBlock{Text=item.Name.ToUpperInvariant(),FontSize=23,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,7,0,0)});root.Children.Add(header);var rail=new MotionRail{Height=3};Grid.SetRow(rail,1);root.Children.Add(rail);var text=new TextBox{Text=item.Notes,IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden,BorderThickness=new Thickness(0),Background=Brushes.Transparent,Foreground=Palette.Ink,Padding=new Thickness(0),Margin=new Thickness(20),FontSize=13};var frame=new RetroFrame{Child=text};Grid.SetRow(frame,2);root.Children.Add(frame);var close=new Button{Content="CLOSE DETAILS",IsCancel=true,FontSize=10,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0)};close.Click+=(_,_)=>dialog.Close();Grid.SetRow(close,3);root.Children.Add(close);VaultChrome.Apply(dialog,root);dialog.ShowDialog();}
 void UpdateStorage(){StorageUsage.Text=StorageReadout.Read(path);StorageUsage.ToolTip="Vault.exe size / 4 GB file ceiling, including the application";}
 bool ImportMany(Window owner){
  var picker=new Microsoft.Win32.OpenFileDialog{Title="Import saved accounts",Filter="Vault text export (*.txt)|*.txt",Multiselect=false};if(picker.ShowDialog(owner)!=true)return false;
  try{var incoming=AccountImport.Read(picker.FileName);var next=AccountImport.Merge(entries,incoming);int added=next.Count-entries.Count;if(!TransferPreview.Show(owner,"Import accounts",$"{added} accounts will be added. {incoming.Count-added} exact duplicates will be skipped. Existing accounts stay in place.",incoming,"IMPORT ACCOUNTS"))return false;if(added==0)return true;return Save(next);}catch{VaultPrompt.Show(owner,"Could not import","Choose a complete Vault .txt export. No accounts were changed.",false);return false;}
 }
 void MasterClick(object sender,RoutedEventArgs e)=>MasterPasswordUI.Manage(this,path,()=>{UpdateStorage();Status.Text=VaultStorage.IsMasterProtected(path)?"MASTER PASSWORD ENABLED":"PASSWORD-FREE MODE";});
 void BackupClick(object sender,RoutedEventArgs e){
  var picker=new Microsoft.Win32.OpenFolderDialog{Title="Choose a backup folder",Multiselect=false};if(picker.ShowDialog(this)!=true)return;
  try{VaultStorage.Save(path,entries);string target=Path.Combine(picker.FolderName,"Vault-backup-"+DateTime.Now.ToString("yyyy-MM-dd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..4]+".vaultbak");VaultStorage.Backup(path,target);UpdateStorage();Status.Text="ENCRYPTED BACKUP SAVED";}catch{VaultPrompt.Show(this,"Could not back up","Choose a writable folder with enough space and try again.",false);}
 }
 void RestoreClick(object sender,RoutedEventArgs e){
  var picker=new Microsoft.Win32.OpenFileDialog{Title="Restore an encrypted backup",Filter="Vault backup (*.vaultbak)|*.vaultbak",Multiselect=false};if(picker.ShowDialog(this)!=true)return;
  try{if(!MasterPasswordUI.Unlock(this,picker.FileName))return;var restored=VaultStorage.LoadBackup(picker.FileName);string theme=VaultStorage.ReadTheme(picker.FileName);var tokens=VaultStorage.LoadAuthenticators(picker.FileName);string backupAccess=VaultStorage.LoadAccessKey(picker.FileName);if(!VaultAccess.Unlock(this,backupAccess,picker.FileName))return;if(!TransferPreview.Show(this,"Restore backup",$"This replaces all {entries.Count} current accounts with the {restored.Count} accounts below and {tokens.Count} authenticator accounts. The backup’s 2FA setting will also be restored. Master-password protection will be retained. Cancel keeps your current vault.",restored,"RESTORE BACKUP"))return;VaultStorage.RestoreBackup(path,picker.FileName);entries=VaultStorage.Load(path);authenticators=VaultStorage.LoadAuthenticators(path);accessKey=VaultStorage.LoadAccessKey(path);Palette.Night=theme!="day";ApplyTheme();Palette.Invalidate(this);Render();Status.Text="BACKUP RESTORED";}catch{VaultPrompt.Show(this,"Cannot restore backup","This backup could not be verified or opened. Your current vault has not changed.",false);}
 }
 void LocalTapped(object sender,System.Windows.Input.MouseButtonEventArgs e){if(SoundFx.Secret.RegisterTap(Environment.TickCount64)){SoundLight.Opacity=SoundFx.Secret.Enabled?1:0;if(SoundFx.Secret.Enabled)SoundFx.Play(true);}e.Handled=true;}
 void ExportClick(object sender,RoutedEventArgs e){
  var picker=new Microsoft.Win32.SaveFileDialog{Title="Export all saved accounts",Filter="Text file (*.txt)|*.txt",DefaultExt=".txt",AddExtension=true,OverwritePrompt=true,FileName="Vault-accounts.txt"};if(picker.ShowDialog(this)!=true)return;
  try{AccountExport.Save(entries,picker.FileName);Status.Text="ALL ACCOUNTS EXPORTED";}catch{VaultPrompt.Show(this,"Could not export","Choose a writable folder and try again.",false);}
 }
 Entry? PrepareQrImport(Entry imported){
  var duplicate=entries.FirstOrDefault(x=>string.Equals(x.Name,imported.Name,StringComparison.OrdinalIgnoreCase));if(duplicate==null)return imported;
  var owner=Application.Current.Windows.OfType<Window>().FirstOrDefault(w=>w.IsActive)??this;int choice=QrImport.DuplicateChoice(owner,imported.Name);if(choice==0)return null;if(choice==2)imported.Id=duplicate.Id;return imported;
 }
 void Edit(Entry? original){new EntryWindow(this,original,record=>{var next=entries.Where(x=>x.Id!=record.Id).ToList();next.Add(record);return Save(next);},prepareImport:PrepareQrImport,importMany:ImportMany).Show();}
}


