using System.Windows;
using System.Windows.Controls;

namespace EyeAuras.Controls.Scaffolding
{
    public static class ColorPickerHelper
    {
        public static readonly DependencyProperty PickerDockProperty = DependencyProperty.RegisterAttached(
            "PickerDock", typeof(Dock), typeof(ColorPickerHelper), new PropertyMetadata(Dock.Right));

        public static void SetPickerDock(DependencyObject element, Dock value)
        {
            element.SetValue(PickerDockProperty, value);
        }

        public static Dock GetPickerDock(DependencyObject element)
        {
            return (Dock) element.GetValue(PickerDockProperty);
        }
    }
}