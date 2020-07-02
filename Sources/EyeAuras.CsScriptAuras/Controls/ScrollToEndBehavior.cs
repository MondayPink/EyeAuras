using System;
using System.Windows;
using System.Windows.Interactivity;
using ICSharpCode.AvalonEdit;

namespace EyeAuras.CsScriptAuras.Controls
{
    public sealed class ScrollToEndBehavior : Behavior<TextEditor>
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
            "IsEnabled",
            typeof(bool),
            typeof(ScrollToEndBehavior),
            new PropertyMetadata(true));

        public bool IsEnabled
        {
            get { return (bool) GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }
        
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.TextChanged += AssociatedObjectOnTextChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.TextChanged -= AssociatedObjectOnTextChanged;
            base.OnDetaching();
        }

        private void AssociatedObjectOnTextChanged(object? sender, EventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }
            
            AssociatedObject.ScrollToEnd();
        }
    }
}