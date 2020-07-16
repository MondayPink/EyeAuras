using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml;
using EyeAuras.CsScriptAuras.Resources;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using JetBrains.Annotations;
using PoeShared.Native;

namespace EyeAuras.CsScriptAuras.Controls
{
    /// <summary>
    ///     Class that inherits from the AvalonEdit TextEditor control to
    ///     enable MVVM interaction.
    /// </summary>
    public class ExtendedTextEditor : TextEditor, INotifyPropertyChanged
    {
        public static readonly DependencyProperty TextContentProperty = DependencyProperty.Register(
            "TextContent", 
            typeof(string), 
            typeof(ExtendedTextEditor), 
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnMyContentChanged));

        public string TextContent
        {
            get => Text;
            set => Text = value;
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public ExtendedTextEditor()
        {
            var manager = new PreloadedHighlightingsManager();
            var highlightings = manager.GetAvailableHighlightings().ToArray();

            if (highlightings.Any())
            {
                using (var textReader = new StringReader(highlightings.First()))
                using (var reader = new XmlTextReader(textReader))
                {
                    this.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }
            
            Options = new TextEditorOptions
            {
                AllowScrollBelowDocument = false,
                AllowToggleOverstrikeMode = false,
                ConvertTabsToSpaces = true,
                EnableEmailHyperlinks = false,
                EnableHyperlinks = true,
                EnableImeSupport = false,
                EnableRectangularSelection = false,
                EnableTextDragDrop = false,
                EnableVirtualSpace = false,
            };
            TextArea.Document.UndoStack.SizeLimit = 64;
        }

        private static void OnMyContentChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var control = (ExtendedTextEditor) sender;
            var newValue = e.NewValue as string;
            if (!string.Equals(newValue, control.TextContent, StringComparison.Ordinal))
            {
                control.TextContent = newValue;
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            SetCurrentValue(TextContentProperty, Text);
            base.OnTextChanged(e);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}