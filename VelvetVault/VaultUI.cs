using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;
namespace VelvetVault;
public static class VaultUI {
 public static Window Window(Window? owner,string title,double width,double height){var w=new Window{Title=title,Width=width,Height=height,MinWidth=Math.Min(420,width),MinHeight=Math.Min(260,height),Background=Palette.Paper,Foreground=Palette.Ink,FontFamily=new FontFamily("Bahnschrift"),WindowStartupLocation=owner?.IsVisible==true?WindowStartupLocation.CenterOwner:WindowStartupLocation.CenterScreen,ShowInTaskbar=false};if(owner?.IsVisible==true)w.Owner=owner;
  foreach(var type in new[]{typeof(Button),typeof(TextBox),typeof(PasswordBox),typeof(ComboBox)})if(owner?.TryFindResource(type) is Style style)w.Resources[type]=style;
  w.Resources["Ink"]=Palette.Ink;w.Resources["Paper"]=Palette.Paper;w.Resources["Muted"]=Palette.Muted;w.Resources["Faint"]=Palette.Faint;
  if(!w.Resources.Contains(typeof(Button)))w.Resources[typeof(Button)]=(Style)XamlReader.Parse("<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'><Setter Property='Foreground' Value='{DynamicResource Ink}'/><Setter Property='Background' Value='{DynamicResource Paper}'/><Setter Property='BorderBrush' Value='{DynamicResource Ink}'/><Setter Property='BorderThickness' Value='1.5'/><Setter Property='Padding' Value='16,9'/><Setter Property='Cursor' Value='Hand'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'><Border x:Name='Face' Opacity='0.72' CornerRadius='8' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><EventTrigger RoutedEvent='MouseEnter'><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Face' Storyboard.TargetProperty='Opacity' From='0.6' To='1' Duration='0:0:0.65' AutoReverse='True' RepeatBehavior='Forever'/></Storyboard></BeginStoryboard></EventTrigger><EventTrigger RoutedEvent='MouseLeave'><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Face' Storyboard.TargetProperty='Opacity' To='0.72' Duration='0:0:0.22'/></Storyboard></BeginStoryboard></EventTrigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>");
  return w;
 }
 public static Grid Grid(params GridLength[] rows){var g=new Grid();foreach(var row in rows)g.RowDefinitions.Add(new RowDefinition{Height=row});return g;}
 public static TextBlock Label(string text,double size=9)=>new(){Text=text,FontSize=size,Foreground=Palette.Muted,TextWrapping=TextWrapping.Wrap};
}
public static class VaultPrompt {
 public static Window Create(Window? owner,string title,string message,bool confirm=false){var w=VaultUI.Window(owner,"Vault",370,235);w.MinWidth=370;w.MinHeight=235;w.ResizeMode=ResizeMode.NoResize;
  NameScope.SetNameScope(w,new NameScope());var root=VaultUI.Grid(new GridLength(8),new GridLength(1,GridUnitType.Star),new GridLength(48));root.Margin=new Thickness(14,2,14,5);root.Children.Add(new MotionRail{Height=3,VerticalAlignment=VerticalAlignment.Top});
  var stack=new StackPanel{Margin=new Thickness(15,15,15,12)};stack.Children.Add(VaultUI.Label(confirm?"CONFIRM / RECORD REMOVAL":"NOTICE / LOCAL VAULT",8));stack.Children.Add(new TextBlock{Text=title.ToUpperInvariant(),FontSize=20,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,7,0,8)});stack.Children.Add(new TextBlock{Text=message,FontSize=12,TextWrapping=TextWrapping.Wrap});var frame=new RetroFrame{Child=new ScrollViewer{Content=stack,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden}};System.Windows.Controls.Grid.SetRow(frame,1);root.Children.Add(frame);
  var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};
  if(confirm){var cancel=new Button{Content="CANCEL",IsCancel=true,IsDefault=true};cancel.Click+=(_,_)=>w.DialogResult=false;actions.Children.Add(cancel);w.RegisterName("CancelAction",cancel);}
  var okay=new Button{Content=confirm?"DELETE RECORD":"OK",IsDefault=!confirm,Margin=new Thickness(0),Background=Palette.Ink,Foreground=Palette.Paper};okay.Click+=(_,_)=>w.DialogResult=true;actions.Children.Add(okay);w.RegisterName("ConfirmAction",okay);System.Windows.Controls.Grid.SetRow(actions,2);root.Children.Add(actions);VaultChrome.Apply(w,root);return w;
 }
 public static bool Show(Window? owner,string title,string message,bool confirm=false){if(owner?.IsEnabled==false)owner=Application.Current.Windows.OfType<Window>().FirstOrDefault(w=>w.IsActive)??owner;return Create(owner,title,message,confirm).ShowDialog()==true;}
}


