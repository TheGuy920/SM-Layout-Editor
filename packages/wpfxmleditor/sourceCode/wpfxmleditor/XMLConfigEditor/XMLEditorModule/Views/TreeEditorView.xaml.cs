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
using System.Xml;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml.XPath;
using WXE.Internal.Tools.ConfigEditor.XMLEditorModule.ViewModels;
using WXE.Internal.Tools.ConfigEditor.Common;

namespace WXE.Internal.Tools.ConfigEditor.XMLEditorModule.Views
{
  
    public partial class TreeEditorView : UserControl
    {
        private TreeEditorViewModel viewModel;
        private ContextMenuProvider contextMenuProvider;
        

        public TreeEditorView()
        {           
            InitializeComponent();
            contextMenuProvider = new ContextMenuProvider();
            this.xmlTreeView.ContextMenu = new ContextMenu();
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(TreeEditorView_DataContextChanged);
           
        }

        void TreeEditorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
           
               ViewModel = e.NewValue as TreeEditorViewModel;            
        }

        public TreeEditorViewModel ViewModel
        {
            get { return viewModel; }
            set
            {
                viewModel = value;
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    this.Cursor = Cursors.Wait;
                BindUIElementToViewModel();
                this.Cursor = Cursors.Arrow;
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void xmlTreeView_Selected(object sender, RoutedEventArgs e)
        {
            XmlNode selectedItem  = xmlTreeView.SelectedItem as XmlNode;
            ViewModel.ViewAttributesCommand.Execute(selectedItem);           
        }

        private void BindUIElementToViewModel()
        {
            //this.DataContext = viewModel;
            if (viewModel == null)
            {
                return;
            }
           
            XmlDataProvider dataProvider = this.FindResource("xmlDataProvider") as XmlDataProvider;
            dataProvider.Document = viewModel.DataModel;
            dataProvider.Refresh();        
            this.xmlTreeView.ContextMenu.Items.Clear();
            contextMenuProvider.ContextMenus[ContextMenuType.Copy].Command = ViewModel.CopyElementCommand;
            this.xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.ContextMenus[ContextMenuType.Copy]);

            contextMenuProvider.ContextMenus[ContextMenuType.Paste].Command = ViewModel.PasteElementCommand;
            this.xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.ContextMenus[ContextMenuType.Paste]);

            this.xmlTreeView.ContextMenu.Items.Add(new Separator());

            contextMenuProvider.ContextMenus[ContextMenuType.Add].Command = ViewModel.AddElementCommand;
            contextMenuProvider.ContextMenus[ContextMenuType.Add].CommandParameter = XmlNodeType.Element;

            this.xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.ContextMenus[ContextMenuType.Add]);

            contextMenuProvider.ContextMenus[ContextMenuType.Delete].Command = ViewModel.DeleteElementCommand;
            this.xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.ContextMenus[ContextMenuType.Delete]);

            ViewModel.AddXmlNode = AddNewNodeFromUI;
            ViewModel.HighlightNodeInUI = HighlightNode;
        }

        XmlNode AddNewNodeFromUI(XmlNodeType xmlNodeType)
        {
            AddChildView popup = new AddChildView(this.ViewModel.DataModel, xmlNodeType);
            popup.ShowDialog();           
            return popup.NewNode;            
        }

       
        private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseEventArgs e)
        {
            TreeViewItem selectedItem = sender as TreeViewItem;
            if (selectedItem != null)
            {
                selectedItem.IsSelected = true;
            }
          

        }


        #region TreeNode Search


        public void HighlightNode(XmlNode xmlNode)
        {
            bool isSelected = false;
            
            TreeViewItem rootNode = null;
            try
            {
                rootNode = xmlTreeView.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem;
              
            }
            catch
            {

            }
            if (xmlNode == null)
            {
                isSelected = SelectTreeViewItem(ref rootNode, "");
            }
            else
            {
                isSelected = SelectTreeViewItem(ref rootNode, xmlNode);
            }
            if (!isSelected)
            {
                MessageBox.Show("Could not locate the node.");
            }

            //temp
            //XmlNode childNode = (xmlTreeView.SelectedItem as XmlNode).FirstChild.CloneNode(true);
            //SelectedNode.InsertAfter(childNode, SelectedNode.FirstChild);

        }

        private bool SelectTreeViewItem(ref TreeViewItem rootNode, XmlNode toBeSelectedNode)
        {
            bool isSelected = false;
            if (rootNode == null)
                return isSelected;

            if (!rootNode.IsExpanded)
            {
                rootNode.Focus();
                rootNode.IsExpanded = true;
            }
            XmlNode tempNode = rootNode.Header as XmlNode;
            if (tempNode == null)
            {
                return isSelected;
            }
            if(tempNode == toBeSelectedNode)
            //if (string.Compare(tempNode.Name, toBeSelectedNode.Name, true) == 0 && tempNode.NodeType == toBeSelectedNode.NodeType)
            {
                rootNode.IsSelected = true;
                rootNode.IsExpanded = true;
                isSelected = true;
                return isSelected;
            }
            else
            {
                for (int i = 0; i < rootNode.Items.Count; i++)
                {
                    TreeViewItem childItem = rootNode.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;

                    isSelected = SelectTreeViewItem(ref childItem, toBeSelectedNode);
                    if (isSelected)
                    {
                        break;
                    }
                }
                return isSelected;
            }


        }

        private bool SelectTreeViewItem(ref TreeViewItem rootNode, string elementName)
        {
            bool isSelected = false;
            if (rootNode == null)
                return isSelected;

            if (!rootNode.IsExpanded)
            {
                rootNode.Focus();
                rootNode.IsExpanded = true;
            }
            XmlNode tempNode = rootNode.Header as XmlNode;
            if (tempNode == null)
            {
                return isSelected;
            }
            if (string.Compare(tempNode.Name, elementName, true) == 0)
            {
                rootNode.IsSelected = true;
                rootNode.IsExpanded = true;
                isSelected = true;
                return isSelected;
            }
            else
            {
                for (int i = 0; i < rootNode.Items.Count; i++)
                {
                    TreeViewItem childItem = rootNode.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;

                    isSelected = SelectTreeViewItem(ref childItem, elementName);
                    if (isSelected)
                    {
                        break;
                    }
                }
                return isSelected;
            }


        }
        #endregion
    }
}
