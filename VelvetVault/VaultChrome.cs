using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Media.Animation;
namespace VelvetVault;
public static class VaultChrome {
 static VaultChrome(){
  EventManager.RegisterClassHandler(typeof(FrameworkElement),ToolTipService.ToolTipOpeningEvent,new ToolTipEventHandler((_,e)=>e.Handled=true),true);
  EventManager.RegisterClassHandler(typeof(FrameworkElement),ContextMenuService.ContextMenuOpeningEvent,new ContextMenuEventHandler((_,e)=>e.Handled=true),true);
 }
 public static void Apply(Window window,UIElement content,string? brandTitle=null){
  SoundFx.Attach(window);bool main=window is MainWindow;double caption=main?67:38;
  window.Content=null;window.WindowStyle=WindowStyle.None;window.Icon=BitmapFrame.Create(new Uri("pack://application:,,,/VelvetVault;component/Vault.ico"));
  foreach(var key in new[]{"Ink","Paper","Muted","Faint"})if(!window.Resources.Contains(key))window.Resources[key]=key switch{"Ink"=>Palette.Ink,"Paper"=>Palette.Paper,"Muted"=>Palette.Muted,_=>Palette.Faint};
  WindowChrome.SetWindowChrome(window,new WindowChrome{CaptionHeight=caption,ResizeBorderThickness=new Thickness(6),GlassFrameThickness=new Thickness(0),CornerRadius=new CornerRadius(0),UseAeroCaptionButtons=false});
  var root=new Grid();root.RowDefinitions.Add(new RowDefinition{Height=new GridLength(caption)});root.RowDefinitions.Add(new RowDefinition());
  var bar=new Grid();bar.SetResourceReference(Panel.BackgroundProperty,"Paper");bar.ColumnDefinitions.Add(new ColumnDefinition());bar.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
  var brand=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(main?24:16,0,0,0),VerticalAlignment=VerticalAlignment.Center};brand.Children.Add(new TextBlock{Text=brandTitle??"VAULT",FontFamily=new FontFamily(new Uri("pack://application:,,,/VelvetVault;component/"),"./Fonts/#Saira Stencil"),FontSize=main?47:23,FontWeight=FontWeights.ExtraBold,VerticalAlignment=VerticalAlignment.Center});
  if(main){brand.Children.Add(new TextBlock{Text="PASSWORD ARCHIVE ®\nUNIT 6-B // VER. 02.00",FontSize=8,Margin=new Thickness(24,0,0,0),VerticalAlignment=VerticalAlignment.Center});var badge=new Border{BorderThickness=new Thickness(1),Padding=new Thickness(8,4,8,4),Margin=new Thickness(26,0,0,0),VerticalAlignment=VerticalAlignment.Center,Child=new TextBlock{Text="LOCAL EDITION",FontSize=10}};badge.SetResourceReference(Border.BorderBrushProperty,"Ink");brand.Children.Add(badge);}bar.Children.Add(brand);
  var controls=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,10,0)};Grid.SetColumn(controls,1);
  if(window is MainWindow vault){var toggle=new Button{Content=Palette.Night?"☀ DAY":"☾ NIGHT",FontSize=10,Padding=new Thickness(10,7,10,7),Margin=new Thickness(0,0,16,0),ToolTip="Switch day / night mode"};toggle.SetResourceReference(Control.ForegroundProperty,"Ink");toggle.SetResourceReference(Control.BorderBrushProperty,"Ink");WindowChrome.SetIsHitTestVisibleInChrome(toggle,true);toggle.Click+=(_,_)=>vault.ToggleTheme();vault.ThemeButton=toggle;controls.Children.Add(toggle);}
  void Button(string label,string tip,Action action){var b=new Button{Content=label,ToolTip=tip,Width=main?42:30,Height=30,Padding=new Thickness(0),Margin=new Thickness(0,0,6,0),FontSize=main?24:17,Background=Brushes.Transparent,BorderThickness=new Thickness(0)};if(label=="□"){var square=new System.Windows.Shapes.Rectangle{Width=main?10:8,Height=main?10:8,StrokeThickness=1,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};square.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty,"Ink");b.Content=square;}b.SetResourceReference(Control.ForegroundProperty,"Ink");WindowChrome.SetIsHitTestVisibleInChrome(b,true);b.Click+=(_,_)=>action();controls.Children.Add(b);}
  Button("−","Minimize",()=>window.WindowState=WindowState.Minimized);if(window.ResizeMode!=ResizeMode.NoResize)Button("□",main&&BuildProfile.HalfSize?"Full size / half size":"Maximize / restore",()=>{if(main&&BuildProfile.HalfSize)AspectSizer.Toggle(window);else window.WindowState=window.WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;});Button("×","Close",()=>window.Close());bar.Children.Add(controls);
  root.Children.Add(bar);Grid.SetRow(content,1);root.Children.Add(content);window.Loaded+=(_,_)=>content.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(400)){EasingFunction=new QuadraticEase{EasingMode=EasingMode.EaseOut}});var outer=new Border{BorderThickness=new Thickness(1.5),CornerRadius=new CornerRadius(12),Child=main&&BuildProfile.HalfSize?Scaled(root,window):root};outer.SetResourceReference(Border.BorderBrushProperty,"Ink");window.Content=outer;
 }
 static UIElement Scaled(Grid root,Window window){root.Width=1057;root.Height=657;var view=new Viewbox{Child=root,Stretch=Stretch.Fill,HorizontalAlignment=HorizontalAlignment.Stretch,VerticalAlignment=VerticalAlignment.Stretch};window.SizeChanged+=(_,_)=>{double scale=Math.Max(1,window.ActualHeight-3)/657;var chrome=WindowChrome.GetWindowChrome(window);if(chrome!=null)chrome.CaptionHeight=67*scale;};return view;}}



