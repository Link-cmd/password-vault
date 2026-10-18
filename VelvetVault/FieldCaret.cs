using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
namespace VelvetVault;
public static class FieldCaret {
 public static Grid Wrap(Control input){var grid=new Grid();grid.Children.Add(input);FrameworkElement caret;
  if(input is TextBox text){text.CaretBrush=Brushes.Transparent;caret=new BlinkCaret{Target=text};}
  else if(input is PasswordBox password){password.CaretBrush=Brushes.Transparent;caret=new PasswordBlockCaret(password);}
  else return grid;
  caret.IsHitTestVisible=false;caret.ClipToBounds=true;caret.SetBinding(UIElement.VisibilityProperty,new Binding(nameof(UIElement.IsKeyboardFocusWithin)){Source=input,Converter=new BooleanToVisibilityConverter()});grid.Children.Add(caret);return grid;
 }
}
public sealed class PasswordBlockCaret : MotionGlyph {
 [StructLayout(LayoutKind.Sequential)] struct NativePoint {public int X,Y;}
 [DllImport("user32.dll")] static extern bool GetCaretPos(out NativePoint point);
 [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr hwnd,ref NativePoint point);
 public PasswordBox Target {get;}
 public PasswordBlockCaret(PasswordBox target){Target=target;}
 protected override double CycleSeconds=>1.15;
 public bool TryGetPosition(out Point position){position=default;if(!Target.IsKeyboardFocusWithin||PresentationSource.FromVisual(Target) is not HwndSource source||!GetCaretPos(out var native)||!ClientToScreen(source.Handle,ref native))return false;position=PointFromScreen(new Point(native.X,native.Y));return position.X>=-1&&position.X<=ActualWidth+1&&position.Y>=-2&&position.Y<=ActualHeight+1;}
 protected override void OnRender(DrawingContext dc){if(!BlinkCaret.LitAt(Phase)||!TryGetPosition(out var p))return;dc.PushOpacity(.68);dc.DrawRectangle(Palette.Ink,null,new Rect(Math.Max(0,p.X),Math.Max(0,p.Y),Math.Max(7,Target.FontSize*.53),Math.Max(1,Target.ActualHeight)));dc.Pop();}
}
