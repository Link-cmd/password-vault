using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
namespace VelvetVault;
public static class AspectSizer {
 const double Ratio=1060.0/660;
 [StructLayout(LayoutKind.Sequential)] struct Bounds {public int Left,Top,Right,Bottom;}
 public static (double Width,double Height) Fit(double width,double height,bool heightDriven){if(heightDriven){height=Math.Clamp(height,330,660);return(height*Ratio,height);}width=Math.Clamp(width,530,1060);return(width,width/Ratio);}
 public static void Attach(Window window){window.SourceInitialized+=(_,_)=>{var source=HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);source?.AddHook((IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam,ref bool handled)=>{
  if(message!=0x0214)return IntPtr.Zero;
  var r=Marshal.PtrToStructure<Bounds>(lParam);var dpi=VisualTreeHelper.GetDpi(window);int edge=wParam.ToInt32();double width=(r.Right-r.Left)/dpi.DpiScaleX,height=(r.Bottom-r.Top)/dpi.DpiScaleY;
  bool heightDriven=edge is 3 or 6 || (edge is 4 or 5 or 7 or 8 && Math.Abs(height-window.ActualHeight)>Math.Abs(width-window.ActualWidth));
  var size=Fit(width,height,heightDriven);int pw=(int)Math.Round(size.Width*dpi.DpiScaleX),ph=(int)Math.Round(size.Height*dpi.DpiScaleY);
  if(edge is 1 or 4 or 7)r.Left=r.Right-pw;else r.Right=r.Left+pw;
  if(edge is 3 or 4 or 5)r.Top=r.Bottom-ph;else r.Bottom=r.Top+ph;
  Marshal.StructureToPtr(r,lParam,false);handled=true;return new IntPtr(1);
 });};}
 public static void Toggle(Window window){window.WindowState=WindowState.Normal;bool large=window.Width>1050;window.Width=large?530:1060;window.Height=large?330:660;}
}
