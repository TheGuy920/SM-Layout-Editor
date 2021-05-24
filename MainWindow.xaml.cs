﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Newtonsoft.Json.Linq;
using CustomExtensions;

namespace SM_Layout_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Point MouseStart;
        public static double Scale = 1;
        public static int MoveSensitivity = 1;
        public static int ZoomSensitivity = 1;
        public static int GridSize = 10;

        private bool ResizingProperties;
        private bool MovingWorkspace;
        private bool DraggingSubMenuItem;
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
        private JObject MenuJson;
        private Grid ActiveElement = null;
        private Rectangle WorkspaceRec;
        private Grid Properites;
        public MainWindow()
        {
            InitializeComponent();
            JObject Default = JObject.Parse(Utility.ReadLocalResource("Fixed.json"));
            JObject Library = JObject.Parse(Utility.ReadLocalResource("Library.json"));
            JObject ExtendedLibrary = null;
            if (File.Exists("Library.json"))
                ExtendedLibrary = JObject.Parse(File.ReadAllText("Library.json"));
            Default.Merge(Library);
            if(ExtendedLibrary != null)
                Default.Merge(ExtendedLibrary);
            MenuJson = Default;
            WorkspaceRec = (Rectangle)Workspace.Children[0];
            Properites = (Grid)PropertiesBox.Children[1];
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
        private void ReSizeLiveSplit_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseStart = MouseUtil.GetMousePosition();
            ResizingProperties = true;
        }
        /// <summary>
        /// Event handler for when the resizing of the properties window has stopped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReSizeLiveSplit_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ResizingProperties = false;
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
                    ViewBox.Margin.Right - MouseUtil.GetMouseMovement().X,
                    50,
                    ActualWidth - 50);
                // Move the viewbox
                ViewBox.AddMargin(0,0,size,0);
                // Move the properties window
                PropertiesBox.Width = size-15;
                // Move the actual drag button/bar
                // ( the button in between the windows that controls this )
                ReSizeLiveSplit.AddMargin(0,0,size-5, 0);
                // Clamp the workspace
                ClampWorkspace();
            }
            if (MovingWorkspace)
            {
                // calcualte the mouse movement and factor in the mouse sensitivity
                var diff = MouseUtil.GetMouseMovement(true);
                // Adjust Position and clamp
                Workspace.SetMarginLT(
                    Math.Clamp(Workspace.Margin.Left + diff.X, -(Workspace.ActualWidth * Scale - 90), ViewBox.ActualWidth - 90),
                    Math.Clamp(Workspace.Margin.Top + diff.Y, -(Workspace.ActualHeight * Scale - 60), ViewBox.ActualHeight - 60));
            }
            if (DraggingSubMenuItem)
            {
                // calcualte the mouse movement and factor in the mouse sensitivity, the scale, and the grid size
                var diff = MouseUtil.GetMouseMovement(true, true, true);
                // adjust position and clamp
                if ((int)diff.X != 0 || (int)diff.Y != 0)
                    ActiveElement.SetMarginLT(
                        Math.Clamp(ActiveElement.Margin.Left + (diff.X * GridSize), 0, Workspace.ActualWidth - ActiveElement.ActualWidth),
                        Math.Clamp(ActiveElement.Margin.Top + (diff.Y * GridSize), 0, Workspace.ActualHeight - ActiveElement.ActualHeight));
            }
            // if the item scale mode is zero or greater, then we are scaling
            if (ItemScaleMode > -1)
            {
                // calcualte the mouse movement and factor in the mouse sensitivity, the scale, and the grid size
                var diff = MouseUtil.GetMouseMovement(true, true, true);
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
                    // Update the scaling overlay size
                    UpdateItemControlSize();
                    // Clamp the workspace position
                    ClampWorkspace();
                }
            }
        }
        /// <summary>
        /// This is the handler for when a menu top-tier item is opened
        /// like, file, view, and settings.
        /// once clicked, it builds the menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuButton_Click(object sender, MouseEventArgs e)
        {
            // get menu tag (basically index)
            MenuState enumTag = (MenuState)int.Parse(((Button)sender).Tag.ToString());
            // only update menu when the target menu is not the current menu
            if (WindowMenu != enumTag)
            {
                // update GUI menu properties
                Menu.Visibility = Visibility.Visible;
                OffClickDetection.Visibility = Visibility.Visible;
                Menu.SetMarginL(10 + (55 * ((int)enumTag-1)));
                MenuList.Items.Clear();
                int Height = 5;
                // load json to build the menu
                foreach (var item in MenuJson[enumTag.ToString("g")])
                {
                    Height += 25;
                    TextBlock buttonToAdd = new() 
                    {
                        Text = item["Display"].ToString(),
                        Tag = item["Tag"].ToString(),
                        Height = 20
                    };
                    MenuList.Items.Add(buttonToAdd);
                }
                MenuList.Height = Height;
                WindowMenu = enumTag;
            }
        }
        /// <summary>
        /// Event handler for the menu options
        /// This is a generic event handler, that is going to need lots of work
        /// It should be used for all menu options and when their selection is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // get the listbox where the change came from
            var box = (ListBox)sender;
            // check the listbox tag, if its the toolbox,
            // then it will not start with MENU-
            if (!box.Tag.ToString().StartsWith("MENU-"))
            {
                if (box.SelectedIndex >= 0)
                {
                    TextBlock item = (TextBlock)box.SelectedItem;
                    Grid g = GUI.Builder.BuildToolBoxItem(
                        item.Tag.ToString(),
                        this,
                        B_PreviewMouseDown,
                        B_PreviewMouseUp);
                    Workspace.Children.Add(g);
                    box.SelectedIndex = -1;
                }
            }
            Menu_PreviewMouseDown(sender, null);
        }
        /// <summary>
        /// MouseDown event for when a UI elemenent inside the workspace has been selected
        /// this element becomes the ActiveElement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void B_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // when the item is selected, we are in a "dragging" state
            DraggingSubMenuItem = true;
            // make sure we are not moving the workspace, and just the item
            MovingWorkspace = false;
            // set the active element
            ActiveElement = (Grid)sender;
            // the control button template, is the template for the scaling overlay
            var cc = Utility.FindContentControl("ControlButtonTemplate");
            // add double click event handler for the overlay
            cc.MouseDoubleClick += B_MouseDoubleClick;
            // set the width and height of the overlay
            ((Grid)cc.Content).SetWidthAndHeight(ActiveElement.ActualWidth, ActiveElement.ActualHeight);
            // finally, add the overlay to the selected item
            ActiveElement.Children.Add(cc);
            // this will build and add items to the properties tab
            Properites.Children.Add(
                GUI.Builder.BuildPropertiesItem(
                    ActiveElement.Tag.ToString(),
                    Tb_TextChanged));
            // reset starting mouse position
            MouseStart = MouseUtil.GetMousePosition();
            // reset workspace state to "not moving"
            Grid_PreviewMouseUp(sender, e);
        }
        /// <summary>
        /// Simple mouse event for the MouseUp event, after dragging the ActiveElement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void B_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
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
            Menu.Visibility = Visibility.Hidden;
            OffClickDetection.Visibility = Visibility.Hidden;
            // OffClickDetection is also used for items
            // it will remove the ActiveElement and reset other various varaibles
            // such as clearing properties menu, and removing the scaling overlay
            if (ActiveElement != null)
            {
                ActiveElement.Children.Remove(Utility.FindContentControl("ControlButtonTemplate"));
                Properites.Children.Clear();
                ActiveElement = null;
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
                // load previous scale for comparison
                var prev = Scale;
                // calcualte scale with pre-set sensitivity of 2500
                // this can be changed to affect the zoom sensitivity
                Scale = Math.Clamp(Scale + ((float)e.Delta / 2500), 0.1, 4);
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
            }
        }
        /// <summary>
        /// This is the mouse down event for the viewbox, and only the viewbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // if user is not 
            if (!DraggingSubMenuItem)
            {
                if (ActiveElement != null)
                    ActiveElement.Children.Remove(ActiveElement.Children[^1]);
                Menu_PreviewMouseDown(sender, e);
                MouseStart = MouseUtil.GetMousePosition();
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
        }
        /// <summary>
        /// This updates the scale overlay width and height
        /// </summary>
        private void UpdateItemControlSize()
        {
            ((Grid)Utility.FindContentControl("ControlButtonTemplate").Content).SetWidthAndHeight(ActiveElement.ActualHeight, ActiveElement.ActualWidth);
        }
        /// <summary>
        /// Double click event for workspace items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void B_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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
            // Fetch the scale mode
            var button = (Button)sender;
            ItemScaleMode = Int32.Parse(button.Tag.ToString());
            // Update the mouse starting position
            MouseStart = MouseUtil.GetMousePosition();
        }
        /// <summary>
        /// Simple event handler for any key down, ONLY inside the viewbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Simple delete method for deleting the selected item
            if(e.Key == Key.Delete && ActiveElement != null)
                Workspace.Children.Remove(ActiveElement);
        }
        /// <summary>
        /// TextChangedEvent for the properties menu
        /// this should be use for ALL editable text properties
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            // This translates the properties text to the textbox text (WIP)
            if(ActiveElement != null)
                ((TextBox)ActiveElement.Children[0]).Text = ((TextBox)sender).Text;
        }
    }
}
