using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using CustomExtensions;
using System.Diagnostics;
using System.ComponentModel;
using Path = System.IO.Path;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Threading;
using LayoutEditor.Windows.Pages;
using LayoutEditor.Utilities;
using Microsoft.Win32;
using System.Linq;
using LayoutEditor.Font;

namespace LayoutEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow Get { get; private set; }
        public int MoveSensitivity = 1;
        public int ZoomSensitivity = 1;
        public int MinGridSizeX = 5;
        public int MinGridSizeY = 5;
        public int GridSize { get; set; }

        private MenuState WindowMenu = MenuState.Closed;
        private MenuItem prevItem = null;
        public string SteamPath;
        public string GamePath;
        private readonly bool Installed;
        private readonly bool Running;
        private readonly bool Updating;
        private readonly string SteamDisplayName;
        private readonly string SteamLanguage;
        private bool HasContentRendered = false;
        private ApplicationConfiguration _configuration;
        private readonly List<MainEditor> PageList;
        public readonly Dictionary<string, FontInfo> Fonts = new();
        public readonly Dictionary<string, string> SMNamingMap = new();


        /// <summary>
        /// Initializaer for the thing and stuff
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            Get = this;
            this.Closing += OnWindowClosing;
            this.PageList = new();

            // STEAM STUFF

            this.SteamPath = Utility.GetRegVal<string>("Software\\Valve\\Steam", "SteamPath").ToValidPath();
            this.Installed = Utility.GetRegVal<bool>("Software\\Valve\\Steam\\Apps\\387990", "Installed");
            this.Updating = Utility.GetRegVal<bool>("Software\\Valve\\Steam\\Apps\\387990", "Updating");
            this.Running = Utility.GetRegVal<bool>("Software\\Valve\\Steam\\Apps\\387990", "Running");
            this.SteamDisplayName = Utility.GetRegVal<string>("Software\\Valve\\Steam", "LastGameNameUsed");
            this.SteamLanguage = Utility.GetRegVal<string>("Software\\Valve\\Steam", "Language").CapitilzeFirst();

            if (!Directory.Exists(Path.Combine(this.SteamPath, "steamapps", "common", "Scrap Mechanic")))
            {
                string[] fileContents = File.ReadAllText(Path.Combine(this.SteamPath, "steamapps", "libraryfolders.vdf")).Split("\"");
                foreach (string path in fileContents)
                {
                    string smPath = Path.Combine(path, "steamapps", "common", "Scrap Mechanic");
                    if (Directory.Exists(smPath))
                    {
                        this.GamePath = smPath;
                        break;
                    }
                }
            }
            else
            {
                throw new Exception("Scrap Mechanic Not Found");
            }

            this.GamePath = this.GamePath.Replace("\\\\", "\\");

            this.Fonts = FontInfo.LoadFontInformation(this.GamePath);

            string[] lineItems = Utility.LoadInternalFile.TextFile("InterfaceTags.txt").Split(Environment.NewLine);
            foreach (var line in lineItems)
                if (line.IndexOf(' ') is int splitter && splitter >= 0)
                    this.SMNamingMap.TryAdd($"#{{{line[..splitter].Trim()}}}", line[splitter..].Trim());

            _ = this.LoadConfiguration(LoadToolBoxItems);
        }
        /// <summary>
        /// Event handler for window resize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EntireWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.PageList != null && this.PageList.Count > 0)
                this.PageList.ForEach(p => p.ClampTabs());
        }
        private void ResolutionClick(object sender, RoutedEventArgs e)
        {
            if (sender is Tuple<int, int> || sender is Tuple<object, object>)
            {
                object tuple = sender;
                object additionalArgs = null;
                if (sender is not Tuple<int, int>)
                {
                    Tuple<object, object> obj = sender as Tuple<object, object>;
                    tuple = obj.Item1 as Tuple<int, int>;
                    additionalArgs = obj.Item2;
                }

                Tuple<int, int> args = tuple as Tuple<int, int>;

                this.SetResolution(args.Item1, args.Item2, additionalArgs);

                foreach (MenuItem item in this.ResolutionSideMenu.Items)
                {
                    if (item.Header.ToString().Contains(args.Item1 + "x" + args.Item2))
                    {
                        item.Background = Brushes.LightBlue;
                        prevItem = item;
                    }
                }
            }
            else
            {
                MenuItem SelectedOption = sender as MenuItem;

                if (prevItem != null)
                    this.prevItem.Background = Brushes.Transparent;

                var resoultion = SelectedOption.Header.ToString().Split(" ")[0].Split("x");

                this.SetResolution(int.Parse(resoultion[0]), int.Parse(resoultion[1]));

                SelectedOption.Background = Brushes.LightBlue;

                prevItem = SelectedOption;
            }
        }
        private void SetResolution(int width, int height, object optional = null)
        {
            if (PageList != null && this.PageList.Count > 0)
            {
                MainEditor currentPage = PageList[this.NavigationWindow.SelectedIndex];
                currentPage.SetResolution(width, height, optional);
            }
        }
        private void LoadToolBoxItems()
        {
            JObject json = JObject.Parse(Utility.LoadInternalFile.TextFile("MyGUI_Trace.json"));
            List<string> tbxi = new();
            tbxi.AddRange(from JProperty item in json["Widget"]
                          select item.Name.ToString());
            tbxi.Sort();
            foreach (string item in tbxi)
            {
                MenuItem menuItem = new()
                {
                    Name = item,
                    Header = item
                };
                menuItem.Click += MenuItemClick;
                this.ToolBoxDropDown.Items.Add(menuItem);
            }
            this.AddNewPage(null, null);
        }
        private void MenuItemClick(object sender, RoutedEventArgs e)
        {
            // invoke page here
            if (PageList != null && this.PageList.Count > 0)
            {
                MainEditor currentPage = PageList[this.NavigationWindow.SelectedIndex];
                currentPage.MenuItemClick(sender, e);
            }
        }

        private void ProjectSettingsClick(object sender, RoutedEventArgs e)
        {

        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {

        }

        private void LoadFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                CheckFileExists = true
            };
            if (dlg.ShowDialog() == true)
            {
                TabItem tabItem = new() { IsSelected = true };
                MainEditor np = new(dlg.FileName);
                this.PageList.Add(np);
                tabItem.Content = new Frame() { Content = np };
                tabItem.Header = Path.GetFileNameWithoutExtension(dlg.FileName);
                ControlTemplate ctmp = this.FindResource("NewTabItemTemplate") as ControlTemplate;
                tabItem.Template = ctmp;
                this.NavigationWindow.Items.Insert(this.NavigationWindow.Items.Count - 1, tabItem);
                this.NavigationWindow.SelectedIndex = this.NavigationWindow.Items.Count - 2;
            }
        }

        private void LoadProjectClick(object sender, RoutedEventArgs e)
        {

        }

        private void SaveFileClick(object sender, RoutedEventArgs e)
        {

        }

        private void SaveAsClick(object sender, RoutedEventArgs e)
        {

        }

        private void SaveProjectClick(object sender, RoutedEventArgs e)
        {

        }

        private async Task LoadConfiguration(Action FinishLoadCallBack = null)
        {
            this._configuration = JsonApplicationConfiguration.Load<ApplicationConfiguration>("LayoutEditor/Config", true, true);

            if (this._configuration.Top > 0)
                this.Top = this._configuration.Top;
            if (this._configuration.Left > 0)
                this.Left = this._configuration.Left;
            if (this._configuration.WindowWidth > 0)
                this.Width = this._configuration.WindowWidth;
            if (this._configuration.WindowHeight > 0)
                this.Height = this._configuration.WindowHeight;
            if (this._configuration.MoveSensitivity > 0)
                this.MoveSensitivity = this._configuration.MoveSensitivity;
            if (this._configuration.ZoomSensitivity > 0)
                this.ZoomSensitivity = this._configuration.ZoomSensitivity;
            this.WindowState = this._configuration.WindowState;

            if (this._configuration.IsFirstStart)
                this._configuration.IsFirstStart = false;

            while (!this.HasContentRendered) { await Task.Delay(100); }

            if (this._configuration.Resolution != null)
                this.ResolutionClick(new Tuple<object, object>(this._configuration.Resolution, this._configuration.Workspace), null);

            FinishLoadCallBack.Invoke();
        }
        private void SaveConfiguration()
        {
            this._configuration.Left = Left;
            this._configuration.Top = Top;
            this._configuration.WindowWidth = Width;
            this._configuration.WindowHeight = Height;
            this._configuration.WindowState = WindowState;
            //_configuration.Resolution = Resolution;
            this._configuration.GridSize = GridSize;
            this._configuration.MoveSensitivity = MoveSensitivity;
            this._configuration.ZoomSensitivity = ZoomSensitivity;

            JsonApplicationConfiguration.Save("LayoutEditor/Config", _configuration, true);
        }
        private async void OnWindowClosing(object sender, CancelEventArgs args)
        {
            this.SaveConfiguration();

            args.Cancel = true;

            this.Closing -= this.OnWindowClosing;

            await this.Dispatcher.InvokeAsync(Close);
        }
        private void OnShowAboutWindow(object sender, RoutedEventArgs e)
        {

        }
        private void OnExitApplication(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// This closes the menu, when the mouse clicks off of the menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // change states and visibilities
            this.WindowMenu = MenuState.Closed;
            this.OffClickDetection.Visibility = Visibility.Hidden;
            // OffClickDetection is also used for items
            // it will remove the ActiveElement and reset other various varaibles
            // such as clearing properties menu, and removing the scaling overlay
            // invoke page here OffClickDetection
            if (this.PageList != null && this.PageList.Count > 0)
            {
                MainEditor currentPage = this.PageList[this.NavigationWindow.SelectedIndex];
                currentPage.OffClickDetection(sender, e);
            }
        }

        private void EntireWindowContentRendered(object sender, EventArgs e)
        {
            this.HasContentRendered = true;
        }

        /// <summary>
        /// this is the huge (it does a lot of stuff) MouseMove event handler,
        /// that triggers when the mouse moves throughout the entire window of the appliation.
        /// this handles workspace movement, ActiveElement scaling, and the split view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EntireWindowMouseMove(object sender, MouseEventArgs e)
        {
            if (this.PageList != null && this.PageList.Count > 0)
            {
                if (this.NavigationWindow.SelectedIndex >= 0 && this.NavigationWindow.SelectedIndex < this.PageList.Count)
                {
                    this.PageList[this.NavigationWindow.SelectedIndex].WindowMouseMove(sender, e);
                }
            }
        }
        private void ToolBarLoaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;

            if (toolBar.Template.FindName("OverflowGrid", toolBar) is FrameworkElement overflowGrid)
                overflowGrid.Visibility = Visibility.Collapsed;

            if (toolBar.Template.FindName("MainPanelBorder", toolBar) is FrameworkElement mainPanelBorder)
                mainPanelBorder.Margin = new Thickness();
        }
        private void AddNewPage(object sender, MouseButtonEventArgs e)
        {
            TabItem tabItem = new() { IsSelected = true };
            MainEditor np = new();
            this.PageList.Add(np);
            tabItem.Content = new Frame() { Content = np };
            tabItem.Header = "Untitled";
            ControlTemplate ctmp = this.FindResource("NewTabItemTemplate") as ControlTemplate;
            tabItem.Template = ctmp;
            this.NavigationWindow.Items.Insert(this.NavigationWindow.Items.Count - 1, tabItem);
            this.NavigationWindow.SelectedIndex = this.NavigationWindow.Items.Count - 2;
        }
        private void ClosePageMouseEnter(object sender, MouseEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            tb.Foreground = Brushes.Red;
        }
        private void ClosePageMouseLeave(object sender, MouseEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            tb.Foreground = Brushes.LightPink;
        }
        private void ClosePage(object sender, MouseButtonEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            this.PageList.RemoveAt(this.NavigationWindow.SelectedIndex);
            this.NavigationWindow.Items.Remove(tb.TemplatedParent);
            if (this.NavigationWindow.Items.Count - 1 == this.NavigationWindow.SelectedIndex)
                this.NavigationWindow.SelectedIndex--;
        }
        public void SetCurrentTabName(string name)
        {
            TabItem tbi = this.NavigationWindow.SelectedItem as TabItem;
            if (tbi != AddNewTab && tbi != null)
                tbi.Header = name;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (this.PageList != null && this.PageList.Count > 0)
            {
                MainEditor currentPage = this.PageList[this.NavigationWindow.SelectedIndex];
                currentPage.GridPreviewKeyDown(sender, e);
            }
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (this.PageList != null && this.PageList.Count > 0)
            {
                MainEditor currentPage = this.PageList[this.NavigationWindow.SelectedIndex];
                currentPage.GridPreviewKeyUp(sender, e);
            }
        }
    }
}
