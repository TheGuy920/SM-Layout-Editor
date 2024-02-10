using CustomExtensions;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Xceed.Wpf.Toolkit;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace LayoutEditor.CustomXML
{
    public partial class XmlDOM : Grid, IXmlDOM
    {
        private static readonly JObject json = JObject.Parse(Utility.LoadInternalFile.TextFile("MyGUI_Trace.json"));
        public XmlPosition Position { get; private set; }
        public Guid? ParentId { get; private set; }
        public List<Guid> ChildrenId { get; private set; }
        public FrameworkElement ChildDisplayDOM { get; private set; }
        public Border SelfOutline { get; private set; } = new() { BorderThickness = new(1.5), BorderBrush = Brushes.Gray, Background = Brushes.Transparent };

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
            this.ParentId = Parent;
            this.ChildrenId = [];
            this.Tag = Tag;
            this.Type = Type;
            this.Guid = Guid.NewGuid();
            this.Attributes = [];
            this.Properties = [];
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

        private static readonly Brush[] BrushDepth =
        [
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
        ];

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
            foreach (Guid child in this.ChildrenId)
                ChildrenContainer.Children.Add(this.Document[child].LoadGui(depth + 1));
            // Add/Set Children
            this.GridActor.Children.Add(ChildrenContainer);
            this.Children.Add(this.GridActor);

            //props
            this.PropertyItems = this.BuildPropertiesItem();

            // Return container
            return this;
        }

        private static readonly Brush[] BrushWire = [Brushes.Transparent];
        private static readonly Brush[][] ViewMode = [BrushWire, BrushDepth, BrushWire];
        
        void IXmlDOM.ChangeViewMode(XmlViewMode viewMode)
        {
            if (this.Type.Equals(XmlType.TextBox))
                return;

            if (viewMode != XmlViewMode.Full)
            {
                Brush[] b = ViewMode[(Int32)viewMode];
                this.GridActor.Background = b[this.Depth % b.Length];
                this.SelfOutline.BorderBrush = Brushes.Gray;
                this.ChildrenId.ForEach(c =>
                {
                    if (this.Document.TryGetValue(c, out var item))
                        item.ChangeViewMode(viewMode);
                });
            }
            else
            {
                Brush[] b = ViewMode[(Int32)viewMode];
                this.SelfOutline.BorderBrush = b[this.Depth % b.Length];
                this.ChildrenId.ForEach(c =>
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
            Grid Parent = this.Document[this.ParentId.Value].GridActor;
            Grid Actor = this.GridActor;
            
            ActualSize TargetSize = Actor.GetActualSize(this.GetDisplayLT()) + diff;
            Point Divisor = new(Parent.ActualWidth, Parent.ActualHeight);
            var tpos = TargetSize.Divide(Divisor);
            this.Position = new(tpos);
            
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
        
        public Grid GridActor { get; private set; } = new ()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

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
            foreach (Guid ChildGuid in this.ChildrenId)
                if (this.Document.TryGetValue(ChildGuid, out var item))
                    xml.Append(item.ToString(nest + 1));

            // Empty line if body is empty
            if (this.ChildrenId.Count is 0 && this.Properties.Count is 0)
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

        public IEnumerable<FrameworkElement> PropertyItems { get; private set; }

        private object GetPropertyItem(string key)
        {
            if (this.Properties.TryGetValue(key, out var item))
                return item;
            if (this.Attributes.TryGetValue(key, out var tpl))
                return tpl.Value;
            return null;
        }

        public IEnumerable<FrameworkElement> BuildPropertiesItem()
        {
            StackPanel main = new()
            {
                Orientation = Orientation.Vertical,
                Tag = this.Guid.ToString()
            };

            StackPanel s = new()
            {
                Orientation = Orientation.Horizontal
            };

            s.Children.Add(new TextBlock()
            {
                Foreground = Brushes.White,
                Margin = new Thickness(5, 2, 5, 2),
                FontSize = 24,
                Text = this.Type.ToString()
            });

            yield return s;

            Dictionary<string, string[]> AttributeList = [];
            foreach (var item in json["Widget"][Type.ToString()]["Attributes"].Cast<JProperty>())
            {
                List<string> content = [.. item.Value.Select(v => (string)v)];
                AttributeList.Add(item.Name, [.. content]);
            }

            foreach (var item in AttributeList)
            {
                if (item.Key.Contains("Colour") && AttributeList.ContainsKey("Color"))
                    continue;

                if (!item.Key.Equals("type") && !item.Key.Equals("position"))
                {
                    object cValue = this.GetPropertyItem(item.Key);
                    bool isChecked = cValue != null;
                    if (!isChecked) cValue = item.Value;

                    DockPanel Attribute = new();

                    int w = 125;
                    string k = item.Key;

                    if (!item.Key.Equals("position_real"))
                    {
                        CheckBox _cb = new() { IsChecked = isChecked, Margin = new Thickness(5, 2, 5, 2) };
                        _cb.Checked += EnableProperty;
                        _cb.Unchecked += DisableProperty;
                        Attribute.Children.Add(_cb);
                        w -= 25;

                    }
                    else { k = "Position Real"; }

                    Attribute.Children.Add(new TextBlock()
                    {
                        Foreground = Brushes.White,
                        Width = w,
                        Margin = new Thickness(5, 2, 5, 2),
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = k
                    });

                    switch (item.Key.ToLower())
                    {
                        case "position":
                        case "position_real":
                            string[] parts = this.Position.ToString().Split(' ');
                            var stck = new StackPanel() { Orientation = Orientation.Vertical };
                            foreach (var part in parts)
                                stck.Children.Add(this.NumberInputBuilder([float.Parse(part)], -100, 100, 1));
                            Attribute.Children.Add(stck);
                            break;
                        case "name":
                        default:
                            if (item.Value.Length > 1 && !item.Key.Equals("name") || item.Key.Equals("skin"))
                            {
                                ComboBox cb = new()
                                {
                                    Foreground = Brushes.Black,
                                    Margin = new Thickness(5, 2, 5, 2)
                                };
                                var ary = item.Value;
                                int index = -1;
                                int cind = 0;
                                var clist = this.Attributes.Values.Select(p => p.Value);
                                foreach (string value in ary)
                                {
                                    if (clist.Contains(value))
                                        index = cind;
                                    cb.Items.Add(new ComboBoxItem() { Content = value });
                                    cind++;
                                }
                                if (isChecked && index < 0 && int.TryParse(cValue.ToString(), out index)) { }

                                cb.SelectedIndex = index;

                                cb.SelectionChanged += SelectionChanged;
                                Attribute.Children.Add(cb);
                            }
                            else
                            {
                                if (!isChecked)
                                    cValue = "";
                                var tb = new TextBox()
                                {
                                    Foreground = Brushes.Black,
                                    Margin = new Thickness(5, 2, 5, 2),
                                    Text = this.Attributes.TryGetValue(item.Key, out var n) ? n.Value : cValue.ToString()
                                };
                                tb.TextChanged += TextChanged;
                                Attribute.Children.Add(tb);
                            }
                            break;
                    }

                    if (item.Key.Equals("position_real"))
                    {
                        //main.Children.Insert(1, Attribute);
                    }
                    else
                    {
                        //main.Children.Add(Attribute);
                    }

                    yield return Attribute;
                }
            }

            if ((json["Widget"][this.Type.ToString()] as JObject).ContainsKey("Property"))
            {
                Dictionary<string, object> PropertyList = new();
                foreach (var item in json["Widget"][Type.ToString()]["Property"].Cast<JProperty>())
                {
                    string str = "";
                    if (item.Value.Type == JTokenType.String)
                    {
                        str = item.Value.ToString();
                        PropertyList.Add(item.Name, str);
                    }
                    else
                    {
                        JArray jry = item.Value as JArray;
                        if (jry.Count == 1 || jry.Count == 2 && (
                            jry[0].ToString().ToLower().Contains("int") ||
                            jry[0].ToString().ToLower().Contains("float")
                            ) && (
                            jry[1].ToString().ToLower().Contains("int") ||
                            jry[1].ToString().ToLower().Contains("float")
                            ))
                        {
                            string sk = jry[0].ToString().ToLower().Trim();
                            if (jry.Count > 1 && jry[1].ToString().ToLower().Trim().Length > sk.Length)
                                sk = jry[1].ToString().ToLower().Trim();

                            switch (sk)
                            {
                                case "float":
                                    PropertyList.Add(item.Name, 0f);
                                    break;
                                case "float, float, float, float":
                                    PropertyList.Add(item.Name, new float[4]);
                                    break;
                                case "float, float, float":
                                    PropertyList.Add(item.Name, new float[3]);
                                    break;
                                case "bool":
                                    PropertyList.Add(item.Name, true);
                                    break;
                                default:
                                    PropertyList.Add(item.Name, string.Empty);
                                    break;
                            }
                        }
                        else
                        {
                            PropertyList.Add(item.Name, string.Empty);
                        }
                    }
                }

                foreach (var item in PropertyList)
                {
                    Debug.WriteLine(item.Key);
                    if (item.Key.Contains("Colour") && PropertyList.ContainsKey("Color"))
                        continue;
                    object cValue = this.GetPropertyItem(item.Key);
                    bool isChecked = cValue != null;
                    if (!isChecked) cValue = item.Value;
                    DockPanel Attribute = new();
                    CheckBox _cb = new() { IsChecked = isChecked, Margin = new Thickness(5, 2, 5, 2) };
                    _cb.Checked += EnableProperty;
                    _cb.Unchecked += DisableProperty;
                    Attribute.Children.Add(_cb);
                    Attribute.Children.Add(new TextBlock()
                    {
                        Foreground = Brushes.White,
                        Margin = new Thickness(5, 2, 5, 2),
                        Width = 100,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = item.Key
                    });
                    if (item.Value is string)
                    {

                        var tb = new TextBox()
                        {
                            Foreground = Brushes.Black,
                            Margin = new Thickness(5, 2, 5, 2),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Text = cValue.ToString()
                        };
                        tb.TextChanged += TextChanged;
                        Attribute.Children.Add(tb);
                    }
                    else if (item.Value is bool)
                    {
                        ComboBox cb = new()
                        {
                            Foreground = Brushes.Black,
                            Margin = new Thickness(5, 2, 5, 2),
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };
                        cb.Items.Add(new ComboBoxItem() { Content = "true" });
                        cb.Items.Add(new ComboBoxItem() { Content = "false" });

                        cb.SelectedIndex = -1;

                        if (isChecked && cValue.ToString().ToLower().Equals("true"))
                        {
                            cb.SelectedIndex = 0;
                        }
                        else if (cValue.ToString().ToLower().Equals("false"))
                        {
                            cb.SelectedIndex = 1;
                        }

                        cb.SelectionChanged += SelectionChanged;
                        Attribute.Children.Add(cb);
                    }
                    else if (item.Value is float or float[])
                    {
                        string z = item.Key.ToLower();
                        if (z.Equals("color") || z.Equals("colour") ||
                            z.Equals("textcolour") || z.Equals("textcolor") ||
                            z.Equals("textshadowcolour") || z.Equals("textshadowcolor"))
                        {
                            byte[] colors = new byte[3] { 255, 255, 255 };
                            if (isChecked)
                            {
                                colors = Array.ConvertAll(cValue.ToString().Split(" "), s =>
                                {
                                    return (byte)(float.Parse(s) * 255);
                                });
                            }

                            ColorPicker cp = new()
                            {
                                Foreground = Brushes.Black,
                                SelectedColor = Color.FromRgb(colors[0], colors[1], colors[2]),
                                Margin = new Thickness(5, 2, 5, 2)
                            };
                            cp.SelectedColorChanged += SelectedColorChanged;
                            Attribute.Children.Add(cp);
                        }
                        else
                        {
                            float min = 0;
                            float max = 999999999;

                            if (item.Key.ToLower().Equals("alpha"))
                                continue;

                            float[] kpls = Array.Empty<float>();

                            if (item.Value as float? == null)
                                kpls = Array.ConvertAll(cValue.ToString().Split(" "), s => float.Parse(s));
                            else
                                kpls = new[] { float.Parse(cValue.ToString()) };

                            Attribute.Children.Add(NumberInputBuilder(kpls, min, max, kpls.Length));
                        }
                    }

                    yield return Attribute;
                    //main.Children.Add(Attribute);
                }
            }
        }

        private void SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker cp = sender as ColorPicker;
            DockPanel propertyType = cp.Parent as DockPanel;
            string index = (propertyType.Children[1] as TextBlock).Text;/*
            if (properties.ContainsKey(index))
            {
                properties[index] = $"{cp.SelectedColor.Value.R / 255f} {cp.SelectedColor.Value.G / 255f} {cp.SelectedColor.Value.B / 255f}";
                properties["Alpha"] = cp.SelectedColor.Value.A / 255f;
                (propertyType.Children[0] as CheckBox).IsChecked = true;
            }
            else
            {
                properties.Add(index, $"{cp.SelectedColor.Value.R / 255f} {cp.SelectedColor.Value.G / 255f} {cp.SelectedColor.Value.B / 255f}");
                properties["Alpha"] = cp.SelectedColor.Value.A / 255f;
                (propertyType.Children[0] as CheckBox).IsChecked = true;
            }*/
            //CallBackList[GUID].Invoke();
        }

        private FrameworkElement NumberInputBuilder(float[] values, float min = 0, float max = 0, int nOfNumberInput = 1)
        {
            if (nOfNumberInput > 1)
            {
                StackPanel grid = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Stretch };
                for (int i = 0; i < nOfNumberInput; i++)
                {
                    SingleUpDown p = new()
                    {
                        Height = 20,
                        Value = values[i],
                        Minimum = min,
                        Maximum = max,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Margin = new Thickness(5, 2, 5, 2)
                    };
                    p.PreviewTextInput += NumberValidationTextBox;
                    p.ValueChanged += P_ValueChanged;
                    grid.Children.Add(p);
                }
                return grid;
            }
            else
            {
                SingleUpDown p = new()
                {
                    Height = 20,
                    Value = values[0],
                    Minimum = min,
                    Maximum = max,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(5, 2, 5, 2)
                };
                p.PreviewTextInput += NumberValidationTextBox;
                p.ValueChanged += P_ValueChanged;
                return p;
            }
        }

        private void P_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SingleUpDown numberBox = sender as SingleUpDown;
            StackPanel nbp = numberBox.Parent as StackPanel;
            DockPanel propertyType = nbp.Parent as DockPanel;/*
            string index = (propertyType.Children[1] as TextBlock).Text;
            if (properties.ContainsKey(index))
            {
                properties[index] = numberBox.Value;
                (propertyType.Children[0] as CheckBox).IsChecked = true;
            }
            else
            {
                properties.Add(index, numberBox.Value);
                (propertyType.Children[0] as CheckBox).IsChecked = true;
            }*/
            //CallBackList[GUID].Invoke();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = NumberMatch();
            e.Handled = regex.IsMatch(e.Text);
        }
        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            DockPanel propertyType = textBox.Parent as DockPanel;
            string index = (propertyType.Children[1] as TextBlock).Text;/*
            if (properties.ContainsKey(index))
            {
                properties[index] = textBox.Text;
                (propertyType.Children[0] as CheckBox).IsChecked = true;
            }
            else
            {
                properties.Add(index, textBox.Text);
                (propertyType.Children[0] as CheckBox).IsChecked = true;
            }*/
            //CallBackList[GUID].Invoke();
        }
        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            DockPanel propertyType = comboBox.Parent as DockPanel;
            string index = (propertyType.Children[1] as TextBlock).Text;/*
            if (properties.ContainsKey(index))
            {
                if (e != null)
                {
                    properties[index] = (e.AddedItems[0] as ComboBoxItem).Content.ToString();
                    (propertyType.Children[0] as CheckBox).IsChecked = true;
                }
            }
            else
            {
                if (e != null)
                {
                    properties.Add(index, (e.AddedItems[0] as ComboBoxItem).Content.ToString());
                    (propertyType.Children[0] as CheckBox).IsChecked = true;
                }
            }*/
            //CallBackList[GUID].Invoke();
        }
        private void DisableProperty(object sender, RoutedEventArgs e)
        {
            CheckBox comboBox = sender as CheckBox;
            DockPanel propertyType = comboBox.Parent as DockPanel;
            string index = (propertyType.Children[1] as TextBlock).Text;/*
            if (properties.ContainsKey(index))
                properties.Remove(index);*/
            //CallBackList[GUID].Invoke();
        }

        private void EnableProperty(object sender, RoutedEventArgs e)
        {
            CheckBox comboBox = sender as CheckBox;
            DockPanel propertyType = comboBox.Parent as DockPanel;

            if (propertyType.Children[2] is TextBox)
            {
                TextChanged(propertyType.Children[2], null);
            }
            else if (propertyType.Children[2] is ComboBox)
            {
                SelectionChanged(propertyType.Children[2], null);
            }

        }

        public void UpdateItemProperties(string GUID, string property, string value)
        {/*

            if (properties.ContainsKey(property))
            {
                properties[property] = value;
            }
            else
            {
                properties.Add(property, value);
            }
            */
        }
        [GeneratedRegex("[^0-9]+")]
        private static partial Regex NumberMatch();
    }
}
