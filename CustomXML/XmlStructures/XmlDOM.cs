using CustomExtensions;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Point = System.Windows.Point;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Markup;
using Newtonsoft.Json.Linq;
using System.Runtime.ConstrainedExecution;
using SixLabors.ImageSharp.ColorSpaces;

namespace LayoutEditor.CustomXML
{
    public partial class XmlDOM : Grid, IXmlDOM
    {
        public XmlPosition Position { get; private set; }
        public Guid? _Parent { get; private set; }
        public List<Guid> _Children { get; private set; }
        public FrameworkElement ChildDisplayDOM { get; private set; }
        public Border SelfOutline { get; private set; } = new() { BorderThickness = new(2), BorderBrush = Brushes.Gray, Background = Brushes.Transparent };

        public Guid Guid { get; private set; }
        public new XmlTag Tag { get; private set; }
        public XmlType Type { get; private set; }
        public Dictionary<string, (string Name, string Value)> Attributes { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public new MouseButtonEventHandler MouseDown { get; set; }
        private int Depth = 0;

        private readonly Dictionary<Guid, IXmlDOM> Document;
        /// <summary>
        /// Creates a new DOM element of the XmlDOM structure
        /// </summary>
        /// <param name="d">The Reference To The Document Linked List</param>
        /// <param name="Tag">Xml Tag</param>
        /// <param name="Type">Xml Type (type="x")</param>
        /// <param name="Parent">Xml Parent Guid</param>
        public XmlDOM(
            ref Dictionary<Guid, IXmlDOM> Document,
            Guid? Parent,
            XmlTag Tag,
            XmlType Type = XmlType.Layout)
        {
            this.Document = Document;
            this._Parent = Parent;
            this._Children = new();
            this.Tag = Tag;
            this.Type = Type;
            this.Guid = Guid.NewGuid();
            this.Attributes = new();
            this.Properties = new();
            this.Position = new(1f, 1f, 1f, 1f);
        }

        public void ParseAttributes(IEnumerable<XAttribute> attributes)
        {
            foreach (XAttribute attribute in attributes)
            {
                string name = attribute.Name.ToString();
                string key = name.ToLowerInvariant();
                switch (key)
                {
                    case "position_real":
                        this.Position = XmlPosition.Parse(attribute.Value);
                        break;
                    case "type":
                        this.Type = Enum.Parse<XmlType>(attribute.Value, true);
                        break;
                    default:
                        if (this.Attributes.ContainsKey(key))
                            this.Attributes[key] = (name, attribute.Value.DecodeHtmlEntities());
                        else
                            this.Attributes.Add(key, (name, attribute.Value.DecodeHtmlEntities()));
                        break;
                }
            }
        }

        public void ParseProperties(KeyValuePair<string, string> property)
        {
            if (this.Properties.ContainsKey(property.Key))
                this.Properties[property.Key] = property.Value.DecodeHtmlEntities();
            else
                this.Properties.Add(property.Key, property.Value.DecodeHtmlEntities());

            string key = property.Key.ToLowerInvariant();

            switch (this.Type)
            {
                case XmlType.ImageBox:
                    this.SwitchImageProperties(key, property.Value);
                    break;
                case XmlType.TextBox:
                    this.GridActor.Background = Brushes.Transparent;
                    this.SwitchTextProperties(key, property.Value);
                    break;
                case XmlType.Button:
                    this.SwitchTextProperties(key, property.Value);
                    break;
            }
        }

        private void SwitchTextProperties(string key, string value)
        {
            if (this.ChildDisplayDOM is not RichTextBox)
            {
                this.GridActor.Children.Remove(this.ChildDisplayDOM);
                this.ChildDisplayDOM = new RichTextBox()
                {
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new(0)
                };
                this.GridActor.Children.Add(this.ChildDisplayDOM);
            }
            switch (key)
            {
                case "caption":
                    this.LoadTextCaption(value);
                    break;
                case "fontname":
                    this.LoadTextFont(value);
                    break;
                case "textalign":
                    this.LoadTextAlign(value);
                    break;
                case "textcolour":
                case "textcolor":
                    this.LoadTextColor(value);
                    break;
            }
        }

        private void LoadTextColor(string value)
        {
            var parts = value.Split(' ').Select(float.Parse).ToList();
            if (parts.Count < 3 || parts.Count > 4)
                throw new ArgumentException("Invalid input format. Expected 3 (for RGB) or 4 (for RGBA) space-separated values.");

            var r = (int)Math.Round(parts[0] * 255);
            var g = (int)Math.Round(parts[1] * 255);
            var b = (int)Math.Round(parts[2] * 255);
            var a = parts.Count == 4 ? (int)Math.Round(parts[3] * 255) : 255; // If alpha is not provided, default to 255 (fully opaque)
            string color = $"#{a:X2}{r:X2}{g:X2}{b:X2}";
            if (this.IsRichTextBox)
            {
                string v = this.RichTextXml.Replace("Foreground=\"#FFFFFF\"", $"Foreground=\"{color}\"");
                this.TryGetTextBox.Document = XamlReader.Parse(v) as FlowDocument;
            }
        }

        private void LoadTextAlign(string value)
        {
            if (value.Trim().Contains(' '))
            {
                (string Horizontal, string Vertical) = (value.Split(' ')[0].TrimStart(new char[] { 'V', 'H' }), value.Split(' ')[1].TrimStart(new char[] { 'V', 'H' }));
                this.TryGetTextBox.HorizontalAlignment = Enum.Parse<HorizontalAlignment>(Horizontal);
                this.TryGetTextBox.VerticalAlignment = Enum.Parse<VerticalAlignment>(Vertical);
            }
            else
            {
                this.TryGetTextBox.HorizontalAlignment = Enum.Parse<HorizontalAlignment>(value.TrimStart(new char[] { 'V', 'H' }));
                this.TryGetTextBox.VerticalAlignment = Enum.Parse<VerticalAlignment>(value.TrimStart(new char[] { 'V', 'H' }));
            }
            if (this.IsRichTextBox)
            {
                string v = this.RichTextXml.Replace("TextAlignment=\"Left\"", $"TextAlignment=\"{this.TryGetTextBox.HorizontalAlignment}\"");
                this.TryGetTextBox.Document = XamlReader.Parse(v) as FlowDocument;
            }
        }

        private void SwitchImageProperties(string key, string value)
        {
            switch (key)
            {
                case "imagetexture":
                    this.LoadImage(value);
                    break;
            }
        }

        private void LoadTextFont(string value)
        {
            if (MainWindow.Get.Fonts.TryGetValue(value, out var font))
            {
                this.TryGetTextBox.FontFamily = font.FontFamily;
                this.TryGetTextBox.FontSize = font.FontSize;
            }
        }

        private void LoadTextCaption(string value)
        {
            this.TryGetTextBox.Document = XamlReader.Parse(this.FormatString(value)) as FlowDocument;
        }

        private string RichTextXml => XamlWriter.Save(this.TryGetTextBox.Document);
        private RichTextBox TryGetTextBox => this.ChildDisplayDOM as RichTextBox;
        private bool IsRichTextBox => this.ChildDisplayDOM is RichTextBox;

        private string FormatString(string value)
        {
            StringBuilder sb = new();
            sb.Append("<FlowDocument xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Paragraph TextAlignment=\"Left\"><Run Foreground=\"#FFFFFF\">");

            string translate = string.Empty;
            value = this.RunRegex(FindSmParts, value,
                match => MainWindow.Get.SMNamingMap.TryGetValue(match.Value, out translate),
                match => (match.Index, match.Index + match.Length, translate)
            );
            List<(int idx, string clr)> colors = new();
            sb.Append(
                this.RunRegex(FindHexColors, value,
                    match => true,
                    match => (match.Index, match.Index + match.Length, $"</Run><Run Foreground=\"{this.AddToList(match.Value, match.Index, colors)}\">")
                    ));

            string ret = sb.ToString().Replace("<Run></Run>", string.Empty);
            while (ret.Contains("\\n"))
            {
                int id = ret.IndexOf("\\n");
                foreach (var (idx, clr) in colors)
                {
                    if (id <= idx)
                        continue;

                    ret = ret[..id] + $"</Run></Paragraph><Paragraph TextAlignment=\"Left\"><Run Foreground=\"{clr}\">" + ret[(id + 2)..];
                    goto END;
                }
                ret = ret[..id] + $"</Run></Paragraph><Paragraph TextAlignment=\"Left\"><Run Foreground=\"#FFFFFF\">" + ret[(id + 2)..];
            END:
                continue;
            }
            ret += "</Run></Paragraph></FlowDocument>";

            Debug.WriteLine(ret);
            return ret;
        }

        private string AddToList(string value, int index, List<(int, string)> list)
        {
            list.Add((index, value));
            return value;
        }

        private string RunRegex(Func<Regex> pattern, string value, Func<Match, bool> check, Func<Match, (int, int, string)> replace)
        {
            StringBuilder sb = new();

            List<(int Index, int EndIndex, string String)> replacements = new();
            var matches = pattern().Matches(value);
            foreach (Match match in matches.ToArray())
                if (check(match))
                    replacements.Add(replace(match));

            int cindex = 0;
            foreach (var replacement in replacements)
            {
                sb.Append(value[cindex..replacement.Index] + replacement.String);
                cindex = replacement.EndIndex;
            }

            if (cindex < value.Length)
                sb.Append(value[cindex..]);

            return sb.ToString();
        }

        [GeneratedRegex(@"\#\{[^}]+\}")]
        private static partial Regex FindSmParts();

        [GeneratedRegex(@"\#[a-zA-Z0-9]{6}")]
        private static partial Regex FindHexColors();

        public void LoadImage(string name)
        {
            FileInfo imageFile = new DirectoryInfo(MainWindow.Get.GamePath).GetFiles(Path.GetFileName(name), SearchOption.AllDirectories).FirstOrDefault();
            if (imageFile != default && imageFile is not null)
            {
                using var image = SixLabors.ImageSharp.Image.Load(imageFile.FullName);
                using MemoryStream memory = new();
                image.Save(memory, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                memory.Position = 0;
                BitmapImage bitmapImage = new();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                if (this.ChildDisplayDOM != null)
                    this.GridActor.Children.Remove(this.ChildDisplayDOM);

                this.ChildDisplayDOM = new System.Windows.Controls.Image
                {
                    Source = bitmapImage,
                };

                Grid.SetColumn(this.ChildDisplayDOM, 1);
                Grid.SetRow(this.ChildDisplayDOM, 1);
                Grid.SetColumnSpan(this.ChildDisplayDOM, 1);
                Grid.SetRowSpan(this.ChildDisplayDOM, 1);

                this.GridActor.Children.Add(this.ChildDisplayDOM);
            }
        }

        public void Selected() { } //=> (this.GetGrid().Children[0] as Border).BorderBrush = Brushes.Blue;
        public void UnSelected() { } //=> (this.GetGrid().Children[0] as Border).BorderBrush = Brushes.Gray;

        private static readonly Brush[] BrushDepth = new Brush[]
        {
            Brushes.Pink,
            Brushes.PaleGoldenrod,
            Brushes.DeepPink,
            Brushes.Orange,
            Brushes.DarkGreen,
            Brushes.Orchid,
            Brushes.Tomato,
            Brushes.DarkOliveGreen,
            Brushes.GreenYellow,
            Brushes.DarkOrange,
            Brushes.DarkSeaGreen
        };
        Grid IXmlDOM.LoadGui(int depth)
        {
            // Init Container
            this.Cursor = Cursors.Hand;
            this.Depth = depth;
            this.Background = null;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Stretch;
            // Set Width/Height percentages
            foreach (var i in this.Position.GetWidth())
                this.ColumnDefinitions.Add(i);
            foreach (var i in this.Position.GetHeight())
                this.RowDefinitions.Add(i);
            // set thing
            //if (!this.Type.Equals(XmlType.TextBox)) // !this.Type.Equals(XmlType.Button)
            //    this.GridActor.Background = BrushDepth[depth % BrushDepth.Length];
            this.GridActor.Background = Brushes.Transparent;
            // Set Column and Row
            Grid.SetColumn(this.GridActor, 1);
            Grid.SetRow(this.GridActor, 1);
            // Border
            // Set Mouse Down Event
            this.SelfOutline.MouseDown += MouseDown_;
            if (this.Type.Equals(XmlType.TextBox))
                this.SelfOutline.BorderBrush = Brushes.Transparent;
            this.GridActor.Children.Add(this.SelfOutline);
            // Create Child Grid Container
            Grid ChildrenContainer = new();
            // Column and row (span)            
            Grid.SetColumn(ChildrenContainer, 0);
            Grid.SetRow(ChildrenContainer, 0);
            Grid.SetColumnSpan(ChildrenContainer, 3);
            Grid.SetRowSpan(ChildrenContainer, 3);
            // Add children to child container
            foreach (Guid child in this._Children)
                ChildrenContainer.Children.Add(this.Document[child].LoadGui(depth + 1));
            // Add/Set Children
            this.GridActor.Children.Add(ChildrenContainer);
            this.Children.Add(this.GridActor);
            // Return container
            return this;
        }

        private static readonly Brush[] BrushWire = new Brush[] { Brushes.Transparent };
        private static readonly Brush[][] ViewMode = new Brush[][] { BrushWire, BrushDepth, BrushWire };
        void IXmlDOM.ChangeViewMode(XmlViewMode viewMode)
        {
            if (this.Type.Equals(XmlType.TextBox))
                return;

            if (viewMode != XmlViewMode.Full)
            {
                Brush[] b = ViewMode[(Int32)viewMode];
                this.GridActor.Background = b[this.Depth % b.Length];
                this.SelfOutline.BorderBrush = Brushes.Gray;
                this._Children.ForEach(c =>
                {
                    if (this.Document.TryGetValue(c, out var item))
                        item.ChangeViewMode(viewMode);
                });
            }
            else
            {
                Brush[] b = ViewMode[(Int32)viewMode];
                this.SelfOutline.BorderBrush = b[this.Depth % b.Length];
                this._Children.ForEach(c =>
                {
                    if (this.Document.TryGetValue(c, out var item))
                        item.ChangeViewMode(viewMode);
                });
            }
        }

        public new void MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        private void MouseDown_(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed && e.RightButton != MouseButtonState.Pressed)
            {
                this.Cursor = Cursors.SizeAll;

                if (this.MouseDown is not null)
                    this.MouseDown.Invoke(this, e);
                else
                    e.Handled = false;
            }
        }

        public void UpdateSizeByPixels(ActualSize diff)
        {
            Grid Parent = this.Document[this._Parent.Value].GridActor;
            Grid Actor = this.GridActor;
            ActualSize TargetSize = Actor.GetActualSize(this.GetDisplayLT()) + diff;
            Point Divisor = new(Parent.ActualWidth, Parent.ActualHeight);
            var tpos = TargetSize.Divide(Divisor);
            this.Position = new(tpos.Min(0));
            // clear
            this.ColumnDefinitions.Clear();
            this.RowDefinitions.Clear();
            // Set Width/Height percentages
            foreach (var i in this.Position.GetWidth())
                this.ColumnDefinitions.Add(i);
            foreach (var i in this.Position.GetHeight())
                this.RowDefinitions.Add(i);
        }

        private Point GetDisplayLT() => this.GridActor.TranslatePoint(new(0, 0), this);
        public Grid GridActor => _gridActor ??= new()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        private Grid _gridActor { get; set; }
        string IXmlDOM.ToString(int nest)
        {
            StringBuilder xml = new();
            // New Line
            xml.Append(Environment.NewLine);
            // Indent Tag
            for (int n = 0; n < nest; n++)
                xml.Append("    ");
            // Open Tag
            xml.Append($"<{this.Tag} type=\"{this.Type}\" position_real=\"{this.Position.ToString()}\"");
            // Add Attributes
            foreach ((string Name, string Value) in this.Attributes.Values)
                xml.Append($" {Name}=\"{Value}\"");
            // Add Tag and Close Main Tag
            xml.Append($" id=\"{this.Guid}\">");
            // Add Property Tags
            foreach (KeyValuePair<string, string> Attribute in this.Properties)
            {
                // New Line
                xml.Append(Environment.NewLine);
                // Indent Tag
                for (int n = 0; n < nest + 1; n++)
                    xml.Append("    ");
                // Property Tag
                xml.Append($"<Property key=\"{Attribute.Key}\" value=\"{Attribute.Value}\"/>");
            }
            // Add Children
            foreach (Guid ChildGuid in this._Children)
                if (this.Document.TryGetValue(ChildGuid, out var item))
                    xml.Append(item.ToString(nest + 1));
            // Empty line if body is empty
            if (this._Children.Count is 0 && this.Properties.Count is 0)
            {
                // New Line
                xml.Append(Environment.NewLine);
                // Indent Tag
                for (int n = 0; n < nest + 1; n++)
                    xml.Append("    ");
            }
            // New Line
            xml.Append(Environment.NewLine);
            // Indent Tag
            for (int n = 0; n < nest; n++)
                xml.Append("    ");
            // Close Tag
            xml.Append($"</{this.Tag}>");
            // Return
            return xml.ToString();
        }
    }
}
