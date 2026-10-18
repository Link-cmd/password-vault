using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace VelvetVault;
public class InkFrame : Decorator {
 protected override void OnRender(DrawingContext dc){base.OnRender(dc);double w=ActualWidth,h=ActualHeight;if(w<30||h<30)return;var ink=Palette.Ink;var p=new Pen(ink,1.8);var points=new[]{new Point(1,17),new Point(17,1),new Point(w-19,1),new Point(w-1,19),new Point(w-1,h-17),new Point(w-17,h-1),new Point(18,h-1),new Point(1,h-18)};var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(points[0],false,true);c.PolyLineTo(points.Skip(1).ToArray(),true,false);}dc.DrawGeometry(null,p,g);var thin=new Pen(ink,1);dc.DrawLine(thin,new Point(20,6),new Point(w*.46,6));dc.DrawLine(thin,new Point(w*.7,6),new Point(w-23,6));dc.DrawLine(thin,new Point(6,23),new Point(6,h*.36));dc.DrawLine(thin,new Point(w-6,h*.65),new Point(w-6,h-23));dc.DrawLine(new Pen(ink,3),new Point(24,h-6),new Point(w*.27,h-6));dc.DrawLine(thin,new Point(w*.52,h-6),new Point(w-23,h-6));}
}
public abstract class MotionGlyph : FrameworkElement {
 public static readonly DependencyProperty PhaseProperty=DependencyProperty.Register(nameof(Phase),typeof(double),typeof(MotionGlyph),new FrameworkPropertyMetadata(0.0,FrameworkPropertyMetadataOptions.AffectsRender));
 public double Phase {get=>(double)GetValue(PhaseProperty);set=>SetValue(PhaseProperty,value);}
 protected virtual double CycleSeconds=>12;
 Window? host;bool running;
 protected MotionGlyph(){Loaded+=(_,_)=>{host=Window.GetWindow(this);if(host!=null)host.StateChanged+=HostChanged;UpdateMotion();};Unloaded+=(_,_)=>{if(host!=null)host.StateChanged-=HostChanged;host=null;Stop();};IsVisibleChanged+=(_,_)=>UpdateMotion();}
 void HostChanged(object? sender,EventArgs e)=>UpdateMotion();
 void Stop(){if(!running)return;BeginAnimation(PhaseProperty,null);running=false;}
 void UpdateMotion(){if(!IsLoaded||!IsVisible||host?.WindowState==WindowState.Minimized){Stop();return;}if(running)return;BeginAnimation(PhaseProperty,new DoubleAnimation(0,1,TimeSpan.FromSeconds(CycleSeconds)){RepeatBehavior=RepeatBehavior.Forever});running=true;}
}
public class VaultSeal : MotionGlyph {
 protected override double CycleSeconds=>12;
 protected override void OnRender(DrawingContext dc){double s=Math.Min(ActualWidth,ActualHeight),cx=ActualWidth/2,cy=ActualHeight/2;var ink=Palette.Ink;for(int i=0;i<7;i++){double r=s*.40-i*s*.026;dc.DrawEllipse(null,new Pen(ink,Palette.Night?(i%2==0?2:1):(i%2==0?1:.55)),new Point(cx,cy),r,r);}// The fixed rings remain legible while short arcs and orbital marks rotate.
for(int i=0;i<3;i++){
 double r=s*(.413-i*.061);double angle=Phase*360*(i%2==0?1:-1)+i*110;
 dc.PushTransform(new RotateTransform(angle,cx,cy));
 var arc=new StreamGeometry();using(var a=arc.Open()){double start=-Math.PI*.44,end=start+Math.PI*.65;a.BeginFigure(new Point(cx+Math.Cos(start)*r,cy+Math.Sin(start)*r),false,false);a.ArcTo(new Point(cx+Math.Cos(end)*r,cy+Math.Sin(end)*r),new Size(r,r),0,false,SweepDirection.Clockwise,true,false);}
 dc.DrawGeometry(null,new Pen(ink,Palette.Night?3.8:1.4),arc);dc.DrawEllipse(ink,null,new Point(cx+r,cy),Palette.Night?3.2:1.6,Palette.Night?3.2:1.6);dc.Pop();
}
double breath=.94+.06*Math.Sin(Phase*Math.PI*8);dc.DrawEllipse(Palette.Night?ink:Palette.Paper,Palette.Night?null:new Pen(ink,.8),new Point(cx,cy),s*.208*breath,s*.208*breath);var white=new Pen(Palette.Night?Palette.Paper:ink,1);dc.DrawLine(white,new Point(cx-8,cy),new Point(cx+8,cy));dc.DrawLine(white,new Point(cx,cy-8),new Point(cx,cy+8));var p=new Pen(ink,1);for(int i=0;i<4;i++){double a=Math.PI*i/2;dc.DrawLine(p,new Point(cx+Math.Cos(a)*s*.43,cy+Math.Sin(a)*s*.43),new Point(cx+Math.Cos(a)*s*.49,cy+Math.Sin(a)*s*.49));}foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1}){var q=new Point(cx+x*s*.43,cy+y*s*.43);dc.DrawLine(p,q,new Point(q.X-x*12,q.Y));dc.DrawLine(p,q,new Point(q.X,q.Y-y*12));dc.DrawEllipse(ink,null,q,1.5,1.5);}}
}
public class MotionRail : MotionGlyph {
 protected override double CycleSeconds=>5;
 protected override void OnRender(DrawingContext dc){double w=ActualWidth;if(w<30)return;var ink=Palette.Ink;var faint=Palette.Faint;double y=ActualHeight/2;dc.DrawLine(new Pen(faint,1),new Point(0,y),new Point(w,y));for(int i=0;i<=12;i++){double x=w*i/12;dc.DrawLine(new Pen(ink,1),new Point(x,y-2),new Point(x,y+2));}double travel=Phase*(w+50)-25;dc.PushClip(new RectangleGeometry(new Rect(0,0,w,ActualHeight)));dc.DrawRectangle(ink,null,new Rect(travel-17,y-1.5,34,3));dc.DrawRectangle(ink,null,new Rect(travel-26,y-1,4,2));dc.DrawRectangle(ink,null,new Rect(travel-34,y-1,2,2));dc.Pop();}
}

