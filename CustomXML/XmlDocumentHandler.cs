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
using System.Xml.Linq;

namespace LayoutEditor.CustomXML
{
    public class XmlDocumentHandler
    {
        private DateTime LastTextUpdate;
        private Guid RootXml;
        private Grid RootGrid;
        private Canvas RootCanvas;
        private Rectangle RootBorder;
        private double Scale = 1;
        private double GridSize = 1;
        private Point? MouseStart;
        private XmlViewMode CurrentViewMode = XmlViewMode.Wire;
        private readonly Task LoadGuiThread;
        private readonly float Version = 1.0f;
        private readonly XmlTextHandler TextHandler;
        private readonly Encoding Encoding = Encoding.UTF8;
        private Dictionary<Guid, IXmlDOM> Document = new();
        private readonly XmlOverlay Overlay = new(1);
        private readonly List<XmlDOM> MouseUpEvent = new();
        private List<XmlDOM> IsSelected { get { return this.Overlay.BindingPair; } }
        public EventHandler<object> ChangesMade;
        public EventHandler<object> ChangesSaved;
        public XmlDocumentHandler(ref TextEditor textEditor, ref Canvas Workspace)
        {
            this.RootCanvas = Workspace;
            this.BuildCanvas();
            this.RootCanvas.LayoutUpdated += this.WorkspaceLayoutUpdated;
            this.TextHandler = new(ref textEditor, this.TextChanged);
            this.LoadGuiThread = new(this.LoadGui);
            this.InitializeDocumentHeaders(ref this.RootGrid);
            this.LastTextUpdate = new DateTime(0);
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
            if (ScaleUpdated)
            {
                // set overlay scale
                this.Overlay.ChangeScale(this, this.Scale);
                // reset
                this.ScaleUpdated = false;
            }
            this.VisualsChanged();
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
            if (e.ChangedButton.Equals(MouseButton.Middle)) this.RootBorder.Stroke = Brushes.Gray;
            if (e.ChangedButton.Equals(MouseButton.Left))
            {
                // invoke mouse up and clear
                this.MouseUpEvent.ForEach(f => f.MouseUp(sender, e));
                this.MouseUpEvent.Clear();
                // invoke overlay
                this.Overlay.MouseUp(sender, e);
                this.MouseStart = null;
                this.UpdateMouseStart = false;

                this.UpdateXML();
            }
        }
        private bool UpdateMouseStart = false;
        private void MouseDown(object sender, MouseButtonEventArgs e)
        {
            XmlDOM dom = sender as XmlDOM;
            this.MouseUpEvent.Add(dom);

            if ( Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                this.Overlay.BindToDOM(dom, true);
            else
                this.Overlay.BindToDOM(dom, false);

            this.UpdateMouseStart = true;

            e.Handled = true;
        }
        bool tick = false;
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
        public void DeselectAll()
        {
            this.Overlay.BindToDOM(null);
        }

        private void WorkspaceLayoutUpdated(object sender, EventArgs e)
        {
            
        }
        private void TextChanged()
        {

        }
        private void VisualsChanged()
        {
            if ((DateTime.UtcNow - this.LastTextUpdate).TotalSeconds > 0.5)
            {
                UpdateXML();
                this.LastTextUpdate = DateTime.UtcNow;
            }
        }
        private void UpdateXML()
        {
            this.TextHandler.UpdateText(this.ToString());
            this.ChangesMade.Invoke(this, EventArgs.Empty);
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

            foreach (Guid child_guid in this.Document[this.RootXml]._Children)
                this.RootGrid.Dispatcher.Invoke(() => {
                    this.RootGrid.Children.Add(this.Document[child_guid].LoadGui(start));
                });

            this.RootGrid.Dispatcher.Invoke(() =>
            {
                (this.RootGrid.Parent as Canvas).Children.Add(this.Overlay);
            });

            this.VisualsChanged();
        }
        public override string ToString()
        {
            StringBuilder DocumentString = new();
            DocumentString.Append($"<?xml version=\"{this.Version}\" encoding=\"{this.Encoding.WebName.ToUpper()}\"?>");
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
            StreamWriter sw = new(fileName, this.Encoding, new FileStreamOptions()
            {
                Access = FileAccess.ReadWrite,
                Mode = FileMode.OpenOrCreate,
                Options = FileOptions.SequentialScan
            });
            sw.NewLine = Environment.NewLine;
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
                    xmlDOM = NewXmlDOM(Parent, XmlTag.Widget, xlm.Attributes());
                    this.Document.Add(xmlDOM.Guid, xmlDOM);
                    break;
                // Add property to parent
                case "Property":
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
            foreach (XElement Child in xlm.Elements()) this.XElementToXDoc(Child, xmlDOM?.Guid ?? Parent ?? this.RootXml);
        }
        public void ChangeViewMode(XmlViewMode viewMode)
        {
            this.CurrentViewMode = viewMode;
            foreach (Guid child_guid in this.Document[this.RootXml]._Children)
                this.RootGrid.Dispatcher.Invoke(() => {
                    this.Document[child_guid].ChangeViewMode(viewMode);
                });
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
            foreach (IXmlDOM Item in this.Document.Values) if (Item._Parent.HasValue) this.Document[Item._Parent.Value]._Children.Add(Item.Guid);
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
    }
}
