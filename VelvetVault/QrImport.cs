using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using FormatException = System.FormatException;
namespace VelvetVault;
public static class QrImport {
 public static Entry Parse(string payload){
  using var doc=JsonDocument.Parse(payload);var root=doc.RootElement;
  if(root.ValueKind!=JsonValueKind.Object)throw new FormatException();
  string Read(string name){if(!root.TryGetProperty(name,out var value)||value.ValueKind!=JsonValueKind.String)throw new FormatException();return value.GetString()!;}
  var entry=new Entry{Name=Read("Name"),Username=Read("Username"),Email=Read("Email"),Password=Read("Password"),Website=Read("Website"),Notes=Read("RecoveryDetails")};
  if(string.IsNullOrWhiteSpace(entry.Name)||(entry.Password.Length==0&&string.IsNullOrWhiteSpace(entry.Notes)))throw new FormatException();return entry;
 }
 public static Entry ReadImage(string path)=>Parse(ReadText(path));
 public static string ReadText(string path){
  if(new FileInfo(path).Length>32*1024*1024)throw new FormatException();
  var image=new BitmapImage();using(var stream=File.OpenRead(path)){image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.DecodePixelWidth=2048;image.StreamSource=stream;image.EndInit();image.Freeze();}
  if((long)image.PixelWidth*image.PixelHeight>16_000_000)throw new FormatException();
  var pixels=new FormatConvertedBitmap(image,PixelFormats.Bgra32,null,0);int stride=pixels.PixelWidth*4;var bytes=new byte[stride*pixels.PixelHeight];pixels.CopyPixels(bytes,stride,0);
  var reader=new BarcodeReaderGeneric{AutoRotate=true,Options=new ZXing.Common.DecodingOptions{TryHarder=true,PossibleFormats=new List<BarcodeFormat>{BarcodeFormat.QR_CODE}}};
  var result=reader.Decode(bytes,pixels.PixelWidth,pixels.PixelHeight,RGBLuminanceSource.BitmapFormat.BGRA32);if(result==null)throw new FormatException();return result.Text;
 }
 public static int DuplicateChoice(Window owner,string name){
  int result=0;var dialog=VaultUI.Window(owner,"Vault",450,265);dialog.ResizeMode=ResizeMode.NoResize;
  var root=VaultUI.Grid(new GridLength(8),new GridLength(1,GridUnitType.Star),new GridLength(48));root.Margin=new Thickness(14,2,14,5);root.Children.Add(new MotionRail{Height=3});
  var text=new StackPanel{Margin=new Thickness(16)};text.Children.Add(VaultUI.Label("IMPORT / MATCHING ACCOUNT",8));text.Children.Add(new TextBlock{Text="ACCOUNT ALREADY EXISTS",FontSize=20,Margin=new Thickness(0,8,0,8)});text.Children.Add(new TextBlock{Text=$"An account named {name} already exists. Choose how to import it, then review the details.",TextWrapping=TextWrapping.Wrap,FontSize=12});var frame=new RetroFrame{Child=text};Grid.SetRow(frame,1);root.Children.Add(frame);
  var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};
  void Button(string label,int choice,bool cancel=false){var b=new Button{Content=label,FontSize=10,Padding=new Thickness(10,8,10,8),IsCancel=cancel};b.Click+=(_,_)=>{result=choice;dialog.Close();};actions.Children.Add(b);}
  Button("CANCEL",0,true);Button("ADD NEW",1);Button("UPDATE EXISTING",2);Grid.SetRow(actions,2);root.Children.Add(actions);VaultChrome.Apply(dialog,root);dialog.ShowDialog();return result;
 }
}
