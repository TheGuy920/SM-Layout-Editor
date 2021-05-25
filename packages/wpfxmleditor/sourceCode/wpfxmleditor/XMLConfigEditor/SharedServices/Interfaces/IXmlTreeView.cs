using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ThomsonReuters.Internal.Tools.ConfigEditor.SharedServices.Interfaces
{
    public interface IXmlTreeView
    {
        event EventHandler<SelectedNodeEventArgs> SelectNode;
        void OnSelectNode(object sender, SelectedNodeEventArgs e);
        void UpdateNode(XmlNode prevNode);
        void LoadDocument(string path);
        void UnloadDocument();
    }

    public class SelectedNodeEventArgs : EventArgs
    {
        public XmlNode CurrentNode { get; set; }        

        public SelectedNodeEventArgs(XmlNode value)
        {
            this.CurrentNode = value;
          
        }
    }

   
}
