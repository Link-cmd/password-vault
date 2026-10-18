using System.Windows;
namespace VelvetVault;
public static class ButtonShape {
 public static readonly DependencyProperty CornerRadiusProperty=DependencyProperty.RegisterAttached("CornerRadius",typeof(CornerRadius),typeof(ButtonShape),new FrameworkPropertyMetadata(new CornerRadius(10)));
 public static CornerRadius GetCornerRadius(DependencyObject target)=>(CornerRadius)target.GetValue(CornerRadiusProperty);
 public static void SetCornerRadius(DependencyObject target,CornerRadius value)=>target.SetValue(CornerRadiusProperty,value);
}
