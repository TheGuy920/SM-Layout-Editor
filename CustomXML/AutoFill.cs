using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LayoutEditor.CustomXML
{
    internal class AutoFill(string text) : ICompletionData
    {
        private readonly string _text = text;

        // No image for now, but you can add one if needed.
        public ImageSource Image => null;

        // The actual completion text.
        public string Text => _text;

        // The content to be displayed in the completion window.
        public object Content => _text;

        // A description for the completion item (you can customize this further).
        public object Description => $"Insert {_text}";

        // Default priority. You can customize this if you want certain items to appear higher in the list.
        public double Priority => 0;

        // When selected, the completion text is inserted at the current position.
        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, _text);
        }
    }
}
