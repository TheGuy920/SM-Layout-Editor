using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace LayoutEditor.CustomXML
{
    public interface IXmlDOM
    {
        Guid? ParentId { get; }
        XmlPosition Position { get; }
        List<Guid> ChildrenId { get; }
        Guid Guid { get; }
        string ToString(int _ = 0);
        Grid LoadGui(int _ = 0);
        void ChangeViewMode(XmlViewMode _);
        Grid GridActor { get; }
        IEnumerable<FrameworkElement> PropertyItems { get; }
    }
}
