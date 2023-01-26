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
        public int GridSize
        {
            get;
            set;
        }

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
        

        /// <summary>
        /// Initializaer for the thing and stuff
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Get = this;
            Closing += OnWindowClosing;
            PageList = new();

            // STEAM STUFF

            SteamPath = Utility.GetRegVal<string>("Software\\Valve\\Steam", "SteamPath").ToValidPath();
            Installed = Utility.GetRegVal<bool>("Software\\Valve\\Steam\\Apps\\387990", "Installed");
            Updating = Utility.GetRegVal<bool>("Software\\Valve\\Steam\\Apps\\387990", "Updating");
            Running = Utility.GetRegVal<bool>("Software\\Valve\\Steam\\Apps\\387990", "Running");
            SteamDisplayName = Utility.GetRegVal<string>("Software\\Valve\\Steam", "LastGameNameUsed");
            SteamLanguage = Utility.GetRegVal<string>("Software\\Valve\\Steam", "Language").CapitilzeFirst();

            if (!Directory.Exists(Path.Combine(SteamPath, "steamapps", "common", "Scrap Mechanic"))) {
                string[] fileContents = File.ReadAllText(Path.Combine(SteamPath, "steamapps", "libraryfolders.vdf")).Split("\"");
                foreach (string path in fileContents)
                {
                    string smPath = Path.Combine(path, "steamapps", "common", "Scrap Mechanic");
                    if (Directory.Exists(smPath))
                    {
                        GamePath = smPath;
                        break;
                    }
                }
            }
            else
            {
                throw new Exception("Scrap Mechanic Not Found");
            }

            GamePath = GamePath.Replace("\\\\", "\\");

            Debug.WriteLine(GamePath);

            _ = LoadConfiguration(LoadToolBoxItems);

            
        }
        /// <summary>
        /// Event handler for window resize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EntireWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (PageList != null && PageList.Count > 0) PageList.ForEach(p => p.ClampTabs());
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

                SetResolution(args.Item1, args.Item2, additionalArgs);

                foreach (MenuItem item in ResolutionSideMenu.Items)
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
                    prevItem.Background = Brushes.Transparent;

                var resoultion = SelectedOption.Header.ToString().Split(" ")[0].Split("x");

                SetResolution(int.Parse(resoultion[0]), int.Parse(resoultion[1]));

                SelectedOption.Background = Brushes.LightBlue;

                prevItem = SelectedOption;
            }
        }
        private void SetResolution(int width, int height, object optional = null)
        {
            if (PageList != null && PageList.Count > 0)
            {
                MainEditor currentPage = PageList[NavigationWindow.SelectedIndex];
                currentPage.SetResolution(width, height, optional);
            }
        }
        private void LoadToolBoxItems()
        {
            JObject json = JObject.Parse(Utility.LoadInternalFile.TextFile("MyGUI_Trace.json"));
            List<string> tbxi = new();
            foreach (JProperty item in json["Widget"])
            {
                tbxi.Add(item.Name.ToString());
            }
            tbxi.Sort();
            foreach (string item in tbxi)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Name = item;
                menuItem.Header = item;
                menuItem.Click += MenuItemClick;
                ToolBoxDropDown.Items.Add(menuItem);
            }
            AddNewPage(null, null);
        }
        private void MenuItemClick(object sender, RoutedEventArgs e)
        {
            // invoke page here
            if (PageList != null && PageList.Count > 0)
            {
                MainEditor currentPage = PageList[NavigationWindow.SelectedIndex];
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
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            if (dlg.ShowDialog() == true)
            {
                TabItem tabItem = new() { IsSelected = true };
                MainEditor np = new(dlg.FileName);
                PageList.Add(np);
                tabItem.Content = new Frame() { Content = np };
                tabItem.Header = Path.GetFileNameWithoutExtension(dlg.FileName);
                ControlTemplate ctmp = FindResource("NewTabItemTemplate") as ControlTemplate;
                tabItem.Template = ctmp;
                NavigationWindow.Items.Insert(NavigationWindow.Items.Count - 1, tabItem);
                NavigationWindow.SelectedIndex = NavigationWindow.Items.Count - 2;
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
            _configuration = JsonApplicationConfiguration.Load<ApplicationConfiguration>("LayoutEditor/Config", true, true);
            
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
            WindowState = _configuration.WindowState;

            if (_configuration.IsFirstStart)
                _configuration.IsFirstStart = false;

            while (!HasContentRendered) { await Task.Delay(100); }

            if (_configuration.Resolution != null)
                ResolutionClick(new Tuple<object, object>(_configuration.Resolution, _configuration.Workspace), null);

            FinishLoadCallBack.Invoke();
        }
        private void SaveConfiguration()
        {
            _configuration.Left = Left;
            _configuration.Top = Top;
            _configuration.WindowWidth = Width;
            _configuration.WindowHeight = Height;
            _configuration.WindowState = WindowState;
            //_configuration.Resolution = Resolution;
            _configuration.GridSize = GridSize;
            _configuration.MoveSensitivity = MoveSensitivity;
            _configuration.ZoomSensitivity = ZoomSensitivity;

            JsonApplicationConfiguration.Save("LayoutEditor/Config", _configuration, true);
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
        /// <summary>
        /// This closes the menu, when the mouse clicks off of the menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // change states and visibilities
            WindowMenu = MenuState.Closed;
            OffClickDetection.Visibility = Visibility.Hidden;
            // OffClickDetection is also used for items
            // it will remove the ActiveElement and reset other various varaibles
            // such as clearing properties menu, and removing the scaling overlay
            // invoke page here OffClickDetection
            if (PageList != null && PageList.Count > 0)
            {
                MainEditor currentPage = PageList[NavigationWindow.SelectedIndex];
                currentPage.OffClickDetection(sender, e);
            }
        }
        private void EntireWindowContentRendered(object sender, EventArgs e)
        {
            HasContentRendered = true;
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
            if (PageList != null && PageList.Count > 0)
            {
                if (NavigationWindow.SelectedIndex >= 0 && NavigationWindow.SelectedIndex < PageList.Count)
                {
                    PageList[NavigationWindow.SelectedIndex].WindowMouseMove(sender, e);
                }
            }
        }
        private void ToolBarLoaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;

            if (overflowGrid != null)
                overflowGrid.Visibility = Visibility.Collapsed;

            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;

            if (mainPanelBorder != null)
                mainPanelBorder.Margin = new Thickness();
        }
        private void AddNewPage(object sender, MouseButtonEventArgs e)
        {
            TabItem tabItem = new() { IsSelected = true };
            MainEditor np = new();
            PageList.Add(np);
            tabItem.Content = new Frame() { Content = np };
            tabItem.Header = "Untitled";
            ControlTemplate ctmp = FindResource("NewTabItemTemplate") as ControlTemplate;
            tabItem.Template = ctmp;
            NavigationWindow.Items.Insert(NavigationWindow.Items.Count - 1, tabItem);
            NavigationWindow.SelectedIndex = NavigationWindow.Items.Count - 2;
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
            PageList.RemoveAt(NavigationWindow.SelectedIndex);
            NavigationWindow.Items.Remove(tb.TemplatedParent);
            if (NavigationWindow.Items.Count - 1 == NavigationWindow.SelectedIndex)
                NavigationWindow.SelectedIndex--;
        }
        public void SetCurrentTabName(string name)
        {
            TabItem tbi = NavigationWindow.SelectedItem as TabItem;
            if (tbi != AddNewTab && tbi != null)
                tbi.Header = name;
        }
    }
}
