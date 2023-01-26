using CustomExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

namespace LayoutEditor.CustomXML
{
    public class XmlDOM : Grid, IXmlDOM
    {
        public XmlPosition Position { get; private set; }
        public Guid? _Parent { get; private set; }
        public List<Guid> _Children { get; private set; }

        public Guid Guid { get; private set; }
        public new XmlTag Tag { get; private set; }
        public XmlType Type { get; private set; }
        public Dictionary<string, string> Attributes { get; set; }
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
            foreach(XAttribute attribute in attributes)
            {
                switch (attribute.Name.ToString())
                {
                    case "position_real":
                        this.Position = XmlPosition.Parse(attribute.Value);
                        break;
                    case "type":
                        this.Type = Enum.Parse<XmlType>(attribute.Value, true);
                        break;
                    default:
                        if (this.Attributes.ContainsKey(attribute.Name.ToString()))
                            this.Attributes[attribute.Name.ToString()] = attribute.Value.DecodeHtmlEntities();
                        else
                            this.Attributes.Add(attribute.Name.ToString(), attribute.Value.DecodeHtmlEntities());
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
        }
        public void Selected() => ((this.Children[0] as Grid).Children[0] as Border).BorderBrush = Brushes.Blue;
        public void UnSelected() => ((this.Children[0] as Grid).Children[0] as Border).BorderBrush = Brushes.Gray;

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
            // Init New Grid
            Grid grid = new() {
                Background = BrushDepth[depth % BrushDepth.Length],
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            // Set Column and Row
            Grid.SetColumn(grid, 1);
            Grid.SetRow(grid, 1);
            // Border
            Border border = new() { BorderThickness = new(2), BorderBrush = Brushes.Gray, Background = Brushes.Transparent };
            // Set Mouse Down Event
            border.MouseDown += MouseDown_;
            grid.Children.Add(border);
            // Create Child Grid Container
            Grid ChildrenContainer = new();
            // Column and row (span)            
            Grid.SetColumn(ChildrenContainer, 0);
            Grid.SetRow(ChildrenContainer, 0);
            Grid.SetColumnSpan(ChildrenContainer, 3);
            Grid.SetRowSpan(ChildrenContainer, 3);
            // Add children to child container
            foreach (Guid child in this._Children) ChildrenContainer.Children.Add(this.Document[child].LoadGui(depth + 1));
            // Add/Set Children
            grid.Children.Add(ChildrenContainer);
            this.Children.Add(grid);
            // Return container
            return this;
        }

        private static readonly Brush[] BrushWire = new Brush[]{ Brushes.Transparent };
        private static readonly Brush[][] ViewMode = new Brush[][] { BrushWire, BrushDepth, BrushWire };
        void IXmlDOM.ChangeViewMode(XmlViewMode viewMode)
        {
            Brush[] b = ViewMode[(Int32)viewMode];
            (this.Children[0] as Grid).Background = b[this.Depth % b.Length];
            this._Children.ForEach(c =>
            {
                this.Document[c].ChangeViewMode(viewMode);
            });
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
            Grid Parent = this.Document[this._Parent.Value].GetGrid();
            ActualSize TargetSize = (this.Children[0] as Grid).GetActualSize(this.GetDisplayLT()) + diff;
            Point Divisor = new(Parent.ActualWidth, Parent.ActualHeight);
            this.Position = new(TargetSize.Divide(Divisor).Min(0));
            // clear
            this.ColumnDefinitions.Clear();
            this.RowDefinitions.Clear();
            // Set Width/Height percentages
            foreach (var i in this.Position.GetWidth())
                this.ColumnDefinitions.Add(i);
            foreach (var i in this.Position.GetHeight())
                this.RowDefinitions.Add(i);
        }

        private Point GetDisplayLT() => this.Children[0].TranslatePoint(new(0, 0), this);
        public Grid GetGrid() => this.Children[0] as Grid;
        string IXmlDOM.ToString(int nest)
        {
            StringBuilder xml = new();
            // New Line
            xml.Append(Environment.NewLine);
            // Indent Tag
            for (int n = 0; n < nest; n++)
                xml.Append("    ");
            // Open Tag
            xml.Append($"<{Tag} type=\"{Type}\" position_real=\"{Position.ToString()}\"");
            // Add Attributes
            foreach (KeyValuePair<string, string> Attribute in Attributes)
                xml.Append($" {Attribute.Key}=\"{Attribute.Value}\"");
            // Close Main Tag
            xml.Append('>');
            // Add Property Tags
            foreach (KeyValuePair<string, string> Attribute in Properties)
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
            foreach (Guid ChildGuid in _Children)
                xml.Append(Document[ChildGuid].ToString(nest + 1));
            // Empty line if body is empty
            if (_Children.Count is 0 && Properties.Count is 0)
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
            xml.Append($"</{Tag}>");
            // Return
            return xml.ToString();
        }
    }
}
