using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ThomsonReuters.Internal.Tools.ConfigEditor.SharedServices.Interfaces
{
    public class DocumentLoadedEventArgs :EventArgs
    {
        public XmlDocument Document { get; set; }
        public string  Path { get; set; }

    }

    public class SearchRequestedEventArgs : EventArgs
    {
        public string XPath { get; set; }
       

    }
}