public class OrbitGlobe : MotionGlyph {
 protected override double CycleSeconds=>16;
 protected override void OnRender(DrawingContext dc){
  double w=ActualWidth,h=ActualHeight,cx=w/2,cy=h/2,r=Math.Min(w,h)*.36,turn=Phase*Math.PI*2;
  var white=new SolidColorBrush(Color.FromRgb(255,255,255));var dim=new SolidColorBrush(Color.FromArgb(65,255,255,255));
  dc.DrawEllipse(null,new Pen(dim,1),new Point(cx,cy),r*1.27,r*1.27);
  dc.PushTransform(new RotateTransform(-24,cx,cy));dc.DrawEllipse(null,new Pen(dim,1),new Point(cx,cy),r*1.47,r*.33);dc.Pop();
  var dots=new List<(double x,double y,double z)>();
  for(int lat=-6;lat<=6;lat++){double a=lat*Math.PI/14;for(int lon=0;lon<24;lon++){double b=lon*Math.PI/12+turn;double x=Math.Cos(a)*Math.Cos(b),z=Math.Cos(a)*Math.Sin(b),y=Math.Sin(a);double yy=y*Math.Cos(.3)-z*Math.Sin(.3),zz=y*Math.Sin(.3)+z*Math.Cos(.3);dots.Add((x,yy,zz));}}
  foreach(var p in dots.OrderBy(p=>p.z)){byte alpha=(byte)(45+(p.z+1)*100);double size=1.0+(p.z+1)*.9;dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(alpha,255,255,255)),null,new Point(cx+p.x*r,cy+p.y*r),size,size);}
  double angle=-turn*1.5;dc.DrawEllipse(white,null,new Point(cx+Math.Cos(angle)*r*1.27,cy+Math.Sin(angle)*r*1.27),3,3);
  var pen=new Pen(white,1);for(int i=0;i<4;i++){double a=i*Math.PI/2;dc.DrawLine(pen,new Point(cx+Math.Cos(a)*r*1.4,cy+Math.Sin(a)*r*1.4),new Point(cx+Math.Cos(a)*r*1.55,cy+Math.Sin(a)*r*1.55));}
 }
}
public class SignalBars : MotionGlyph {
 protected override double CycleSeconds=>4;
 protected override void OnRender(DrawingContext dc){var brush=new SolidColorBrush(Color.FromRgb(255,255,255));for(int i=0;i<33;i++){double v=.18+.82*Math.Abs(Math.Sin(i*.48+Phase*Math.PI*2)*Math.Cos(i*.18-Phase*Math.PI*2));double h=ActualHeight*v;double step=ActualWidth/33;dc.DrawRectangle(brush,null,new Rect(i*step,(ActualHeight-h)/2,Math.Max(1,step-2),h));}}
}

