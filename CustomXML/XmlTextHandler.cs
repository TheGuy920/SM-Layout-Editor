
using CustomExtensions;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.Model;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
            this.XmlEditor = editor;
            this.TextChangedCallback = textChangedCallback;
            this.InitializeXmlEditor();
        }

        private void UpdateXmlFoldings()
        {
            if (this.CurrentXmlFoldingStrategy != null
                    && this.CurrentXmlFoldingManager != null
                    && this.XmlEditor != null
                    && this.XmlEditor.Document != null
                    && this.XmlEditor.Text.Length > 2)
            {
                this.CurrentXmlFoldingStrategy.UpdateFoldings(this.CurrentXmlFoldingManager, this.XmlEditor.Document);
            }
        }

        private void InitializeXmlEditor()
        {
            this.XmlEditor.Foreground = Brushes.Red;
            this.XmlEditor.WordWrap = true;
            this.XmlEditor.TextArea.Caret.CaretBrush = Brushes.Red;
            SearchPanel.Install(this.XmlEditor, new SolidColorBrush(Color.FromRgb(40, 40, 40)), Brushes.White);
            this.XmlEditor.ShowLineNumbers = true;
            this.CurrentXmlFoldingStrategy = new XmlFoldingStrategy()
            { 
                ShowAttributesWhenFolded = true,
            };
            this.XmlEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
            this.CurrentXmlFoldingManager = FoldingManager.Install(this.XmlEditor.TextArea, Brushes.WhiteSmoke, MainWindow.Get.EntireWindow.Background, Brushes.DarkSlateGray);
            this.XmlEditor.TextArea.Options.HighlightCurrentLine = true;
            this.XmlEditor.TextArea.TextView.CurrentLineBorder = new Pen() { Brush = new SolidColorBrush(Color.FromArgb(16, 255, 255, 255)) };
            this.XmlEditor.TextArea.TextView.LinkTextForegroundBrush = Brushes.Blue;
            this.XmlEditor.TextArea.Options.EnableHyperlinks = true;
            this.XmlEditor.TextArea.Options.IndentationSize = 4;
            this.XmlEditor.TextArea.Options.HideCursorWhileTyping = false;
            this.XmlEditor.TextArea.Options.EnableTextDragDrop = true;
            this.XmlEditor.TextArea.Options.ShowSpaces = false;
            this.XmlEditor.TextArea.TextView.NonPrintableCharacterBrush = Brushes.DimGray;
            this.XmlEditor.TextArea.Options.ConvertTabsToSpaces = true;
            this.XmlEditor.TextChanged += TextChanged;
            this.XmlEditor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(128, 51, 153, 255));
            this.XmlEditor.TextArea.SelectionForeground = null;
            this.XmlEditor.TextArea.SelectionCornerRadius = 0;
            this.XmlEditor.TextArea.SelectionBorder = new Pen() { Brush = Brushes.Transparent, Thickness = 0 };
            this.XmlEditor.SyntaxHighlighting = Utility.LoadInternalFile.HighlightingDefinition("HightlightingRules.xshd");
            //this.XmlEditor.SyntaxHighlighting = Utility.LoadInternalFile.FormatHighlightingDefinition("HightlightingRules.xshd", "<!--REPLACE-->", this.FontTagBuilder());
            this.CurrentXmlFoldingStrategy.UpdateFoldings(this.CurrentXmlFoldingManager, this.XmlEditor.Document);
        }

        private string FontTagBuilder()
        {
            StringBuilder builder = new();
            
            foreach (var key in MainWindow.Get.Fonts.Keys.OrderDescending())
            {
                var font = MainWindow.Get.Fonts[key];
                builder.AppendLine($"<Rule color=\"ValidType\">{key}</Rule>");
                foreach (var tag in font.Tags)
                    builder.AppendLine($"<Rule color=\"SMFormat\">\\#\\{{{tag}\\}}</Rule>");
            }

            string current = builder.ToString();

            foreach (var line in Utility.LoadInternalFile.TextFile("InterfaceTags.txt").Split(Environment.NewLine))
                if (line.Split(' ')[0] is string name && line.Trim().Length > 2 && !current.Contains(name))
                    builder.AppendLine($"<Rule color=\"SMFormat\">\\#\\{{{name}\\}}</Rule>");
            
            
            return builder.ToString();
        }

        private void ShowCompletion(IEnumerable<string> suggestions)
        {
            CompletionWindow completionWindow = new(this.XmlEditor.TextArea);
            IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;

            foreach (var suggestion in suggestions)
            {
                data.Add(new AutoFill(suggestion));
            }

            completionWindow.Show();
        }

        public void Undo() => this.XmlEditor.Undo();

        public void Redo() => this.XmlEditor.Redo();

        private void TextChanged(object sender, EventArgs e)
        {
            // this.TextChangedCallback.Invoke();
            this.UpdateXmlFoldings();
        }

        public void UpdateText(string text)
        {
            if (this.XmlEditor.Text.Equals(text))
                return;

            this.XmlEditor.Text = text;
        }

        public void UpdateCursor(List<XmlDOM> doms, XmlDocumentHandler hndl)
        {
            /*
            if (this.XmlEditor != null && doms.Count > 0)
            {
                string XmlAsText = this.XmlEditor.Text;
                int ind = XmlAsText.IndexOf(doms.LastOrDefault().Tag.ToString());
                if (ind > 0)
                {
                    XmlAsText = XmlAsText[..ind];
                    int line = XmlAsText.Count(s => s == '\n') + 1;
                    int column = XmlAsText[XmlAsText.LastIndexOf('\n')..].IndexOf('<');
                    this.XmlEditor.TextArea.Caret.Position = new TextViewPosition(line, column);
                    this.XmlEditor.TextArea.Caret.Show();
                    this.XmlEditor.ScrollToLine(line);
                }
            }
            */
        }

        public void RemoveCaret()
        {
            this.XmlEditor.TextArea.Focusable = false;
            this.XmlEditor.TextArea.ClearSelection();
            Task.Run(async () => 
            { 
                await Task.Delay(10);
                this.XmlEditor.TextArea.Dispatcher.Invoke(() =>
                {
                    this.XmlEditor.TextArea.Focusable = true;
                });
            });
        }

        internal void AddTextSize(int delta)
            => this.XmlEditor.TextArea.FontSize = Math.Max(Math.Min(this.XmlEditor.TextArea.FontSize + (delta / 100), 148), 6);
        
    }
}
