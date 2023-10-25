using CustomExtensions;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Search;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Path = System.IO.Path;
using System.Windows.Threading;
using System.Xml;
using Microsoft.Win32;
using System.Text;
using LayoutEditor.CustomXML;
using System.Diagnostics;
using System.Collections.Generic;
using DiffPlex.DiffBuilder.Model;

namespace LayoutEditor.Windows.Pages
{
    /// <summary>
    /// Interaction logic for MainEditor.xaml
    /// </summary>
    public partial class MainEditor : Page
    {
        public Point MouseStart = new(0, 0);
        private Point Resolution = new(1920, 1080);
        private double d_scale = 1;
        public double Scale
        {
            get
            {
                return this.d_scale;
            }
            set
            {
                this.d_scale = value;
                this.XmlDocumentHandler.ChangeScale(this, value);
                this.ZoomDisplay.Text = (Math.Floor(value * 100) / 100).ToString() + "x";
            }
        }

        private bool ResizingProperties;
        private bool ResizingEditor;
        private bool MovingWorkspace;
        private bool IsInView = true;
        private bool HasSavedChanges = true;
        private bool HasContentRendered = false;
        private readonly XmlDocumentHandler XmlDocumentHandler;
        private readonly JObject Library;
        private readonly string CurrentFileName = "Untitled.layout";

        public MainEditor(string fileName = null)
        {
            this.InitializeComponent();
            this.Library = JObject.Parse(Utility.LoadInternalFile.TextFile("Library.json"));
            this.XmlDocumentHandler = new(ref this.XMLViewer, ref this.WorkspaceCanvas);
            this.ViewBox.MouseLeave += this.XmlDocumentHandler.MouseLeave;
            this.ViewBox.MouseEnter += this.XmlDocumentHandler.MouseEnter;
            this.ViewBox.MouseMove += this.XmlDocumentHandler.MouseMove;
            this.XmlDocumentHandler.ChangesMade += this.VisualsChangedEvent;
            this.XmlDocumentHandler.ChangesSaved += this.VisualsSavedEvent;

            fileName ??= "C:\\Users\\Matthew\\AppData\\Roaming\\Axolot Games\\Scrap Mechanic\\User\\User_76561198299556567\\Mods\\Challenge Mode in Creative With Mod Support\\Gui\\Layouts\\ChallengeModeMenuPack2.layout";

            if (fileName is not null)
            {
                Stopwatch stopWatch = new();
                stopWatch.Start();
                this.CurrentFileName = fileName;
                this.XmlDocumentHandler.LoadFile(fileName.Replace("ChallengeModeMenuPack2", "ChallengeModeMenuPack"));
                MainWindow.Get.SetCurrentTabName(Path.GetFileNameWithoutExtension(this.CurrentFileName));
                stopWatch.Stop();
                Debug.WriteLine(stopWatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Event handler for when the resizing of the properties window has started
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizePropMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton.Equals(MouseButton.Left))
            {
                this.MouseStart = MouseUtil.GetMousePosition();
                this.ResizingProperties = true;
            }
        }

        /// <summary>
        /// Event handler for when the resizing of the editor window has started
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizeEditorMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton.Equals(MouseButton.Left))
            {
                this.MouseStart = MouseUtil.GetMousePosition();
                this.ResizingEditor = true;
            }
        }

