using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace LayoutEditor.CustomXML
{
    public class MyGUI : IXmlDOM
    {
        public Guid? _Parent { get; private set; }
        public XmlPosition Position { get; private set; }
        public List<Guid> _Children { get; private set; }
        public Guid Guid { get; private set; }
        public Grid WorkSpaceBinding { get; private set; }
        private readonly Dictionary<Guid, IXmlDOM> Document;

        private readonly Version Version = new(3, 2, 0);
        /// <summary>
        /// Creates a new MyGUI element of the MyGUI structure
        /// </summary>
        /// <param name="d">The Reference To The Document Linked List</param>
        /// <param name="WorkspaceBind">The Main Workspace The MyGUI Root Element Will Bind To</param>
        public MyGUI(ref Dictionary<Guid, IXmlDOM> d, ref Grid WorkspaceBind)
        {
            this.Document = d;
            this._Children = new();
            this.Guid = Guid.NewGuid();
            this.Position = new(1,1,1,1);
            this.WorkSpaceBinding = WorkspaceBind;
            this._Parent = null;
        }

        string IXmlDOM.ToString(int nest)
        {
            StringBuilder xml = new();
            // Open Tag
            xml.Append($"<MyGUI type=\"{XmlType.Layout}\" version=\"{Version}\">");
            // Add Children
            foreach (Guid ChildGuid in this._Children)
                xml.Append(this.Document[ChildGuid].ToString(1));
            // New Line
            xml.Append(Environment.NewLine);
            // Close Tag
            xml.Append($"</MyGUI>");
            // Return
            return xml.ToString();
        }
        public Grid LoadGui(int _) => null;
        public Grid GetGrid() => this.WorkSpaceBinding;
        public void ChangeViewMode(XmlViewMode _) { }
    }
}
