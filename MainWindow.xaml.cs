using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Newtonsoft.Json.Linq;
using CustomExtensions;
using System.Diagnostics;
using Xceed.Wpf.AvalonDock;
using MyToolkit.Mvvm;
using System.ComponentModel;
using System.Linq;
using MyToolkit.UI;
using MyToolkit.Utilities;
using SM_Layout_Editor.Utilities;
using SM_Layout_Editor.Windows;
using Microsoft.Win32;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Path = System.IO.Path;
using Gameloop.Vdf.JsonConverter;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SM_Layout_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow Get;

        public static Point MouseStart;
        private static double g_scale = 1;
        public static double Scale {
            get { 
                return g_scale;
            }
            set {
                g_scale = value;
                Get.ZoomDisplay.Text = (Math.Floor(g_scale*100)/100).ToString() + "x";
            }
        }
        public static int MoveSensitivity = 1;
        public static int ZoomSensitivity = 1;
        public static int GridSize = 50;
        public static Tuple<int, int> Resolution;

        private bool ResizingProperties;
        private bool ResizingEditor;
        private bool MovingWorkspace;
        private static bool d_item = false;
        private bool DraggingSubMenuItem
        {
            get
            {
                return d_item;
            }
            set
            {
                d_item = value;
                Debug.WriteLine(value);
            }
        }
        private bool SelectedItemDoubleClick;
        private int ItemScaleMode = -1;

        private readonly Thickness[] adjustSubItem = new Thickness[]
        {
            new Thickness(1,1,0,0),
            new Thickness(0,1,0,0),
            new Thickness(0,1,1,0),
            new Thickness(0,0,1,0),
            new Thickness(0,0,1,1),
            new Thickness(0,0,0,1),
            new Thickness(1,0,0,1),
            new Thickness(1,0,0,0)
        };

        private MenuState WindowMenu = MenuState.Closed;
        private JObject Library;
        private Grid ActiveElement = null;
        private Rectangle WorkspaceRec;
        private Grid Properites;

        private string SteamPath;
        private string GamePath;
        private bool Installed;
        private bool Running;
        private bool Updating;
        private string SteamDisplayName;
        private string Language;

        private ApplicationConfiguration _configuration;
        private MenuItem prevItem = null;
        private bool HasContentRendered = false;

        /// <summary>
        /// Initializaer for the thing and stuff
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            Library = JObject.Parse(Utility.LoadInternalFile.TextFile("Library.json"));
            WorkspaceRec = (Rectangle)Workspace.Children[0];
            Properites = (Grid)PropertiesBox.Children[1];
            Get = this;

            Closing += OnWindowClosing;

            // STEAM STUFF

            SteamPath = Utility.GetRegVal<string>("Software\\Valve\\Steam", "SteamPath").ToValidPath();
            Installed = Utility.GetRegVal<bool>("Software\\Valve\\Steam\\Apps\\387990", "Installed");
            Updating = Utility.GetRegVal<bool>("Software\\Valve\\Steam\\Apps\\387990", "Updating");
            Running = Utility.GetRegVal<bool>("Software\\Valve\\Steam\\Apps\\387990", "Running");
            SteamDisplayName = Utility.GetRegVal<string>("Software\\Valve\\Steam", "LastGameNameUsed");
            Language = Utility.GetRegVal<string>("Software\\Valve\\Steam", "Language").CapitilzeFirst();

            if (!Directory.Exists(Path.Combine(SteamPath, "steamapps", "common", "Scrap Mechanic"))) {
                VProperty VDF_Object = VdfConvert.Deserialize(File.ReadAllText(Path.Combine(SteamPath, "steamapps", "libraryfolders.vdf")));
                JObject GameInstallationPaths = (JObject)JObject.Parse("{" + VDF_Object.ToJson().ToString() + "}")["LibraryFolders"];
                int index = 1;
                while (GameInstallationPaths.ContainsKey(index.ToString()))
                {
                    string smPath = Path.Combine(GameInstallationPaths[index.ToString()].ToString(), "steamapps", "common", "Scrap Mechanic");
                    if (Directory.Exists(smPath))
                    {
                        GamePath = smPath;
                        break;
                    }
                    index++;
                }
            }
            else
            {
                GamePath = Path.Combine(SteamPath, "steamapps", "common", "Scrap Mechanic");
            }

            Debug.WriteLine(GamePath);

            LoadConfiguration();

            LoadToolBoxItems();
        }
        /// <summary>
        /// Event handler for window resize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClampWorkspace();
        }
        /// <summary>
        /// Event handler for when the resizing of the properties window has started
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizeProp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseUtil.ResetMousePos();
            ResizingProperties = true;
        }
        /// <summary>
        /// Event handler for when the resizing of the properties window has stopped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizeProp_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ResizingProperties = false;
        }
        /// <summary>
        /// Event handler for when the resizing of the editor window has started
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizeEditor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseUtil.ResetMousePos();
            ResizingEditor = true;
        }
        /// <summary>
        /// Event handler for when the resizing of the editor window has stopped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizeEditor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ResizingEditor = false;
        }
        /// <summary>
        /// this is the huge (it does a lot of stuff) MouseMove event handler,
        /// that triggers when the mouse moves throughout the entire window of the appliation.
        /// this handles workspace movement, ActiveElement scaling, and the split view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (ResizingProperties)
            {
                // Clamp the the workspace and properties tab min/max size
                var size = Math.Clamp(
                    ViewBox.Margin.Right - e.GetPosition(ResizeProp).X,
                    50,
                    ActualWidth - 50);
                // Move the viewbox
                ViewBox.SetMarginR(size);
                // Move the properties window
                PropertiesBox.Width = size-15;
                // Move the actual drag button/bar
                // ( the button in between the windows that controls this )
                ResizeProp.SetMarginR(size - 5);
                // Clamp the workspace
                ClampWorkspace();
            }
            if (ResizingEditor)
            {
                // Clamp the the workspace and properties tab min/max size
                var size = Math.Clamp(
                    UpperWorkspace.Margin.Bottom - e.GetPosition(ResizeEditor).Y,
                    50,
                    ActualHeight - 100);
                // Change the upperworkspace height
                UpperWorkspace.SetMarginB(size);
                // Change the lowwerworkspace height
                LowwerWorkspace.Height = UpperWorkspace.Margin.Bottom - 15;
                // Clamp the workspace position
                ClampWorkspace();
            }
            if (MovingWorkspace)
            {
                // calcualte the mouse movement and factor in the mouse sensitivity
                var diff = MouseUtil.GetMouseMovement(true, true);
                // Adjust Position and clamp
                Workspace.SetMarginLT(
                    Math.Clamp(Workspace.Margin.Left + diff.X, -(Workspace.ActualWidth * Scale - 90), ViewBox.ActualWidth - 90),
                    Math.Clamp(Workspace.Margin.Top + diff.Y, -(Workspace.ActualHeight * Scale - 60), ViewBox.ActualHeight - 60));
                ClampWorkspace();
            }
            if (DraggingSubMenuItem)
            {
                // calcualte the mouse movement and factor in the mouse sensitivity, the scale, and the grid size
                var diff = MouseUtil.GetMouseMovement(false, true, true, true);
                // adjust position and clamp
                if ((int)diff.X != 0 || (int)diff.Y != 0)
                {
                    ActiveElement.SetMarginLT(
                        Math.Clamp(ActiveElement.Margin.Left + (diff.X * GridSize), 0, Math.Clamp(Workspace.ActualWidth - ActiveElement.ActualWidth, 0, 999999)),
                        Math.Clamp(ActiveElement.Margin.Top + (diff.Y * GridSize), 0, Math.Clamp(Workspace.ActualHeight - ActiveElement.ActualHeight, 0, 999999)));
                    MouseUtil.ResetMousePos();
                }
                // Update the position
                UpdateItemControlPos();
            }
            // if the item scale mode is zero or greater, then we are scaling
            if (ItemScaleMode > -1)
            {
                // calcualte the mouse movement and factor in the mouse sensitivity, the scale, and the grid size
                var diff = MouseUtil.GetMouseMovement(false, true, true, true);
                // make sure the change in mouse movement is greater than the grid size
                // the difference will allways be less than zero, if the grid size is greater
                // than the difference
                if ((int)diff.X != 0 || (int)diff.Y != 0)
                {
                    // complex math use to calcuate the exact scaling mode, and offsets
                    if (ItemScaleMode != 1 && ItemScaleMode != 5)
                        if (Math.Clamp(ItemScaleMode, 6, 7) == ItemScaleMode || ItemScaleMode == 0)
                            if (diff.X != Math.Abs(diff.X))
                            {
                                ActiveElement.SetMarginL(Math.Clamp(ActiveElement.Margin.Left + (diff.X * GridSize), 0, Workspace.ActualWidth));
                                ActiveElement.Width = Math.Clamp(ActiveElement.ActualWidth + (Math.Abs(diff.X) * GridSize), GridSize, Workspace.ActualWidth);
                            }
                            else
                            {
                                ActiveElement.Width = Math.Clamp(ActiveElement.ActualWidth + (-diff.X * GridSize), GridSize, Workspace.ActualWidth);
                                ActiveElement.SetMarginL(Math.Clamp(ActiveElement.Margin.Left + (diff.X * GridSize), 0, Workspace.ActualWidth));
                            }
                        else
                            ActiveElement.Width = Math.Clamp(ActiveElement.ActualWidth + (diff.X * GridSize), GridSize, Workspace.ActualWidth);
                    if (ItemScaleMode != 3 && ItemScaleMode != 7)
                        if (Math.Clamp(ItemScaleMode, 0, 2) == ItemScaleMode)
                            if (diff.Y != Math.Abs(diff.Y))
                            {
                                ActiveElement.SetMarginT(Math.Clamp(ActiveElement.Margin.Top + (diff.Y * GridSize), 0, Workspace.ActualHeight));
                                ActiveElement.Height = Math.Clamp(ActiveElement.ActualHeight + (Math.Abs(diff.Y) * GridSize), GridSize, Workspace.ActualHeight);
                            }
                            else
                            {
                                ActiveElement.Height = Math.Clamp(ActiveElement.ActualHeight + (-diff.Y * GridSize), GridSize, Workspace.ActualHeight);
                                ActiveElement.SetMarginT(Math.Clamp(ActiveElement.Margin.Top + (diff.Y * GridSize), 0, Workspace.ActualHeight));
                            }
                        else
                            ActiveElement.Height = Math.Clamp(ActiveElement.ActualHeight + (diff.Y * GridSize), GridSize, Workspace.ActualHeight);
                    // Update the mouse pos
                    MouseUtil.ResetMousePos();
                }
                // Clamp the workspace position
                ClampWorkspace();
            }
        }

        private void ClampActiveElementScale()
        {
            if (ActiveElement != null)
            {
                ActiveElement.SetMarginL(Math.Clamp(ActiveElement.Margin.Left + (GridSize), 0, Workspace.ActualWidth));
                ActiveElement.Width = Math.Clamp(ActiveElement.ActualWidth + (GridSize), GridSize, Workspace.ActualWidth);
                ActiveElement.SetMarginT(Math.Clamp(ActiveElement.Margin.Top + (GridSize), 0, Workspace.ActualHeight));
                ActiveElement.Height = Math.Clamp(ActiveElement.ActualHeight + (GridSize), GridSize, Workspace.ActualHeight);
            }
        }

        private void UpdateItemControlPos()
        {
            if (ActiveElement != null)
            {
                ContentControl cc = Utility.FindContentControl("ControlButtonTemplate");
                Thickness m = ActiveElement.Margin;
                Thickness w = Workspace.Margin;
                m = new Thickness(m.Left * Scale, m.Top * Scale, m.Right * Scale, m.Bottom * Scale);
                m = new Thickness(m.Left + w.Left, m.Top + w.Top, m.Right + w.Right, m.Bottom + w.Bottom);
                cc.SetMargin(m);
                ((Grid)cc.Content).SetWidthAndHeight(ActiveElement.ActualWidth * Scale, ActiveElement.ActualHeight * Scale);
            }
        }

        /// <summary>
        /// MouseDown event for when a UI elemenent inside the workspace has been selected
        /// this element becomes the ActiveElement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed && e.RightButton != MouseButtonState.Pressed)
            {
                // when the item is selected, we are in a "dragging" state
                DraggingSubMenuItem = true;
                // make sure we are not moving the workspace, and just the item
                MovingWorkspace = false;
                // reset scaling mode
                ItemScaleMode = -1;
                // remove highlighting effect
                WorkspaceRec.Stroke = Brushes.Gray;
                // set the active element
                ActiveElement = (Grid)sender;
                // the control button template, is the template for the scaling overlay
                var cc = Utility.FindContentControl("ControlButtonTemplate");
                // add double click event handler for the overlay
                cc.MouseDoubleClick += Item_MouseDoubleClick;
                // set the width and height of the overlay
                ((Grid)cc.Content).SetWidthAndHeight(ActiveElement.ActualWidth, ActiveElement.ActualHeight);
                // finally, add the overlay to the selected item
                ViewBox.Children.Add(cc);
                // this will build and add items to the properties tab
                /* Properites.Children.Add(
                    GUI.Builder.BuildPropertiesItem(
                        ActiveElement.Tag.ToString(),
                        Tb_TextChanged));
                */
                // reset starting mouse position
                MouseUtil.ResetMousePos();                
            }
        }
        /// <summary>
        /// Simple mouse event for the MouseUp event, after dragging the ActiveElement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
                DraggingSubMenuItem = false;
        }
        /// <summary>
        /// This closes the menu, when the mouse clicks off of the menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Menu_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // change states and visibilities
            WindowMenu = MenuState.Closed;
            OffClickDetection.Visibility = Visibility.Hidden;
            // OffClickDetection is also used for items
            // it will remove the ActiveElement and reset other various varaibles
            // such as clearing properties menu, and removing the scaling overlay
            ContentControl cc = Utility.FindContentControl("ControlButtonTemplate");
            if (ActiveElement != null && e.MiddleButton != MouseButtonState.Pressed && ItemScaleMode == -1)
            {
                ViewBox.Children.Remove(cc);
                // Properites.Children.Clear();
                // ActiveElement = null;
            }
        }
        /// <summary>
        /// This is the event for the scoll wheel, ONLY when the mouse is inside the viewbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Delta != 0)
            {
                ChangeScale(Math.Clamp(Scale + ((float)e.Delta / 3500), 0.1, 4));
            }
        }
        /// <summary>
        /// Changes scale of main grid
        /// </summary>
        /// <param name="newScale">New Scale</param>
        private void ChangeScale(double newScale)
        {
            // load previous scale for comparison
            var prev = Scale;
            // calcualte scale with pre-set sensitivity of 2500
            // this can be changed to affect the zoom sensitivity
            Scale = newScale;
            // scale the workspace
            Workspace.RenderTransform = new ScaleTransform()
            {
                ScaleX = Scale,
                ScaleY = Scale
            };
            // scale boarder
            WorkspaceRec.StrokeThickness = 3 / Scale;
            // calcualte the positional offset for Left and Top
            var wDiff = (((Workspace.ActualWidth * Scale) - Workspace.ActualWidth) / 2) -
                (((Workspace.ActualWidth * prev) - Workspace.ActualWidth) / 2);
            var hDiff = (((Workspace.ActualHeight * Scale) - Workspace.ActualHeight) / 2) -
                (((Workspace.ActualHeight * prev) - Workspace.ActualHeight) / 2);
            // adjust position and clamp
            Workspace.SetMarginLT(
                Math.Clamp(Workspace.Margin.Left - wDiff, -(Workspace.ActualWidth * Scale - 90), ViewBox.ActualWidth - 90),
                Math.Clamp(Workspace.Margin.Top - hDiff, -(Workspace.ActualHeight * Scale - 60), ViewBox.ActualHeight - 60));
            ClampWorkspace();
        }
        /// <summary>
        /// This is the mouse down event for the viewbox, and only the viewbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // if user is not DraggingSubMenuItem
            if ((
                (!DraggingSubMenuItem && e.LeftButton == MouseButtonState.Pressed)
                || e.MiddleButton == MouseButtonState.Pressed)
                && e.RightButton != MouseButtonState.Pressed)
            {
                if (ActiveElement != null && e.MiddleButton != MouseButtonState.Pressed)
                    ViewBox.Children.Remove(Utility.FindContentControl("ControlButtonTemplate"));
                Menu_PreviewMouseDown(sender, e);
                MouseUtil.ResetMousePos();
                WorkspaceRec.Stroke = Brushes.White;
                MovingWorkspace = true;
            }
        }
        /// <summary>
        /// This is the mouse up event for the viewbox, and only the viewbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // remove highlighting effect
            WorkspaceRec.Stroke = Brushes.Gray;
            // reset moving state
            MovingWorkspace = false;
            // reset scaling mode
            ItemScaleMode = -1;
        }
        /// <summary>
        /// This event is for when the mouse leaves the viewbox 
        /// although the mouse could still be in the application window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewBox_MouseLeave(object sender, MouseEventArgs e)
        {
            MovingWorkspace = false;
            DraggingSubMenuItem = false;
        }
        /// <summary>
        /// This limits the maximium and minimum position (Left and Right) for the Workspace
        /// </summary>
        private void ClampWorkspace()
        {
            Workspace.SetMarginLT(
                Math.Clamp(
                    Workspace.Margin.Left,
                    -(Workspace.ActualWidth * Scale - 90),
                    ViewBox.ActualWidth - 90),
                Math.Clamp(
                    Workspace.Margin.Top,
                    -(Workspace.ActualHeight * Scale - 60),
                    ViewBox.ActualHeight - 60));
            // Update the position and scale and w/h
            UpdateItemControlPos();
        }
        /// <summary>
        /// Double click event for workspace items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectedItemDoubleClick = true;
        }
        /// <summary>
        /// Event for scaling. This handles the 8 scale methods
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SubItemControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Disbale moving, because we are scaling, not moving
            DraggingSubMenuItem = false;
            // disable this bc
            Grid_PreviewMouseUp(sender, e);
            // Fetch the scale mode
            var button = (Button)sender;
            ItemScaleMode = int.Parse(button.Tag.ToString());
            // the control button template, is the template for the scaling overlay
            var cc = Utility.FindContentControl("ControlButtonTemplate");
            if (!ViewBox.Children.Contains(cc))
            {
                // add double click event handler for the overlay
                cc.MouseDoubleClick += Item_MouseDoubleClick;
                // set the width and height of the overlay
                ((Grid)cc.Content).SetWidthAndHeight(ActiveElement.ActualWidth, ActiveElement.ActualHeight);
                // finally, add the overlay to the selected item
                ViewBox.Children.Add(cc);
            }
            // Update the mouse starting position
            MouseUtil.ResetMousePos();
        }
        /// <summary>
        /// Simple event handler for any key down, ONLY inside the viewbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Simple delete method for deleting the selected item
            if (e.Key == Key.Delete && ActiveElement != null)
            {
                Workspace.Children.Remove(ActiveElement);
                ViewBox.Children.Remove(Utility.FindContentControl("ControlButtonTemplate"));
            }
        }
        private async Task LoadConfiguration()
        {
            _configuration = JsonApplicationConfiguration.Load<ApplicationConfiguration>("SM_Layout_Editor/Config", true, true);
            
            if(_configuration.Top > 0)
                Top = _configuration.Top;
            if (_configuration.Left > 0)
                Left = _configuration.Left;
            if (_configuration.WindowWidth > 0)
                Width = _configuration.WindowWidth;
            if (_configuration.WindowHeight > 0)
                Height = _configuration.WindowHeight;
            if(_configuration.MoveSensitivity > 0)
                MoveSensitivity = _configuration.MoveSensitivity;
            if (_configuration.ZoomSensitivity > 0)
                ZoomSensitivity = _configuration.ZoomSensitivity;
            if (_configuration.GridSize > 0)
                GridSize = _configuration.GridSize;
            WindowState = _configuration.WindowState;

            if (_configuration.IsFirstStart)
                _configuration.IsFirstStart = false;

            while (!HasContentRendered) { await Task.Delay(100); }

            if(_configuration.Scale > 0)
                ChangeScale(_configuration.Scale);

            if (_configuration.Resolution != null)
                Resolution_Click(new Tuple<object, object>(_configuration.Resolution, _configuration.Workspace), null);
            else
                Resolution_Click(new Tuple<int, int>(1280, 720), null);

            var sws = _configuration.SubWindowSizing;
            if (sws != null)
            {
                ViewBox.SetMarginR(sws.Item1);
                PropertiesBox.Width = sws.Item2;
                ResizeProp.SetMarginR(sws.Item3);
                UpperWorkspace.SetMarginB(sws.Item4);
                LowwerWorkspace.Height = sws.Item5;
            }
        }
        private void SaveConfiguration()
        {
            _configuration.Left = Left;
            _configuration.Top = Top;
            _configuration.WindowWidth = Width;
            _configuration.WindowHeight = Height;
            _configuration.WindowState = WindowState;
            _configuration.Resolution = Resolution;
            _configuration.GridSize = GridSize;
            _configuration.Scale = Scale;
            _configuration.MoveSensitivity = MoveSensitivity;
            _configuration.ZoomSensitivity = ZoomSensitivity;
            Thickness m = Workspace.Margin;
            _configuration.Workspace = new(new(m.Left, m.Top), new(Workspace.ActualWidth, Workspace.ActualHeight));

            _configuration.SubWindowSizing = new(
                ViewBox.Margin.Right,
                PropertiesBox.Width,
                ResizeProp.Margin.Right,
                UpperWorkspace.Margin.Bottom,
                LowwerWorkspace.Height);

            JsonApplicationConfiguration.Save("SM_Layout_Editor/Config", _configuration, true);
        }
        private async void OnWindowClosing(object sender, CancelEventArgs args)
        {
            SaveConfiguration();

            args.Cancel = true;

            Closing -= OnWindowClosing;
  
            await Dispatcher.InvokeAsync(Close);
        }
        private void OnShowAboutWindow(object sender, RoutedEventArgs e)
        {

        }
        private void OnExitApplication(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            RefreshHighlighting();
            XML_Viewer.Text = File.ReadAllText(@"C:\Program Files (x86)\Steam\steamapps\common\Scrap Mechanic\Data\Gui\Layouts\Hud\Hud_SurvivalHud.layout");
            HasContentRendered = true;
        }
        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            
            if (overflowGrid != null)
                overflowGrid.Visibility = Visibility.Collapsed;

            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            
            if (mainPanelBorder != null)
                mainPanelBorder.Margin = new Thickness();
        }
        private void Resolution_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Tuple<int, int> || sender is Tuple<object, object>)
            {
                object tuple = sender;
                object additionalArgs = null;
                if (!(sender is Tuple<int, int>))
                {
                    Tuple<object, object> obj = sender as Tuple<object, object>;
                    tuple = obj.Item1 as Tuple<int, int>;
                    additionalArgs = obj.Item2;
                }

                Tuple<int, int> args = tuple as Tuple<int, int>;

                SetResolution(args.Item1, args.Item2, additionalArgs);

                foreach (MenuItem item in ResolutionSideMenu.Items)
                    if (item.Header.ToString().Contains(args.Item1 + "x" + args.Item2))
                    {
                        item.Background = Brushes.LightBlue;
                        prevItem = item;
                    }
            }
            else
            {
                MenuItem SelectedOption = sender as MenuItem;

                if (prevItem != null)
                    prevItem.Background = Brushes.Transparent;

                var resoultion = SelectedOption.Header.ToString().Split(" ")[0].Split("x");

                SetResolution(int.Parse(resoultion[0]), int.Parse(resoultion[1]));

                SelectedOption.Background = Brushes.LightBlue;

                prevItem = SelectedOption;
            }
        }
        private void SetResolution(int width, int height, object optional = null)
        {
            Resolution = new(width, height);
            if (optional == null)
            {
                Workspace.SetWidthAndHeight(width, height);

                double NewScaleH = Scale, NewScaleW = Scale;
                if (width > ViewBox.ActualWidth)
                    NewScaleW = (ViewBox.ActualWidth - 50) / width;
                if (height > ViewBox.ActualHeight)
                    NewScaleH = (ViewBox.ActualHeight - 50) / height;

                ChangeScale(Math.Min(NewScaleH, NewScaleW));

                Workspace.SetMarginLT(
                    (ViewBox.ActualWidth - width * Scale) / 2,
                    (ViewBox.ActualHeight - height * Scale) / 2);
            }
            else
            {
                var w = optional as Tuple<Tuple<double, double>, Tuple<double, double>>;
                Workspace.SetMarginLT(w.Item1.Item1, w.Item1.Item2);
                Workspace.SetWidthAndHeight(w.Item2.Item1, w.Item2.Item2);
            }
            ClampActiveElementScale();
            ClampWorkspace();
        }

        private void RefreshHighlighting()
        {
            XML_Viewer.SyntaxHighlighting = Utility.LoadInternalFile.HighlightingDefinition("HightlightingRules.xshd");
        }

        private void LoadToolBoxItems()
        {
            JObject json = JObject.Parse(Utility.LoadInternalFile.TextFile("MyGUI_Trace.json"));
            List<string> tbxi = new List<string>();
            foreach(JProperty item in json["Widget"])
            {
                tbxi.Add(item.Name.ToString());
            }
            tbxi.Sort();
            foreach(string item in tbxi)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Name = item;
                menuItem.Header = item;
                menuItem.Click += MenuItem_Click;
                ToolBoxDropDown.Items.Add(menuItem);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            Grid g = GUI.Builder.BuildToolBoxItem(menuItem.Name, Get, Item_PreviewMouseDown, Item_PreviewMouseUp);
            Workspace.Children.Add(g);
        }
    }
}
