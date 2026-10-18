using System.Windows;
using System.Windows.Controls;
namespace VelvetVault;
public static class TransferPreview {
 public static bool Show(Window owner,string title,string description,IEnumerable<Entry> entries,string action){
  var dialog=VaultUI.Window(owner,"Vault",480,425);var root=VaultUI.Grid(new GridLength(8),new GridLength(1,GridUnitType.Star),new GridLength(48));root.Margin=new Thickness(14,2,14,6);root.Children.Add(new MotionRail{Height=3});
  var body=new StackPanel{Margin=new Thickness(17)};body.Children.Add(VaultUI.Label("ARCHIVE / TRANSFER PREVIEW",8));body.Children.Add(new TextBlock{Text=title.ToUpperInvariant(),FontSize=23,Margin=new Thickness(0,8,0,8)});body.Children.Add(new TextBlock{Text=description,FontSize=11,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,14)});
  foreach(var entry in entries)body.Children.Add(new TextBlock{Text=entry.Name,FontSize=13,TextTrimming=TextTrimming.CharacterEllipsis,Margin=new Thickness(0,0,0,7)});
  var frame=new RetroFrame{Child=new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden}};Grid.SetRow(frame,1);root.Children.Add(frame);
  var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};var cancel=new Button{Content="CANCEL",IsCancel=true,IsDefault=true,FontSize=10};cancel.Click+=(_,_)=>dialog.DialogResult=false;var okay=new Button{Content=action,FontSize=10,Background=Palette.Ink,Foreground=Palette.Paper,Margin=new Thickness(0)};okay.Click+=(_,_)=>dialog.DialogResult=true;actions.Children.Add(cancel);actions.Children.Add(okay);Grid.SetRow(actions,2);root.Children.Add(actions);VaultChrome.Apply(dialog,root);return dialog.ShowDialog()==true;
 }
}
