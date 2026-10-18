using System.Windows;
using System.Windows.Media;
namespace VelvetVault;
public static class Palette {
 public static bool Night=true;
 public static Brush Ink=>new SolidColorBrush(Night?Color.FromRgb(225,225,215):Color.FromRgb(20,21,18));
 public static Brush Paper=>new SolidColorBrush(Night?Color.FromRgb(23,24,21):Color.FromRgb(224,225,215));
 public static Brush Muted=>new SolidColorBrush(Night?Color.FromRgb(155,157,145):Color.FromRgb(91,94,83));
 public static Brush Faint=>new SolidColorBrush(Night?Color.FromRgb(65,68,59):Color.FromRgb(173,177,162));
 public static Brush Alpha(byte alpha)=>new SolidColorBrush(Color.FromArgb(alpha,Night?(byte)225:(byte)20,Night?(byte)225:(byte)21,Night?(byte)215:(byte)18));
 public static void Invalidate(DependencyObject root){if(root is UIElement u)u.InvalidateVisual();for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)Invalidate(VisualTreeHelper.GetChild(root,i));}
}