        /// <summary>
        /// this is the huge (it does a lot of stuff) MouseMove event handler,
        /// that triggers when the mouse moves throughout the entire window of the appliation.
        /// this handles workspace movement, ActiveElement scaling, and the split view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WindowMouseMove(object sender, MouseEventArgs e)
        {
            if (this.IsInView)
            {
                if (this.ResizingProperties)
                {
                    // Clamp the the workspace and properties tab min/max size
                    var size = Math.Clamp(
                        this.PropertiesBox.Width - e.GetPosition(this.ResizeProp).X,
                        200,
                        this.ActualWidth - 500);
                    // Move the viewbox
                    // this.ViewBox.SetMarginR(size);
                    // Move the properties window
                    if (this.PropertiesBox.Width != size)
                        this.PropertiesBox.Width = size;
                    // Move the actual drag button/bar
                    // ( the button in between the windows that controls this )
                    // this.ResizeProp.SetMarginR(size - 5);
                    // Clamp the workspace
                    this.ClampWorkspace();
                }
                else if (this.ResizingEditor)
                {
                    // Clamp the the workspace and properties tab min/max size
                    var size = Math.Clamp(
                        this.LowwerWorkspace.Height - e.GetPosition(this.ResizeEditor).Y,
                        48,
                        this.ActualHeight - 88) + 1;
                    // Change the this.UpperWorkspace height
                    // this.UpperWorkspace.SetMarginB(size);
                    // Change the lowwerworkspace height
                    if (this.LowwerWorkspace.Height != size)
                        this.LowwerWorkspace.Height = size;

                    if ((size < 50 && this.LowwerWorkspace.Height > 5) || size > this.ActualHeight - 90)
                    {
                        this.XMLViewer.Visibility = size < 50 ? Visibility.Collapsed : Visibility.Visible;
                        this.UpperWorkspace.Visibility = size < 50 ? Visibility.Visible : Visibility.Collapsed;
                        this.LowwerWorkspace.Height = size < 50 ? 5 : this.ActualHeight - 50;
                    }
                    else
                    {
                        if (this.XMLViewer.Visibility != Visibility.Visible)
                        {
                            this.XMLViewer.Visibility = Visibility.Visible;
                        }
                        if (this.UpperWorkspace.Visibility != Visibility.Visible)
                        {
                            this.UpperWorkspace.Visibility = Visibility.Visible;
                        }
                    }

                    // Clamp the workspace position
                    this.ClampWorkspace();
                }
                else if (this.MovingWorkspace)
                {
                    // calcualte the mouse movement and factor in the mouse sensitivity
                    var diff = this.GetMouseMovement(true, true);
                    // Adjust Position and clamp
                    this.WorkspaceCanvas.SetMarginLT(
                        Math.Clamp(this.WorkspaceCanvas.Margin.Left + diff.X, -(this.WorkspaceCanvas.ActualWidth * Scale - 90), this.ViewBox.ActualWidth - 90),
                        Math.Clamp(this.WorkspaceCanvas.Margin.Top + diff.Y, -(this.WorkspaceCanvas.ActualHeight * Scale - 60), this.ViewBox.ActualHeight - 60));

                    this.XmlDocumentHandler.UpdateOverlay();
                }
            }
        }

        private void VisualsChangedEvent(object sender = null, object args = null)
        {
            MainWindow.Get.SetCurrentTabName(Path.GetFileNameWithoutExtension(this.CurrentFileName) + "*");

            if (this.HasSavedChanges)
                this.HasSavedChanges = false;
        }

        private void VisualsSavedEvent(object sender = null, object args = null)
        {
            MainWindow.Get.SetCurrentTabName(Path.GetFileNameWithoutExtension(this.CurrentFileName));
            
            if (!this.HasSavedChanges)
            {
                if (!File.Exists(this.CurrentFileName))
                {
                    SaveFileDialog sd = new()
                    {
                        DefaultExt = "layout",
                        Filter = "Layout Files (*.layout)|*.layout",
                        AddExtension = true,
                        CheckPathExists = true,
                        FileName = Path.GetFileName(this.CurrentFileName),
                        OverwritePrompt = true,
                        Title = "Save File As"
                    };
                    if (sd.ShowDialog() == true) this.XmlDocumentHandler.SaveAs(sd.FileName); else return;
                }
                this.HasSavedChanges = true;
                this.XmlDocumentHandler.SaveAs(this.CurrentFileName);
            }
        }

        /// <summary>
        /// Simple event handler for any key down, ONLY inside the viewbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Simple delete method for deleting the selected item
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                foreach (var child in this.XmlDocumentHandler.GetSelected())
                    this.RecursiveDelete(this.WorkspaceCanvas, child);

