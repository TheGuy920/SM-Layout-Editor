using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WXE.Internal.Tools.ConfigEditor.Common;
using System.Xml;
using System.Diagnostics;
using Microsoft.Win32;

namespace WXE.Internal.Tools.ConfigEditor.XMLEditorModule.Views
{
    /// <summary>
    /// Interaction logic for CommandsBarView.xaml
    /// </summary>
    public partial class CommandsBarView : UserControl
    {
    
        public CommandsBarView()
        {
            InitializeComponent();
            this.MenuBar.Items.Clear();
            MenuItem fileMenuItem = new MenuItem { Header = "_File" };
            MenuItem openMenuItem = new MenuItem { Header = "_Open" };
            openMenuItem.Click += new RoutedEventHandler(openMenuItem_Click);
            MenuItem saveMenuItem = new MenuItem { Header = "_Save" };
            saveMenuItem.Click += new RoutedEventHandler(saveMenuItem_Click);

            MenuItem saveAsMenuItem = new MenuItem { Header = "Save _As" };
            saveAsMenuItem.Click += new RoutedEventHandler(saveAsMenuItem_Click);

            fileMenuItem.Items.Add(openMenuItem);
            fileMenuItem.Items.Add(new Separator());
            fileMenuItem.Items.Add(saveMenuItem);          
            fileMenuItem.Items.Add(saveAsMenuItem);
            this.MenuBar.Items.Add(fileMenuItem);
        }

      

        
        
        //public string xPath
        //{
        //    get { return (string)GetValue(xPathProperty); }
        //    set { SetValue(xPathProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for xPath.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty xPathProperty =
        //    DependencyProperty.Register("xPath", typeof(string), typeof(CommandsBarView), new UIPropertyMetadata(string.Empty));

        

        public event EventHandler<DocumentLoadedEventArgs> DocumentLoaded;
        public event EventHandler<SearchRequestedEventArgs> SearchRequested;
        public event EventHandler SaveRequested;
        public event EventHandler<SaveAsEventArgs> SaveAsRequested;

        private void OnDocumentLoaded(object sender, DocumentLoadedEventArgs e)
        {
            if (DocumentLoaded != null)
            {
                DocumentLoaded(sender, e);
            }
        }
      

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            //if (SearchRequested != null)
            //{
            //    SearchRequested(this, new SearchRequestedEventArgs { XPath = searchTextBox.Text });
            //}
        }

     

        #region Menu Click Handlers

        void openMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "XML Files (*.xml)|*.xml";
            if (open.ShowDialog() == true)
            {
                XmlDocument document = new XmlDocument();
                try
                {
                    document.Load(open.FileName);
                    DocumentLoadedEventArgs args = new DocumentLoadedEventArgs() { Path = open.FileName, Document = document, FileName = open.SafeFileName };
                    OnDocumentLoaded(this, args);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

            }
        }

        void saveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "XML Files (*.xml)|*.xml";
            if (dialog.ShowDialog() == true)
            {
                SaveAsEventArgs args = new SaveAsEventArgs { FileName = dialog.SafeFileName, Path = dialog.FileName };
                if (SaveAsRequested != null)
                {
                    SaveAsRequested(this, args);
                }
            } 
        }

        void saveMenuItem_Click(object sender, RoutedEventArgs e)
        {           

            if (SaveRequested != null)
            {
                SaveRequested(this, e);
            }
        }
        #endregion


       
    }
}
