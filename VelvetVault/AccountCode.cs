using System.Text.Json;
using System.Text.Encodings.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QRCoder;
namespace VelvetVault;

public static class AccountCode {
 public static void SaveImage(Entry item,string path){using var data=Generate(item);using var png=new PngByteQRCode(data);System.IO.File.WriteAllBytes(path,png.GetGraphic(12));}
 public static string Payload(Entry item)=>JsonSerializer.Serialize(new {Name=item.Name,Username=item.Username,Email=item.Email,Password=item.Password,Website=item.Website,RecoveryDetails=item.Notes},new JsonSerializerOptions{Encoder=JavaScriptEncoder.UnsafeRelaxedJsonEscaping});
 public static QRCodeData Generate(Entry item){using var generator=new QRCodeGenerator();return generator.CreateQrCode(Payload(item),QRCodeGenerator.ECCLevel.M,forceUtf8:true);}
 public static void Show(MainWindow owner,Entry item){
  QRCodeData data;
  try{data=Generate(item);}catch(QRCoder.Exceptions.DataTooLongException){VaultPrompt.Show(owner,"Entry too large","This entry has more text than one QR code can hold. Shorten the recovery details to create a scannable code.",false);return;}
  var dialog=VaultUI.Window(owner,"Vault",450,550);
  var root=VaultUI.Grid(new GridLength(54),new GridLength(6),new GridLength(1,GridUnitType.Star),new GridLength(50));root.Margin=new Thickness(16,7,16,8);
  var header=new StackPanel{Margin=new Thickness(3,0,0,0)};header.Children.Add(VaultUI.Label("SCAN / SAVED ACCOUNT",8));header.Children.Add(new TextBlock{Text=item.Name.ToUpperInvariant(),FontSize=22,TextTrimming=TextTrimming.CharacterEllipsis,Margin=new Thickness(0,5,0,0)});root.Children.Add(header);
  var rail=new MotionRail{Height=3};Grid.SetRow(rail,1);root.Children.Add(rail);
  var frame=new RetroFrame{Child=new AccountQr(data){Margin=new Thickness(16)}};Grid.SetRow(frame,2);root.Children.Add(frame);
  var footer=new Grid();footer.Children.Add(new TextBlock{Text="USERNAME / EMAIL / KEY\nINCLUDES RECOVERY DETAILS",FontSize=8,VerticalAlignment=VerticalAlignment.Center});
  var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};
  var save=new Button{Content="SAVE IMAGE",FontSize=10,Padding=new Thickness(11,8,11,8),Margin=new Thickness(0,0,7,0)};
  save.Click+=(_,_)=>{var picker=new Microsoft.Win32.SaveFileDialog{Title="Save account QR image",Filter="PNG image (*.png)|*.png",DefaultExt=".png",AddExtension=true,OverwritePrompt=true,FileName="Vault-account-QR.png"};if(picker.ShowDialog(dialog)!=true)return;try{SaveImage(item,picker.FileName);save.Content="SAVED";}catch(Exception){VaultPrompt.Show(owner,"Could not save image","Choose a writable folder and try saving the image again.",false);}};
  var close=new Button{Content="CLOSE",IsCancel=true,FontSize=10,Padding=new Thickness(11,8,11,8),Margin=new Thickness(0)};close.Click+=(_,_)=>dialog.Close();actions.Children.Add(save);actions.Children.Add(close);footer.Children.Add(actions);Grid.SetRow(footer,3);root.Children.Add(footer);
  VaultChrome.Apply(dialog,root);try{dialog.ShowDialog();}finally{data.Dispose();}
 }
}
public sealed class AccountQr : FrameworkElement {
 readonly QRCodeData data;
 public AccountQr(QRCodeData data){this.data=data;SnapsToDevicePixels=true;}
 protected override void OnRender(DrawingContext dc){
  int count=data.ModuleMatrix.Count;double dpi=VisualTreeHelper.GetDpi(this).DpiScaleX;double cell=Math.Floor(Math.Min(ActualWidth,ActualHeight)*dpi/count)/dpi;if(cell<=0)return;
  double size=cell*count,x=Math.Round((ActualWidth-size)/2*dpi)/dpi,y=Math.Round((ActualHeight-size)/2*dpi)/dpi;
  RenderOptions.SetEdgeMode(this,EdgeMode.Aliased);dc.DrawRectangle(Brushes.White,null,new Rect(x,y,size,size));
  for(int row=0;row<count;row++)for(int col=0;col<count;col++)if(data.ModuleMatrix[row][col])dc.DrawRectangle(Brushes.Black,null,new Rect(x+col*cell,y+row*cell,cell,cell));
 }
}
