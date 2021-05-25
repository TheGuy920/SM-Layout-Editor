using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using ThomsonReuters.Internal.Tools.ConfigEditor.SharedServices;
using System.Xml;

namespace ThomsonReuters.Internal.Tools.ConfigEditor.XMLEditorModule
{
    public enum ContextMenuType
    {
        Cut,
        Copy,
        Paste,
        Delete,
        Add
    }
    public class ContextMenuProvider
    {
        public readonly Dictionary<ContextMenuType, MenuItem> ContextMenus = new Dictionary<ContextMenuType, MenuItem>();

        public ContextMenuProvider()
        {
            ContextMenus.Add(ContextMenuType.Copy, new MenuItem { Header = "Copy" });
            ContextMenus.Add(ContextMenuType.Paste, new MenuItem { Header = "Paste" });
            ContextMenus.Add(ContextMenuType.Delete, new MenuItem { Header = "Delete Element" });
            ContextMenus.Add(ContextMenuType.Add, new MenuItem { Header = "Add Element" });

        }
    }
}
