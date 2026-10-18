#nullable enable
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VelvetVault;
static class DialogChecks {


 static T? Find<T>(DependencyObject root) where T:DependencyObject {if(root is T found)return found;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var nested=Find<T>(VisualTreeHelper.GetChild(root,i));if(nested!=null)return nested;}return null;}
 static void Click(Button b)=>b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
 static void Shot(Window w,string path){var frame=new DispatcherFrame();var wait=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)};wait.Tick+=(_,_)=>{wait.Stop();frame.Continue=false;};wait.Start();Dispatcher.PushFrame(frame);w.UpdateLayout();var bmp=new RenderTargetBitmap((int)w.ActualWidth,(int)w.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(w);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));using var stream=File.Create(path);encoder.Save(stream);}
 public static void Run(MainWindow owner,string testPath){
  string target=testPath+".entry-test";File.Copy(testPath,target);
  try {
   Entry? committed=null;
   var entry=new EntryWindow(owner,null,record=>{committed=record;VaultStorage.Save(target,new List<Entry>{record});return true;},importMany:_=>false);entry.Window.Show();entry.Window.UpdateLayout();Click(entry.SaveButton);if(committed!=null||!entry.Window.IsVisible)throw new Exception("Invalid entry was accepted");
   entry.NameInput.Text="DESIGN TEST";entry.UserInput.Text="test-user";entry.EmailInput.Text="test@example.com";entry.SecretInput.Password="sample-test-123!";entry.SecretInput.Focus();entry.Window.UpdateLayout();var caretFrame=new DispatcherFrame();var caretWait=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(70)};caretWait.Tick+=(_,_)=>{caretWait.Stop();caretFrame.Continue=false;};caretWait.Start();Dispatcher.PushFrame(caretFrame);var block=Find<PasswordBlockCaret>(entry.Window);if(block==null||!block.IsVisible||!block.TryGetPosition(out _)||entry.SecretInput.CaretBrush!=Brushes.Transparent||entry.NameInput.CaretBrush!=Brushes.Transparent||entry.UserInput.CaretBrush!=Brushes.Transparent||entry.EmailInput.CaretBrush!=Brushes.Transparent||entry.NotesInput.CaretBrush!=Brushes.Transparent)throw new Exception("Entry block caret placement failed");Console.WriteLine("PASS: entry fields use the block caret, including the native masked password field");entry.NotesInput.Text="Sample recovery details — test data only.";Click(entry.RevealButton);if(entry.RevealInput.Text!="sample-test-123!"||!entry.RevealInput.IsVisible)throw new Exception("Reveal failed");entry.RevealInput.Text="changed-test-321!";Click(entry.RevealButton);if(entry.SecretInput.Password!="changed-test-321!")throw new Exception("Hide did not preserve changes");Click(entry.GenerateButton);string generated=entry.SecretInput.Password;if(generated.Length!=20||!generated.Any(char.IsUpper)||!generated.Any(char.IsLower)||!generated.Any(char.IsDigit)||!generated.Any(c=>!char.IsLetterOrDigit(c)))throw new Exception("Generated key requirements failed");Shot(entry.Window,"entry-night.png");Click(entry.SaveButton);if(entry.Window.IsVisible||committed?.Password!=generated||VaultStorage.Load(target)[0].Notes!=entry.NotesInput.Text)throw new Exception("Entry save failed");
   if(VaultStorage.Load(target)[0].Username!="test-user"||VaultStorage.Load(target)[0].Email!="test@example.com")throw new Exception("Separate username/email persistence failed");Console.WriteLine("PASS: separate username and email persist independently");
   owner.ToggleTheme();bool cancelledCommit=false;var day=new EntryWindow(owner,committed,_=>{cancelledCommit=true;return true;});day.Window.Show();if(day.UserInput.Text!="test-user"||day.EmailInput.Text!="test@example.com")throw new Exception("Separate username/email edit failed");Click(day.DetailsButton);Shot(day.Window,"entry-day.png");Click(day.CancelButton);if(cancelledCommit)throw new Exception("Cancel changed saved data");
   Confirm(owner,false,"prompt-day.png");owner.ToggleTheme();Confirm(owner,false,"prompt-night.png");Confirm(owner,true,null);
   Console.WriteLine("PASS: themed add/edit validation, reveal/hide, generation, save/cancel, recovery details, and both delete choices");
  }finally{if(File.Exists(target))File.Delete(target);}
 }
 static void Confirm(MainWindow owner,bool accept,string? preview){var dialog=VaultPrompt.Create(owner,"Delete record?","Delete DESIGN TEST? This removes it from this vault.",true);var cancel=(Button)dialog.FindName("CancelAction");var yes=(Button)dialog.FindName("ConfirmAction");if(!cancel.IsDefault||yes.IsDefault)throw new Exception("Deletion is the default action");var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(200)};timer.Tick+=(_,_)=>{timer.Stop();if(preview!=null)Shot(dialog,preview);Click(accept?yes:cancel);};timer.Start();bool result=dialog.ShowDialog()==true;if(result!=accept)throw new Exception("Confirmation result failed");}
}




