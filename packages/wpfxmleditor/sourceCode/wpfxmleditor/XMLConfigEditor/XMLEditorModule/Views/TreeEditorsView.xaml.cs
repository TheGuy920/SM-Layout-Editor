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
using WXE.Internal.Tools.ConfigEditor.XMLEditorModule.ViewModels;

namespace WXE.Internal.Tools.ConfigEditor.XMLEditorModule.Views
{
    /// <summary>
    /// Interaction logic for TreeEditorsView.xaml
    /// </summary>
    public partial class TreeEditorsView : UserControl
    {
        private TreeEditorsViewModel viewModel;
        public TreeEditorsViewModel ViewModel
        {
            get { return viewModel; }
            set { this.DataContext = viewModel = value; }
        }
        public TreeEditorsView()
        {
            InitializeComponent();           
        }
      

        void CloseTab_Handler(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel.Remove((sender as CloseableTabItem).Content as TreeEditorViewModel);
            }
            catch
            {
            }
        }


    }
}
