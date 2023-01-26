using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace LayoutEditor.CustomXML
{
    public interface IXmlDOM
    {
        Guid? _Parent { get; }
        XmlPosition Position { get; }
        List<Guid> _Children { get; }
        Guid Guid { get; }
        string ToString(int _ = 0);
        Grid LoadGui(int _ = 0);
        void ChangeViewMode(XmlViewMode _);
        Grid GetGrid();
    }
}
