using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace VelvetVault;
public class RetroFrame : Decorator {
 public bool TopNotch {get;set;}=true;
 protected override void OnRender(DrawingContext dc){double w=ActualWidth,h=ActualHeight;if(w<50||h<45)return;var g=new StreamGeometry();double a=w*.38,b=w*.59,notch=h*.64;using(var c=g.Open()){
 c.BeginFigure(new Point(17,1),true,true);if(TopNotch){c.LineTo(new Point(a-10,1),true,false);c.LineTo(new Point(a,10),true,false);c.LineTo(new Point(b,10),true,false);c.LineTo(new Point(b+10,1),true,false);}c.LineTo(new Point(w-17,1),true,false);c.ArcTo(new Point(w-1,17),new Size(16,16),0,false,SweepDirection.Clockwise,true,false);c.LineTo(new Point(w-1,notch-8),true,false);c.ArcTo(new Point(w-1,notch+8),new Size(8,8),0,false,SweepDirection.Counterclockwise,true,false);c.LineTo(new Point(w-1,h-17),true,false);c.ArcTo(new Point(w-17,h-1),new Size(16,16),0,false,SweepDirection.Clockwise,true,false);c.LineTo(new Point(17,h-1),true,false);c.ArcTo(new Point(1,h-17),new Size(16,16),0,false,SweepDirection.Clockwise,true,false);c.LineTo(new Point(1,notch+8),true,false);c.ArcTo(new Point(1,notch-8),new Size(8,8),0,false,SweepDirection.Counterclockwise,true,false);c.LineTo(new Point(1,17),true,false);c.ArcTo(new Point(17,1),new Size(16,16),0,false,SweepDirection.Clockwise,true,false);}
 dc.DrawGeometry(Palette.Paper,new Pen(Palette.Ink,1.6),g);
 }
}
public class RetroGlobe : MotionGlyph {
 protected override double CycleSeconds=>24;
 protected override void OnRender(DrawingContext dc){double w=ActualWidth,h=ActualHeight,r=Math.Min(w,h)*.31,cx=w/2,cy=h/2;var center=new Point(cx,cy);var ink=Palette.Ink;dc.DrawEllipse(null,new Pen(ink,1),center,r*1.32,r*1.32);var dashed=new Pen(Palette.Alpha(155),.8){DashStyle=new DashStyle(new double[]{2,4},0)};dc.DrawEllipse(null,dashed,center,r*1.21,r*1.21);
 for(int i=0;i<60;i++){double a=i*Math.PI/30;double start=r*1.32,end=r*(i%5==0?1.43:1.37);dc.DrawLine(new Pen(ink,i%5==0?1.2:.65),new Point(cx+Math.Cos(a)*start,cy+Math.Sin(a)*start),new Point(cx+Math.Cos(a)*end,cy+Math.Sin(a)*end));}
 dc.DrawEllipse(null,new Pen(ink,2.4),center,r,r);
 for(int i=-3;i<=3;i++){double a=i*Math.PI/8,y=Math.Sin(a)*r,rr=Math.Cos(a)*r;dc.DrawEllipse(null,new Pen(ink,1.7),new Point(cx,cy+y),rr,rr*.075);}
 for(int j=0;j<6;j++){double a=j*Math.PI/6+Phase*Math.PI*2;var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(new Point(cx,cy-r),false,false);for(int k=1;k<=72;k++){double t=k*Math.PI/72;c.LineTo(new Point(cx+Math.Sin(t)*Math.Cos(a)*r,cy-Math.Cos(t)*r),true,false);}}dc.DrawGeometry(null,new Pen(ink,1.6),g);}
 dc.PushTransform(new RotateTransform(-23,cx,cy));dc.DrawEllipse(null,new Pen(ink,1.1),center,r*1.49,r*.31);double orbit=Phase*Math.PI*2;dc.DrawEllipse(ink,null,new Point(cx+Math.Cos(orbit)*r*1.49,cy+Math.Sin(orbit)*r*.31),3.5,3.5);dc.Pop();
 for(int i=0;i<4;i++){double a=i*Math.PI/2;var text=new FormattedText(new[]{"E","S","W","N"}[i],System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Consolas"),8,ink,VisualTreeHelper.GetDpi(this).PixelsPerDip);dc.DrawText(text,new Point(cx+Math.Cos(a)*r*1.55-3,cy+Math.Sin(a)*r*1.55-5));}
 }
}
public class StripeMeter : MotionGlyph {
 protected override double CycleSeconds=>4;
 protected override void OnRender(DrawingContext dc){dc.PushClip(new RectangleGeometry(new Rect(0,0,ActualWidth,ActualHeight)));double offset=Phase*14;for(double x=-30+offset;x<ActualWidth+20;x+=14){var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(new Point(x,ActualHeight),true,true);c.PolyLineTo(new[]{new Point(x+ActualHeight*.5,0),new Point(x+ActualHeight*.5+7,0),new Point(x+7,ActualHeight)},true,false);}dc.DrawGeometry(Palette.Ink,null,g);}dc.Pop();}
}
public class ArchiveCore : MotionGlyph {
 protected override double CycleSeconds=>9;
 protected override void OnRender(DrawingContext dc){
  double cx=ActualWidth/2,cy=ActualHeight/2-3;var p=new Pen(Palette.Ink,.9);
  dc.DrawLine(new Pen(Palette.Faint,.7),new Point(cx,0),new Point(cx,ActualHeight-9));
  dc.DrawLine(new Pen(Palette.Faint,.7),new Point(0,cy),new Point(ActualWidth,cy));
  dc.DrawEllipse(null,p,new Point(cx,cy),12,12);
  dc.PushTransform(new RotateTransform(Phase*360,cx,cy));
  dc.DrawRectangle(null,p,new Rect(cx-7,cy-7,14,14));
  dc.DrawEllipse(Palette.Ink,null,new Point(cx,cy-12),1.8,1.8);dc.Pop();
  dc.DrawEllipse(Palette.Ink,null,new Point(cx,cy),2+Math.Sin(Phase*Math.PI*4)*.6,2+Math.Sin(Phase*Math.PI*4)*.6);
  var label=new FormattedText("CORE",System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Consolas"),6,Palette.Muted,VisualTreeHelper.GetDpi(this).PixelsPerDip);dc.DrawText(label,new Point(cx-label.Width/2,ActualHeight-7));
 }
}
public class TypewriterReadout : MotionGlyph {
 public static readonly DependencyProperty TextProperty=DependencyProperty.Register(nameof(Text),typeof(string),typeof(TypewriterReadout),new FrameworkPropertyMetadata("",FrameworkPropertyMetadataOptions.AffectsRender));
 public string Text {get=>(string)GetValue(TextProperty);set=>SetValue(TextProperty,value);}
 protected override double CycleSeconds=>12;
 public static int VisibleCharacters(string text,double phase)=>Math.Clamp((int)(text.Length*Math.Min(1,phase/.30)),0,text.Length);
 protected override void OnRender(DrawingContext dc){
  if(ActualWidth<=0||ActualHeight<=0)return;
  var text=Text;int count=VisibleCharacters(text,Phase);var lines=text.Split('\n');double size=Math.Min(12,ActualWidth/(Math.Max(1,lines.Max(l=>l.Length))*.61));double lineHeight=ActualHeight/Math.Max(1,lines.Length);int offset=0;
  dc.PushClip(new RectangleGeometry(new Rect(0,0,ActualWidth,ActualHeight)));
  for(int i=0;i<lines.Length;i++){int length=Math.Clamp(count-offset,0,lines[i].Length);var formatted=new FormattedText(lines[i][..length],System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Consolas"),size,Palette.Ink,VisualTreeHelper.GetDpi(this).PixelsPerDip);double y=i*lineHeight+(lineHeight-size*1.2)/2;dc.DrawText(formatted,new Point(0,y));if(count>=offset&&count<=offset+lines[i].Length&&(Phase<.30||BlinkCaret.LitAt(Phase*12)))dc.DrawRectangle(Palette.Ink,null,new Rect(formatted.WidthIncludingTrailingWhitespace+2,y+1,size*.57,size));offset+=lines[i].Length+1;}
  dc.Pop();
 }
}
public class DotCounter : FrameworkElement {
 public int Number {get;set;}=1;
 protected override void OnRender(DrawingContext dc){string[] patterns={"01110100011000110001100011000101110","00100011000010000100001000010001110","01110100010000100010001000100011111","11110000010000101110000010000111110","00010001100101010010111110001000010","11111100001000011110000010000111110","01110100001000011110100011000101110","11111000010001000100010000100001000","01110100011000101110100011000101110","01110100011000101111000010000101110"};string pattern=patterns[Math.Abs(Number)%10];double step=Math.Min(ActualWidth/5,ActualHeight/7),cx=(ActualWidth-step*5)/2,cy=(ActualHeight-step*7)/2;for(int y=0;y<7;y++)for(int x=0;x<5;x++)if(pattern[y*5+x]=='1')dc.DrawEllipse(Palette.Ink,null,new Point(cx+(x+.5)*step,cy+(y+.5)*step),step*.32,step*.32);}
}
public class BlinkCaret : MotionGlyph {
 public static readonly DependencyProperty TargetProperty=DependencyProperty.Register(nameof(Target),typeof(TextBox),typeof(BlinkCaret),new PropertyMetadata(null,TargetChanged));
 public TextBox? Target {get=>(TextBox?)GetValue(TargetProperty);set=>SetValue(TargetProperty,value);}
 static void TargetChanged(DependencyObject d,DependencyPropertyChangedEventArgs e){var caret=(BlinkCaret)d;if(e.OldValue is TextBox old){old.SelectionChanged-=caret.Moved;old.TextChanged-=caret.Moved;old.GotKeyboardFocus-=caret.Moved;old.LostKeyboardFocus-=caret.Moved;old.SizeChanged-=caret.Moved;}if(e.NewValue is TextBox next){next.SelectionChanged+=caret.Moved;next.TextChanged+=caret.Moved;next.GotKeyboardFocus+=caret.Moved;next.LostKeyboardFocus+=caret.Moved;next.SizeChanged+=caret.Moved;}caret.InvalidateVisual();}
 void Moved(object sender,RoutedEventArgs e)=>InvalidateVisual();
 protected override double CycleSeconds=>1.15;
 public static bool LitAt(double phase)=>phase%1<.55;
 protected override void OnRender(DrawingContext dc){var t=Target;if(t==null||!t.IsKeyboardFocusWithin||t.SelectionLength>0||!LitAt(Phase))return;var rect=t.GetRectFromCharacterIndex(t.CaretIndex,false);if(rect.IsEmpty)return;var point=t.TranslatePoint(rect.TopLeft,this);double width=Math.Max(7,t.FontSize*.53);if(t.CaretIndex<t.Text.Length){var glyph=new FormattedText(t.Text.Substring(t.CaretIndex,1),System.Globalization.CultureInfo.CurrentUICulture,t.FlowDirection,new Typeface(t.FontFamily,t.FontStyle,t.FontWeight,t.FontStretch),t.FontSize,Palette.Ink,VisualTreeHelper.GetDpi(this).PixelsPerDip);width=Math.Max(7,glyph.WidthIncludingTrailingWhitespace);}dc.PushOpacity(.68);dc.DrawRectangle(Palette.Ink,null,new Rect(point.X,point.Y,width,Math.Max(1,rect.Height)));dc.Pop();}
}


