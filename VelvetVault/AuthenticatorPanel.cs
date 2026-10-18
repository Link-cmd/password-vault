using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
namespace VelvetVault;
public sealed class AuthenticatorPanel : Grid {
 readonly TextBlock name=new(){FontSize=12,TextAlignment=TextAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis,Margin=new Thickness(0,12,0,5)};
 readonly TextBlock code=new(){Text="--- ---",FontFamily=new FontFamily("Consolas"),FontSize=32,TextAlignment=TextAlignment.Center,Margin=new Thickness(0,5,0,5)};
 readonly TextBlock countdown=new(){FontFamily=new FontFamily("Consolas"),FontSize=9,TextAlignment=TextAlignment.Center,Margin=new Thickness(0,7,0,10)};
 readonly Border progress=new(){Height=3,HorizontalAlignment=HorizontalAlignment.Left};readonly Border track=new(){Height=3,Margin=new Thickness(8,0,8,0)};
 readonly Button copy,previous,next,remove;
 readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(250)};
 List<TotpAccount> accounts=new();Func<List<TotpAccount>,bool>? save;Action<string>? copyCode;int selected;
 public AuthenticatorPanel(){
  var body=new StackPanel{VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,8,0,8)};body.Children.Add(new TextBlock{Text="2FA",FontFamily=new FontFamily(new Uri("pack://application:,,,/VelvetVault;component/"),"./Fonts/#Saira Stencil"),FontSize=37,TextAlignment=TextAlignment.Center});body.Children.Add(new TextBlock{Text="AUTHENTICATOR",FontFamily=new FontFamily("Consolas"),FontSize=9,TextAlignment=TextAlignment.Center});body.Children.Add(name);body.Children.Add(code);track.Child=progress;body.Children.Add(track);body.Children.Add(countdown);
  var paging=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};previous=Button("‹",()=>{selected=(selected-1+accounts.Count)%accounts.Count;Update();});copy=Button("COPY CODE",()=>{if(accounts.Count>0)copyCode?.Invoke(Totp.Code(accounts[selected],DateTimeOffset.UtcNow.ToUnixTimeSeconds()));});next=Button("›",()=>{selected=(selected+1)%accounts.Count;Update();});paging.Children.Add(previous);paging.Children.Add(copy);paging.Children.Add(next);body.Children.Add(paging);
  var tools=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,9,0,0)};tools.Children.Add(Button("+ ADD 2FA",Add));remove=Button("REMOVE",Remove);tools.Children.Add(remove);body.Children.Add(tools);Children.Add(body);
  timer.Tick+=(_,_)=>{if(Window.GetWindow(this)?.WindowState!=WindowState.Minimized)Update();};Loaded+=(_,_)=>{Update();timer.Start();};Unloaded+=(_,_)=>timer.Stop();
 }
 static Button Button(string text,Action action){var button=new Button{Content=text,FontSize=9,Padding=new Thickness(7,5,7,5),Margin=new Thickness(0,0,4,0)};button.Click+=(_,_)=>action();return button;}
 public void Configure(List<TotpAccount> values,Func<List<TotpAccount>,bool> commit,Action<string> clipboard){accounts=values;save=commit;copyCode=clipboard;selected=Math.Clamp(selected,0,Math.Max(0,accounts.Count-1));Update();}
 void Update(){
  name.Foreground=Palette.Ink;code.Foreground=Palette.Ink;countdown.Foreground=Palette.Muted;progress.Background=Palette.Ink;track.Background=Palette.Faint;copy.IsEnabled=remove.IsEnabled=accounts.Count>0;previous.IsEnabled=next.IsEnabled=accounts.Count>1;
  if(accounts.Count==0){name.Text="NO ACCOUNTS";code.Text="--- ---";countdown.Text="ADD A SETUP KEY";progress.Width=0;return;}
  var account=accounts[selected];try{long now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();string value=Totp.Code(account,now);name.Text=account.Name;code.Text=value.Insert(value.Length/2," ");int left=account.Period-(int)(now%account.Period);countdown.Text=$"{left:D2}s / {selected+1:D2} OF {accounts.Count:D2}";progress.Width=Math.Max(0,track.ActualWidth)*left/account.Period;}catch{name.Text=account.Name;code.Text="ERROR";countdown.Text="CHECK SETUP KEY";copy.IsEnabled=false;}
 }
 void Add(){var owner=Window.GetWindow(this);if(owner==null||save==null)return;new TotpSetup(owner,account=>{var values=accounts.ToList();values.Add(account);if(!save(values))return false;selected=values.Count-1;Update();return true;}).Show();}
 void Remove(){if(accounts.Count==0||save==null)return;var owner=Window.GetWindow(this);var account=accounts[selected];if(!TransferPreview.Show(owner,"Remove 2FA?","This removes the authenticator setup from Vault. It does not disable 2FA on the website.",new[]{new Entry{Name=account.Name}},"REMOVE 2FA"))return;save(accounts.Where(x=>x.Id!=account.Id).ToList());}
}
public sealed class TotpSetup {
 readonly Window window;
 public TotpSetup(Window owner,Func<TotpAccount,bool> save){
  window=VaultUI.Window(owner,"Vault",460,360);var root=VaultUI.Grid(new GridLength(8),new GridLength(1,GridUnitType.Star),new GridLength(48));root.Margin=new Thickness(14,2,14,6);root.Children.Add(new MotionRail{Height=3});var body=new StackPanel{Margin=new Thickness(17)};
  body.Children.Add(VaultUI.Label("GOOGLE AUTHENTICATOR / COMPATIBLE",8));body.Children.Add(new TextBlock{Text="ACCOUNT NAME",FontSize=9,Margin=new Thickness(0,14,0,6)});var name=new TextBox{FontSize=13,Padding=new Thickness(0),BorderThickness=new Thickness(0),Background=Brushes.Transparent};
  Border Field(Control input)=>new(){Child=FieldCaret.Wrap(input),BorderBrush=Palette.Ink,BorderThickness=new Thickness(1.2),CornerRadius=new CornerRadius(6),Padding=new Thickness(9,7,9,7)};
  body.Children.Add(Field(name));body.Children.Add(new TextBlock{Text="SETUP KEY / OTPAUTH LINK",FontSize=9,Margin=new Thickness(0,12,0,6)});var secret=new PasswordBox{FontSize=13,Padding=new Thickness(0),BorderThickness=new Thickness(0),Background=Brushes.Transparent};body.Children.Add(Field(secret));
  var error=new TextBlock{FontSize=10,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,0)};var qr=new Button{Content="+ QR IMAGE",FontSize=10,HorizontalAlignment=HorizontalAlignment.Left,Padding=new Thickness(9,6,9,6),Margin=new Thickness(0,10,0,0)};qr.Click+=(_,_)=>{var picker=new Microsoft.Win32.OpenFileDialog{Title="Choose authenticator setup QR",Filter="QR images|*.png;*.jpg;*.jpeg;*.bmp"};if(picker.ShowDialog(window)!=true)return;try{string payload=QrImport.ReadText(picker.FileName);var account=Totp.Parse(name.Text,payload);name.Text=account.Name;secret.Password=payload;error.Text="";}catch{error.Text="Choose a time-based authenticator setup QR image.";}};body.Children.Add(qr);body.Children.Add(error);
  var frame=new RetroFrame{Child=new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden}};Grid.SetRow(frame,1);root.Children.Add(frame);var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};var cancel=new Button{Content="CANCEL",FontSize=10,IsCancel=true};cancel.Click+=(_,_)=>window.Close();var add=new Button{Content="SAVE 2FA",FontSize=10,IsDefault=true,Background=Palette.Ink,Foreground=Palette.Paper,Margin=new Thickness(0)};add.Click+=(_,_)=>{TotpAccount account;try{account=Totp.Parse(name.Text,secret.Password);}catch{error.Text="Enter an account name and valid Base32 setup key or otpauth link.";return;}if(save(account))window.Close();else error.Text="Could not save. Your setup is still here.";};actions.Children.Add(cancel);actions.Children.Add(add);Grid.SetRow(actions,2);root.Children.Add(actions);VaultChrome.Apply(window,root,"ADD 2FA");
 }
 public void Show()=>window.ShowDialog();
}