public class ActionGlyph : FrameworkElement {
 readonly string kind;
 public ActionGlyph(string kind){this.kind=kind;Width=16;Height=16;}
 protected override void OnRender(DrawingContext dc){dc.PushTransform(new ScaleTransform(ActualWidth/16,ActualHeight/16));var p=new Pen(Palette.Ink,1.15){StartLineCap=PenLineCap.Square,EndLineCap=PenLineCap.Square};void Line(double x,double y,double a,double b)=>dc.DrawLine(p,new Point(x,y),new Point(a,b));
  if(kind=="Copy user"){dc.DrawEllipse(null,p,new Point(6,4),2.5,2.5);dc.DrawGeometry(null,p,Geometry.Parse("M1,14 L1,12 C1,8 11,8 11,12 L11,14"));dc.DrawRectangle(null,p,new Rect(11,10,4,5));Line(9,8,13,8);}
  else if(kind=="Copy email"){dc.DrawRectangle(null,p,new Rect(1,3,14,10));Line(1,3,8,9);Line(8,9,15,3);}
  else if(kind=="Copy password"){dc.DrawEllipse(null,p,new Point(5,5),3.2,3.2);Line(7.3,7.3,14,14);Line(11,11,13,9);Line(13,13,15,11);}
  else if(kind=="View / edit"){dc.DrawGeometry(null,p,Geometry.Parse("M7,2 L2,2 L2,14 L14,14 L14,9 M7,8 L12,3 L15,6 L10,11 L6,12 Z M11,4 L14,7"));}
  else if(kind=="Delete"){Line(2,4,14,4);Line(6,2,10,2);dc.DrawGeometry(null,p,Geometry.Parse("M4,4 L5,14 L11,14 L12,4 M7,7 L7,12 M9,7 L9,12"));}
  else {dc.DrawRectangle(null,p,new Rect(3,1,10,14));Line(5,5,11,5);Line(5,8,11,8);Line(5,11,9,11);}dc.Pop();
 }
}
public class CircuitField : MotionGlyph {
 protected override double CycleSeconds=>8;
 protected override void OnRender(DrawingContext dc){double w=ActualWidth,h=ActualHeight;if(w<10||h<10)return;var dim=new SolidColorBrush(Color.FromArgb(30,255,255,255));var line=new Pen(new SolidColorBrush(Color.FromArgb(45,255,255,255)),.7);
  for(double y=9;y<h;y+=13)for(double x=9;x<w;x+=13)dc.DrawRectangle(dim,null,new Rect(x,y,.8,.8));
  dc.PushClip(new RectangleGeometry(new Rect(0,0,w,h)));double cx=w-25,cy=h-27;dc.DrawEllipse(null,line,new Point(cx,cy),21,21);dc.DrawEllipse(null,line,new Point(cx,cy),14,14);dc.DrawLine(line,new Point(cx-28,cy),new Point(cx+28,cy));dc.DrawLine(line,new Point(cx,cy-28),new Point(cx,cy+28));
    for(int i=0;i<9;i++){double x=7+i*5;dc.DrawLine(line,new Point(x,h-4),new Point(x+10,h-14));}
  var trace=new Pen(new SolidColorBrush(Color.FromArgb(85,255,255,255)),.7);
  for(int i=0;i<4;i++){double y=h-19-i*10;var path=new StreamGeometry();using(var c=path.Open()){c.BeginFigure(new Point(3,y),false,false);c.PolyLineTo(new[]{new Point(w*.30,y),new Point(w*.30+8,y-8),new Point(w*.64,y-8),new Point(w*.64+7,y-1)},true,false);}dc.DrawGeometry(null,trace,path);double x=(Phase*w*(i%2==0?1:-1)+w+i*29)%w;dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(120,255,255,255)),null,new Rect(x,y-1,2,2));}
  for(int i=0;i<5;i++){double x=w-37+i*6;dc.DrawLine(trace,new Point(x,5),new Point(x,10+(i%3)*3));}
  double a=Phase*Math.PI*2;dc.DrawEllipse(Brushes.Gray,null,new Point(cx+Math.Cos(a)*21,cy+Math.Sin(a)*21),1.5,1.5);
  var label=new FormattedText("+ ARCHIVE / LOCAL",System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Consolas"),6,new SolidColorBrush(Color.FromRgb(130,130,130)),VisualTreeHelper.GetDpi(this).PixelsPerDip);dc.DrawText(label,new Point(6,Math.Max(1,h-59)));
  dc.Pop();
 }
}
public class GlitchLabel : MotionGlyph {
 public static readonly DependencyProperty InkProperty=DependencyProperty.Register(nameof(Ink),typeof(Brush),typeof(GlitchLabel),new FrameworkPropertyMetadata(Brushes.White,FrameworkPropertyMetadataOptions.AffectsRender));
 public Brush Ink {get=>(Brush)GetValue(InkProperty);set=>SetValue(InkProperty,value);}
 protected override double CycleSeconds=>8;
 public double Scale {get;set;}=1;
 public GlitchLabel(){Height=11;ClipToBounds=true;System.Windows.Automation.AutomationProperties.SetName(this,"Personal credential / 個人用・認証情報");}
 public static string TextAt(double phase){double p=phase%1;bool japanese=p>=.5;string text=japanese?"個人用 / 認証情報":"PERSONAL / CREDENTIAL";double segment=p%.5;if(segment<.45)return text;char[] chars=text.ToCharArray();int frame=(int)(p*240);for(int i=0;i<chars.Length;i++)if((i+frame)%4==0)chars[i]="ハ_01/キ"[(i+frame)%6];return new string(chars);}
 protected override void OnRender(DrawingContext dc){if(ActualWidth<1)return;double p=Phase%1;bool glitch=p%.5>=.45;string label=TextAt(p);var typeface=new Typeface(new FontFamily(p>=.5||glitch?"Yu Gothic UI":"Consolas"),FontStyles.Normal,FontWeights.Normal,FontStretches.Normal);var text=new FormattedText(label,System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,typeface,(p>=.5?7:6.5)*Scale,Ink,VisualTreeHelper.GetDpi(this).PixelsPerDip){MaxTextWidth=Math.Max(1,ActualWidth),MaxTextHeight=Math.Max(11,ActualHeight),Trimming=TextTrimming.CharacterEllipsis};double offset=glitch?Math.Sin(p*1100)*2:0;dc.DrawText(text,new Point(offset,0));if(glitch){dc.PushClip(new RectangleGeometry(new Rect(0,4,ActualWidth,2)));dc.DrawText(text,new Point(-offset*2,0));dc.Pop();}}
}



