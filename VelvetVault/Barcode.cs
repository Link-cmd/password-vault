using System.Text;
using System.Windows;
using System.Windows.Media;
namespace VelvetVault;
public class Barcode : FrameworkElement {
 public static readonly DependencyProperty DataProperty=DependencyProperty.Register(nameof(Data),typeof(string),typeof(Barcode),new FrameworkPropertyMetadata("VAULT",FrameworkPropertyMetadataOptions.AffectsRender));
 public bool EdgeAligned {get;set;}
 public string Data {get=>(string)GetValue(DataProperty);set=>SetValue(DataProperty,value);}
 public static readonly DependencyProperty ScanModeProperty=DependencyProperty.Register(nameof(ScanMode),typeof(bool),typeof(Barcode),new FrameworkPropertyMetadata(false,FrameworkPropertyMetadataOptions.AffectsRender)); public bool ScanMode {get=>(bool)GetValue(ScanModeProperty);set=>SetValue(ScanModeProperty,value);}
 static readonly string[] Patterns={"212222","222122","222221","121223","121322","131222","122213","122312","132212","221213","221312","231212","112232","122132","122231","113222","123122","123221","223211","221132","221231","213212","223112","312131","311222","321122","321221","312212","322112","322211","212123","212321","232121","111323","131123","131321","112313","132113","132311","211313","231113","231311","112133","112331","132131","113123","113321","133121","313121","211331","231131","213113","213311","213131","311123","311321","331121","312113","312311","332111","314111","221411","431111","111224","111422","121124","121421","141122","141221","112214","112412","122114","122411","142112","142211","241211","221114","413111","241112","134111","111242","121142","121241","114212","124112","124211","411212","421112","421211","212141","214121","412121","111143","111341","131141","114113","114311","411113","411311","113141","114131","311141","411131","211412","211214","211232","2331112"};
 public static string EncodedText(string value)=>value.All(c=>c>=32&&c<=126)?value:"UTF8:"+Convert.ToHexString(Encoding.UTF8.GetBytes(value));
 public static int[] Symbols(string text){var value=EncodedText(text);var result=new List<int>{104};int sum=104;for(int i=0;i<value.Length;i++){int code=value[i]-32;result.Add(code);sum+=code*(i+1);}result.Add(sum%103);result.Add(106);return result.ToArray();}
 public static int ModuleCount(string text)=>20+Symbols(text).Sum(code=>Patterns[code].Sum(c=>c-'0'));
 protected override void OnRender(DrawingContext dc){var value=Data??"";bool aligned=EdgeAligned&&!ScanMode;double total=ModuleCount(value)-(aligned?20:0),scale=ActualWidth/total,x=aligned?0:10*scale;if(ScanMode)dc.DrawRectangle(Brushes.White,null,new Rect(0,0,ActualWidth,ActualHeight));foreach(int symbol in Symbols(value)){bool bar=true;foreach(char c in Patterns[symbol]){double width=(c-'0')*scale;if(bar)dc.DrawRectangle(ScanMode?Brushes.Black:Palette.Ink,null,new Rect(x,2,width,Math.Max(0,ActualHeight-4)));x+=width;bar=!bar;}}}
}


