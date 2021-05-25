using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ThomsonReuters.Internal.Tools.ConfigEditor.SharedServices.Interfaces
{
    public interface IPropertiesView
    {
        XmlNode CurrentNode { get; set; }      
        
    }
}
