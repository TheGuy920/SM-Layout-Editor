using CustomExtensions;
using ICSharpCode.AvalonEdit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LayoutEditor.CustomXML
{
    public class XmlDocumentHandler
    {
        private DateTime LastTextUpdate;
        private Guid RootXml;
        private Grid RootGrid;
        private Canvas RootCanvas;
        private StackPanel PropertiesWindow;
        private Rectangle RootBorder;
        private double Scale = 1;
        private double GridSize = 1;
        private Point? MouseStart;
        private XmlViewMode CurrentViewMode = XmlViewMode.Wire;
        private readonly Task LoadGuiThread;
        private readonly float Version = 1.0f;
        private readonly XmlTextHandler TextHandler;
        private readonly Encoding Encoding = Encoding.UTF8;
        private Dictionary<Guid, IXmlDOM> Document;
        private readonly XmlOverlay Overlay;
        private readonly List<XmlDOM> MouseUpEvent;
        private List<XmlDOM> IsSelected { get { return this.Overlay.BindingPair; } }
        public event EventHandler<object> ChangesMade;
        public event EventHandler<object> ChangesSaved;

        public XmlDocumentHandler(ref TextEditor textEditor, ref Canvas Workspace, ref StackPanel prop)
        {
            this.RootCanvas = Workspace;
            this.PropertiesWindow = prop;
            this.BuildCanvas();
            this.Document = [];
            this.Overlay = new(1);
            this.MouseUpEvent = [];
            this.RootCanvas.LayoutUpdated += this.WorkspaceLayoutUpdated;
            this.TextHandler = new(ref textEditor, this.TextChanged);
            this.LoadGuiThread = new(this.LoadGui);
            this.InitializeDocumentHeaders(ref this.RootGrid);
            this.LastTextUpdate = new DateTime(0);
            this.Overlay.DisplayItemChanged += this.VisualsChanged;
        }

        private void BuildCanvas()
        {
            this.RootGrid = new()
            {
                Name = "Workspace",
                Background = Brushes.Transparent
            };
            this.RootGrid.Loaded += RootGridLoaded;
            this.RootBorder = new Rectangle() { Stroke = Brushes.Gray, StrokeThickness = 3 };
            this.RootGrid.Children.Add(this.RootBorder);
            Binding Width = new()
            {
                ElementName = this.RootCanvas.Name,
                Path = new PropertyPath("ActualWidth")
            };
            this.RootGrid.SetBinding(FrameworkElement.WidthProperty, Width);
            Binding Height = new()
            {
                ElementName = this.RootCanvas.Name,
                Path = new PropertyPath("ActualHeight")
            };
            this.RootGrid.SetBinding(FrameworkElement.HeightProperty, Height);
            this.RootCanvas.Children.Add(this.RootGrid);
        }

        private void RootGridLoaded(object sender, RoutedEventArgs e)
        {
            this.RootGrid.LayoutUpdated -= this.LayoutUpdated;
            this.RootGrid.LayoutUpdated += this.LayoutUpdated;
        }

        public void SetGridSize(double grid)
        {
            this.GridSize = grid;
        }

        public void HighlightBorder()
        {
            this.RootBorder.Stroke = Brushes.White;
        }

        private bool ScaleUpdated = false;

        public void ChangeScale(object sender, double e)
        {
            // set Scale
            this.Scale = e;
            // Scale the workspace
            this.RootGrid.RenderTransform = new ScaleTransform()
            {
                ScaleX = this.Scale,
                ScaleY = this.Scale
            };
            // Scale border
            this.RootBorder.StrokeThickness = 3 / this.Scale;
            // update
            this.ScaleUpdated = true;
        }

        private void LayoutUpdated(object sender, EventArgs e)
        {
            if (this.ScaleUpdated)
            {
                // set overlay scale
                this.Overlay.ChangeScale(this, this.Scale);
                // reset
                this.ScaleUpdated = false;
                // udap
                this.VisualsChanged();
            }
            // this.VisualsChanged();
        }

        public void MouseEnter(object sender, MouseEventArgs e)
        {
            // Invoke all registered children
        }

        public void MouseLeave(object sender, MouseEventArgs e)
        {
            // Invoke all registered children
        }

        public void MouseUp(object sender, MouseButtonEventArgs e)
        {
            // remove highlighting effect
            if (e.ChangedButton.Equals(MouseButton.Middle))
                this.RootBorder.Stroke = Brushes.Gray;
            if (e.ChangedButton.Equals(MouseButton.Left))
            {
                // invoke mouse up and clear
                this.MouseUpEvent.ForEach(f => f.MouseUp(sender, e));
                this.MouseUpEvent.Clear();
                // invoke overlay
                this.Overlay.MouseUp(sender, e);
                this.MouseStart = null;
                this.UpdateMouseStart = false;

                // this.UpdateXML();
            }
        }

        private bool UpdateMouseStart = false;

        private void MouseDown(object sender, MouseButtonEventArgs e)
        {
            XmlDOM dom = sender as XmlDOM;
            this.MouseUpEvent.Add(dom);

            if (!this.Overlay.BindingPair.Contains(dom))
            {
                this.PropertiesWindow.Children.Clear();
                foreach (var item in dom.PropertyItems)
                    this.PropertiesWindow.Children.Add(item);
            }

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                this.Overlay.BindToDOM(dom, true);
            else
                this.Overlay.BindToDOM(dom, false);

            this.UpdateMouseStart = true;

            e.Handled = true;

            this.UnSelectTextBox();
        }

        private bool tick = false;

        public void MouseMove(object sender, MouseEventArgs e)
        {
            if (this.UpdateMouseStart)
            {
                this.UpdateMouseStart = false;
                this.MouseStart = e.GetPosition(this.Overlay);
            }
            else
            {
                this.tick = !this.tick;
                if (this.tick)
                {
                    this.Overlay.MouseMove(this.MouseStart.GetValueOrDefault(), e, this.MouseStart is not null);
                    e.Handled = true;
                }
            }
        }

        public void MoveSelected(Point diff)
        {
            if (diff.X != 0 || diff.Y != 0)
                this.Overlay.MoveByPixels(diff);
        }

        public void DeselectAll()
        {
            this.Overlay.BindToDOM(null);
            this.PropertiesWindow.Children.Clear();
        }

        private void WorkspaceLayoutUpdated(object sender, EventArgs e)
        {
            // Debug.WriteLine("hallo??");
        }

        private void TextChanged()
        {

        }

        private void VisualsChanged(object sender = null, EventArgs e = null)
        {
            if ((DateTime.UtcNow - this.LastTextUpdate).TotalSeconds > 0.5)
            {
                this.UpdateXML();
                this.LastTextUpdate = DateTime.UtcNow;
            }
        }

        private void UpdateXML()
        {
            this.TextHandler.UpdateText(this.ToString());
            this.TextHandler.UpdateCursor(this.IsSelected, this);
            this.ChangesMade.Invoke(this, EventArgs.Empty);
        }

        public void Undo()
        {
            this.TextHandler?.Undo();
        }

        public void Redo()
        {
            this.TextHandler?.Redo();
        }

        public void LoadGuiSync()
        {
            if (this.LoadGuiThread.Status == TaskStatus.Created)
            {
                this.LoadGuiThread.Start();
                this.LoadGuiThread.Wait();
            }
        }

        public void LoadGuiSync(ref Grid Workspace)
        {
            if (Workspace is not null)
                this.RootGrid = Workspace;
            if (this.LoadGuiThread.Status == TaskStatus.Created)
            {
                this.LoadGuiThread.Start();
                this.LoadGuiThread.Wait();
            }
        }

        public void LoadGuiAsync()
        {
            if (this.LoadGuiThread.Status == TaskStatus.Created)
                this.LoadGuiThread.Start();
        }

        public void LoadGuiAsync(ref Grid Workspace)
        {
            if (Workspace is not null)
                this.RootGrid = Workspace;
            if (this.LoadGuiThread.Status == TaskStatus.Created)
                this.LoadGuiThread.Start();
        }

        private void LoadGui()
        {
            this.RootGrid.Dispatcher.Invoke(() => {
                this.RootGrid.Children.RemoveRange(1, this.RootGrid.Children.Count);
            });

            int start = new Random().Next(0, 100);

            foreach (Guid child_guid in this.Document[this.RootXml].ChildrenId)
                this.RootGrid.Dispatcher.Invoke(() => {
                    this.RootGrid.Children.Add(this.Document[child_guid].LoadGui(start));
                });

            this.RootGrid.Dispatcher.Invoke(() =>
            {
                ((this.RootGrid.Parent as Canvas).Parent as Grid).Children.Add(this.Overlay);
            });

            this.VisualsChanged();
        }

        public void DeleteSelected()
        {
            foreach(var elem in this.IsSelected)
                this.Document.Remove(elem.Guid);

            this.DeselectAll();
        }

        public override string ToString()
        {
            StringBuilder DocumentString = new();
            DocumentString.Append($"<?xml version=\"{this.Version.ToString("0.0#")}\" encoding=\"{this.Encoding.WebName.ToUpper()}\"?>");
            DocumentString.Append(Environment.NewLine);
            DocumentString.Append(this.Document[RootXml].ToString());
            return DocumentString.ToString();
        }

        private void InitializeDocumentHeaders(ref Grid Workspace)
        {
            this.Document.Clear();
            MyGUI root = new(ref this.Document, ref Workspace);
            this.RootXml = root.Guid;
            this.Document.Add(this.RootXml, root);
        }

        public void SaveAs(string fileName, bool prettyPrint = true)
        {
            StreamWriter sw = new(
                fileName,
                this.Encoding,
                new FileStreamOptions()
                {
                    Access = FileAccess.Write,
                    Mode = FileMode.Create,
                    Options = FileOptions.SequentialScan
                })
            {
                NewLine = Environment.NewLine
            };
            sw.Write(this.ToString());
            sw.Close();
            sw.Dispose();
        }

        public void LoadString(string xml)
        {
            this.XElementToXDoc(XElement.Parse(xml));
            this.BuildLinkedList();
            this.LoadGui();
        }

        public void LoadFile(string path)
        {
            this.XElementToXDoc(XElement.Load(path));
            this.BuildLinkedList();
            this.LoadGui();
        }

        private void XElementToXDoc(XElement xlm, Guid? Parent = null)
        {
            XmlDOM xmlDOM = null;
            switch (xlm.Name.ToString())
            {
                // Initialize document if document is empty, the tag is MyGUI, and it has no parent
                case "MyGUI":
                    // xmlDOM = new MyGUI(ref Document, ref );
                    // RootXml = xmlDOM.Guid;
                    // Document.Add(xmlDOM.Guid, xmlDOM);
                    break;
                // Add widget
                case "Widget":
                    xmlDOM = this.NewXmlDOM(Parent, XmlTag.Widget, xlm.Attributes());
                    this.Document.Add(xmlDOM.Guid, xmlDOM);
                    break;
                // Add property to parent
                case "Property":
                case "UserString":
                    List<XAttribute> PropertyList = xlm.Attributes().ToList();
                    // index 0 should be the Key and index 1 should be the Value for the propert tag
                    (this.Document[Parent.Value] as XmlDOM).ParseProperties(new(PropertyList[0].Value, PropertyList[1].Value));
                    break;
                case "CodeGeneratorSettings":
                    break;
                default:
                    throw new Exception($"Unkown Tag: {xlm.Name}");
            }
            // Load all XML elements, with their respective parents (children not loaded)
            foreach (XElement Child in xlm.Elements())
                this.XElementToXDoc(Child, xmlDOM?.Guid ?? Parent ?? this.RootXml);
        }

        public void ChangeViewMode(XmlViewMode viewMode)
        {
            this.CurrentViewMode = viewMode;
            var root = this.Document[this.RootXml];
            foreach (Guid child_guid in root.ChildrenId)
                if (this.Document.TryGetValue(child_guid, out var item))
                    this.RootGrid.Dispatcher.Invoke(item.ChangeViewMode,viewMode);
        }

        private XmlDOM NewXmlDOM(Guid? Parent, XmlTag Tag, IEnumerable<XAttribute> attributes = null)
        {
            XmlDOM xmlDOM = new(ref this.Document, Parent, Tag);
            xmlDOM.ParseAttributes(attributes);
            xmlDOM.MouseDown += MouseDown;
            return xmlDOM;
        }

        private void BuildLinkedList()
        {
            // Add all children to their parents
            foreach (IXmlDOM Item in this.Document.Values)
                if (Item.ParentId.HasValue)
                    this.Document[Item.ParentId.Value].ChildrenId.Add(Item.Guid);
        }

        public XmlDOM AddXmlElement(XmlTag tag, XmlType type, Guid? parent = null)
        {
            XmlDOM d = new (
                ref this.Document,
                parent ?? this.RootXml,
                tag,
                type
            );
            this.Document.Add(d.Guid, d);
            return d;
        }

        internal IEnumerable<UIElement> GetSelected() => this.Overlay.BindingPair;

        public void UnSelectTextBox()
        {
            this.TextHandler.RemoveCaret();
            // Keyboard.ClearFocus();
            // (this.RootCanvas.Parent as Grid).Focus();
        }

        public void UpdateOverlay()
        {
            this.Overlay.UpdateSize();
        }

        internal void AddTextSize(int delta)
        {
            this.TextHandler.AddTextSize(delta);
        }
    }
}
