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
using WXE.Internal.Tools.ConfigEditor.XMLEditorModule.Views;
using ConfigEditor;
using WXE.Internal.Tools.ConfigEditor.XMLEditorModule.ViewModels;
using WXE.Internal.Tools.ConfigEditor.Common;

namespace WXE.Internal.Tools.ConfigEditor.ConfigEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TreeEditorsViewModel editorsVM;
        public MainWindow()
        {
            InitializeComponent();     
            this.commandBarView.DocumentLoaded += new EventHandler<DocumentLoadedEventArgs>(commandBarView_DocumentLoaded);
            this.commandBarView.SearchRequested += new EventHandler<SearchRequestedEventArgs>(commandBarView_SearchRequested);
            this.commandBarView.SaveRequested += new EventHandler(commandBarView_SaveRequested);
            this.commandBarView.SaveAsRequested += new EventHandler<SaveAsEventArgs>(commandBarView_SaveAsRequested);
            editorsVM = new TreeEditorsViewModel();
           
            this.editorsView.ViewModel = editorsVM;
        }

          

        void commandBarView_SearchRequested(object sender, SearchRequestedEventArgs e)
        {          
           
           this.editorsVM.ActiveEditor.FindElementCommand.Execute(e.XPath);
        }

      
        void commandBarView_DocumentLoaded(object sender, DocumentLoadedEventArgs e)
        {
            var xmlTreeViewModel = new TreeEditorViewModel(e.Document, e.Path, e.FileName);            
    
            editorsVM.Add(xmlTreeViewModel);            
        }

        void commandBarView_SaveAsRequested(object sender, SaveAsEventArgs e)
        {
            editorsVM.ActiveEditor.SaveAsDocumentCommand.Execute(e.Path);
        }

        void commandBarView_SaveRequested(object sender, EventArgs e)
        {
            editorsVM.ActiveEditor.SaveDocumentCommand.Execute(null);
        }  
    }
}