                this.XmlDocumentHandler.DeleteSelected();
            }
            else if (
                (CtrlKeyDown && Keyboard.IsKeyDown(Key.S)) ||
                (e.Key == Key.S && CtrlKeyDown))
            {
                this.VisualsSavedEvent();
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Up:
                    case Key.PageUp:
                        this.XmlDocumentHandler.MoveSelected(new Point(0,-1));
                        break;
                    case Key.Left:
                        this.XmlDocumentHandler.MoveSelected(new Point(-1, 0));
                        break;
                    case Key.Down:
                    case Key.PageDown:
                        this.XmlDocumentHandler.MoveSelected(new Point(0, 1));
                        break;
                    case Key.Right:
                        this.XmlDocumentHandler.MoveSelected(new Point(1, 0));
                        break;
                    case Key.Z:
                        if (!CtrlKeyDown)
                            break;
                        this.XmlDocumentHandler.Undo();
                        break;
                    case Key.Y:
                        if (!CtrlKeyDown)
                            break;
                        this.XmlDocumentHandler.Redo();
                        break;
                }
            }
        }

        private static bool CtrlKeyDown => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

        public void GridPreviewKeyUp(object sender, KeyEventArgs e)
        { }

        /// <summary>
        /// This is the event for the scoll wheel, ONLY when the mouse is inside the viewbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (CtrlKeyDown && e.Delta != 0)
                this.ChangeScale(Math.Clamp(Scale + ((float)e.Delta / 5000), 0.02, 5));
        }

        /// <summary>
        /// Changes Scale of main grid
        /// </summary>
        /// <param name="newScale">New Scale</param>
        private void ChangeScale(double newScale)
        {
            // check for content render
            if (this.HasContentRendered)
            {
                // calcualte the positional offset for Left and Top
                double wDiff = (this.WorkspaceCanvas.ActualWidth * this.Scale / 2) - (this.WorkspaceCanvas.ActualWidth * newScale / 2);
                double hDiff = (this.WorkspaceCanvas.ActualHeight * this.Scale / 2) - (this.WorkspaceCanvas.ActualHeight * newScale / 2);
                // adjust position and clamp
                this.WorkspaceCanvas.SetMarginLT(
                    Math.Clamp(this.WorkspaceCanvas.Margin.Left + wDiff, -(this.WorkspaceCanvas.ActualWidth * newScale - 90), this.ViewBox.ActualWidth - 90),
                    Math.Clamp(this.WorkspaceCanvas.Margin.Top + hDiff, -(this.WorkspaceCanvas.ActualHeight * newScale - 60), this.ViewBox.ActualHeight - 60));
            }
            // set new scale
            this.Scale = newScale;
        }

        /// <summary>
        /// This is the mouse down event for the viewbox, and only the viewbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (
                e.LeftButton != MouseButtonState.Pressed &&
                e.MiddleButton == MouseButtonState.Pressed &&
                e.RightButton != MouseButtonState.Pressed)
            {
                this.MouseStart = MouseUtil.GetMousePosition();
                this.XmlDocumentHandler.HighlightBorder();
                this.MovingWorkspace = true;
                e.Handled = true;
            }
            else
            {
                this.OffClickDetection(sender, e);
            }
        }

        public void MenuItemClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            this.XmlDocumentHandler.AddXmlElement(XmlTag.Widget, Enum.Parse<XmlType>(menuItem.Name));
        }

        public void OffClickDetection(object sender, MouseButtonEventArgs e)
        {
            // invoke the xmldochandler
            this.XmlDocumentHandler.DeselectAll();
        }

        /// <summary>
        /// This limits the maximium and minimum position (Left and Right) for the Workspace
        /// </summary>
        public void ClampWorkspace()
        {
            double left = Math.Clamp(
                    this.WorkspaceCanvas.Margin.Left,
                    -(this.WorkspaceCanvas.ActualWidth * Scale - 90),
                    this.ViewBox.ActualWidth - 90);
            double top = Math.Clamp(
                    this.WorkspaceCanvas.Margin.Top,
                    -(this.WorkspaceCanvas.ActualHeight * Scale - 60),
                    this.ViewBox.ActualHeight - 60);

            if(!this.WorkspaceCanvas.Margin.Left.Equals(left))
                this.WorkspaceCanvas.SetMarginL(left);

            if (!this.WorkspaceCanvas.Margin.Top.Equals(top))
                this.WorkspaceCanvas.SetMarginT(top);
        }

        /// <summary>
        /// This limits the maximium and minimum position (Left and Right) for the Tabs
        /// </summary>
        public void ClampTabs()
        {
            // Clamp the the workspace and properties tab min/max size
            var size = Math.Clamp(
                this.PropertiesBox.Width,
                Math.Min(200, this.ActualWidth - 500),
                this.ActualWidth - 500);
            // Move the viewbox
            //if(!this.ViewBox.Margin.Right.Equals(size))
            //    this.ViewBox.SetMarginR(size);
            // Move the properties window
            if (!this.PropertiesBox.Width.Equals(size - 15))
                this.PropertiesBox.Width = size - 15;
            // Move the actual drag button/bar
            // ( the button in between the windows that controls this )
            //if (!this.ResizeProp.Margin.Right.Equals(size - 5))
            //    this.ResizeProp.SetMarginR(size - 5);
            // Clamp the the workspace and properties tab min/max size
            
            size = Math.Clamp(
                this.LowwerWorkspace.Height,
                48,
                this.ActualHeight - 88) + 1;
            // Change the this.UpperWorkspace height
            //if (!this.UpperWorkspace.Margin.Bottom.Equals(size))
            //    this.UpperWorkspace.SetMarginB(size);
            // Change the lowwerworkspace height
            if (!this.LowwerWorkspace.Height.Equals(size))
                this.LowwerWorkspace.Height = size;
            /*
            if ((size < 50 && this.LowwerWorkspace.Height > 5) || size > this.ActualHeight - 90)
            {
                this.XMLViewer.Visibility = size < 50 ? Visibility.Collapsed : Visibility.Visible;
                this.UpperWorkspace.Visibility = size < 50 ? Visibility.Visible : Visibility.Collapsed;
                this.LowwerWorkspace.Height = size < 50 ? 5 : this.ActualHeight - 50;
            }
            else
            {
                if (this.XMLViewer.Visibility != Visibility.Visible)
                {
                    this.XMLViewer.Visibility = Visibility.Visible;
                }
                if (this.UpperWorkspace.Visibility != Visibility.Visible)
                {
                    this.UpperWorkspace.Visibility = Visibility.Visible;
                }
            }*/

            if (this.UpperWorkspace.Visibility != Visibility.Visible)
            {
                this.LowwerWorkspace.Height = this.ActualHeight - 50;
            }

            if (this.XMLViewer.Visibility != Visibility.Visible)
            {
                this.LowwerWorkspace.Height = 5;
            }

            // Clamp the workspace position
            this.ClampWorkspace();
        }

        /// <summary>
        /// This is the mouse up event for the viewbox, and only the viewbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EntireAppPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // reset moving state
            if (e.ChangedButton.Equals(MouseButton.Middle)) this.MovingWorkspace = false;
            if (e.ChangedButton.Equals(MouseButton.Left))
            {
                this.ResizingEditor = false;
                this.ResizingProperties = false;
            }
            this.XmlDocumentHandler.MouseUp(sender, e);
        }

        private void RecursiveDelete(UIElement elemt, UIElement target)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(elemt); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(elemt, i);
                if (child is Grid && child.Equals(target))
                {
                    (VisualTreeHelper.GetParent(child) as Grid).Children.Remove(target);
                }
                else if (child is Grid)
                {
                    this.RecursiveDelete(child as Grid, target);
                }
            }
        }

        public void SetResolution(double width, double height, object optional = null)
        {
            this.Resolution = new(width, height);
            if (this.HasContentRendered)
            {
                if (optional == null)
                {
                    this.WorkspaceCanvas.SetWidthAndHeight(width, height);

                    double NewScaleH = Scale, NewScaleW = Scale;
                    if (width > this.ViewBox.ActualWidth)
                        NewScaleW = (this.ViewBox.ActualWidth - 50) / width;
                    if (height > this.ViewBox.ActualHeight)
                        NewScaleH = (this.ViewBox.ActualHeight - 50) / height;

                    this.ChangeScale(Math.Min(NewScaleH, NewScaleW));

                    this.WorkspaceCanvas.SetMarginLT(
                        (this.ViewBox.ActualWidth - width * Scale) / 2,
                        (this.ViewBox.ActualHeight - height * Scale) / 2);
                }
                else
                {
                    var w = optional as Tuple<Tuple<double, double>, Tuple<double, double>>;
                    this.WorkspaceCanvas.SetMarginLT(w.Item1.Item1, w.Item1.Item2);
                    this.WorkspaceCanvas.SetWidthAndHeight(w.Item2.Item1, w.Item2.Item2);
                }
            }
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            this.HasContentRendered = true;
            this.SetResolution(this.Resolution.X, this.Resolution.Y);
        }

        private void ViewModeClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            this.XmlDocumentHandler.ChangeViewMode((XmlViewMode)Int32.Parse(button.Tag.ToString()));
        }

        readonly Dictionary<Key, KeyStates> KeyStates = new();
        readonly Dictionary<MouseButton, MouseButtonState> MouseStates = new();
        private void ViewBoxMouseEnter(object sender, MouseEventArgs e)
        {
            this.IsInView = true;
            //Dictionary<Key, KeyStates> _KeyStates = new();
            //foreach (Key k in Enum.GetValues(typeof(Key)))
            //{
            //    _KeyStates.Add(k, Keyboard.GetKeyStates(k));
            //}
            Dictionary<MouseButton, MouseButtonState> _MouseStates = new();
            foreach (MouseButton m in Enum.GetValues(typeof(MouseButton)))
            {
                _MouseStates.Add(m, Mouse.PrimaryDevice.GetButtonState(m));
            }
            if (!_MouseStates.Equals(this.MouseStates))
            {
                foreach(KeyValuePair<MouseButton, MouseButtonState> Keys in _MouseStates)
                {
                    if (this.MouseStates.ContainsKey(Keys.Key))
                    {
                        if (!Keys.Value.Equals(this.MouseStates[Keys.Key]) && Keys.Value.Equals(MouseButtonState.Released))
                        {
                            this.EntireAppPreviewMouseUp(this, new(Mouse.PrimaryDevice, 0, Keys.Key));
                        }
                    }
                    else
                    {
                        this.MouseStates.Add(Keys.Key, Keys.Value);
                    }
                }
            }
        }

        /// <summary>
        /// This event is for when the mouse leaves the viewbox 
        /// although the mouse could still be in the application window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewBoxMouseLeave(object sender, MouseEventArgs e)
        {
            this.IsInView = false;
            foreach (Key k in Enum.GetValues(typeof(Key)))
            {
                if (k.Equals(Key.None)) continue;

                if (this.KeyStates.ContainsKey(k))
                    this.KeyStates[k] = Keyboard.GetKeyStates(k);
                else
                    this.KeyStates.Add(k, Keyboard.GetKeyStates(k));
            }
            foreach (MouseButton m in Enum.GetValues(typeof(MouseButton)))
            {
                if (this.MouseStates.ContainsKey(m))
                    this.MouseStates[m] = Mouse.PrimaryDevice.GetButtonState(m);
                else
                    this.MouseStates.Add(m, Mouse.PrimaryDevice.GetButtonState(m));
            }
        }

        private void UpperWorkspace_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.XmlDocumentHandler.UnSelectTextBox();
        }

        private void LowwerWorkspace_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (CtrlKeyDown)
            {
                this.XmlDocumentHandler.AddTextSize(e.Delta);
                e.Handled = true;
            }
        }
    }
}
