
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace LayoutEditor.CustomXML
{
    internal class XmlTextHandler
    {
        private XmlFoldingStrategy CurrentXmlFoldingStrategy;
        private FoldingManager CurrentXmlFoldingManager;
        private readonly TextEditor XmlEditor;
        private readonly Action TextChangedCallback;
        public XmlTextHandler(ref TextEditor editor, Action textChangedCallback)
        {
            XmlEditor = editor;
            TextChangedCallback = textChangedCallback;
            InitializeXmlEditor();
        }
        private void UpdateXmlFoldings()
        {
            if (CurrentXmlFoldingStrategy != null
                    && CurrentXmlFoldingManager != null
                    && XmlEditor != null
                    && XmlEditor.Document != null
                    && XmlEditor.Text.Length > 2)
            {
                CurrentXmlFoldingStrategy.UpdateFoldings(CurrentXmlFoldingManager, XmlEditor.Document);
            }
        }
        private void InitializeXmlEditor()
        {
            XmlEditor.Foreground = Brushes.Red;
            XmlEditor.WordWrap = true;
            XmlEditor.TextArea.Caret.CaretBrush = Brushes.Yellow;
            SearchPanel.Install(XmlEditor, new SolidColorBrush(Color.FromRgb(40, 40, 40)), Brushes.White);
            XmlEditor.ShowLineNumbers = true;
            CurrentXmlFoldingStrategy = new XmlFoldingStrategy();
            XmlEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
            CurrentXmlFoldingManager = FoldingManager.Install(XmlEditor.TextArea, Brushes.White, MainWindow.Get.EntireWindow.Background, Brushes.DarkSlateGray);
            XmlEditor.TextArea.Options.HighlightCurrentLine = true;
            XmlEditor.TextArea.Options.EnableTextDragDrop = true;
            XmlEditor.TextChanged += TextChanged;
            XmlEditor.TextArea.SelectionBrush = Brushes.DarkBlue;
            XmlEditor.TextArea.SelectionForeground = Brushes.White;
            XmlEditor.TextArea.SelectionCornerRadius = 0;
            XmlEditor.TextArea.SelectionBorder = new Pen() { Brush = Brushes.DarkBlue, Thickness = 1 };
            XmlEditor.SyntaxHighlighting = Utility.LoadInternalFile.HighlightingDefinition("HightlightingRules.xshd");
            CurrentXmlFoldingStrategy.UpdateFoldings(CurrentXmlFoldingManager, XmlEditor.Document);
        }
        private void TextChanged(object sender, EventArgs e)
        {
            TextChangedCallback.Invoke();
            UpdateXmlFoldings();
        }
        public void UpdateText(string text)
        {
            if (XmlEditor.Text.Equals(text))
                return;
            XmlEditor.Text = text;
        }
        private void UpdateCursorPos()
        {
            /*
            if (ActiveElement != null && XmlEditor != null)
            {
                string XmlAsText = CurrentXmlObject.PrettyXml(false);
                int ind = XmlAsText.IndexOf(ActiveElement.Tag.ToString());
                if (ind > 0)
                {
                    XmlAsText = XmlAsText[..ind];
                    int line = XmlAsText.Count(s => s == '\n') + 1;
                    int column = XmlAsText[XmlAsText.LastIndexOf('\n')..].IndexOf('<');
                    XmlEditor.TextArea.Caret.Position = new ICSharpCode.XmlEditor.TextViewPosition(line, column);
                    XmlEditor.TextArea.Caret.Show();
                    XmlEditor.ScrollToLine(line);
                }
                (Properties.Parent as ScrollViewer).ScrollToEnd();
            }
            */
        }
    }
}
