using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using ThomsonReuters.Internal.Tools.ConfigEditor.Common;
using System.Xml;
using System.Diagnostics;

namespace ThomsonReuters.Internal.Tools.ConfigEditor.XMLEditorModule.ViewModels
{
    class CommandsBarViewModel
    {
        private TreeEditorViewModel documentEditor;
        private string path;
        private XmlDocument activeDocument;

        public string Path
        {
            get { return path; }
            set { path = value; }
        }
        

        ICommand LoadCommand { get; set; }
        public CommandsBarViewModel()
        {
            LoadCommand = new RelayCommand(LoadCommandExecute, CanExecute);
        }
        public TreeEditorViewModel DocumentEditor
        {
            get { return documentEditor; }
            set { documentEditor = value; }
        }
        
        private void LoadCommandExecute(object param)
        {
            activeDocument = new XmlDocument();
            try
            {
                activeDocument.Load(path);
                
            }
            catch
            {
                Debug.WriteLine(string.Format("Metadata file {0} doesn't exists.", path));

            }
        }

        private bool CanExecute(object param)
        {
            return true;
        }

    }
}
