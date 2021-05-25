using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThomsonReuters.Internal.Tools.ConfigEditor.SharedServices.Interfaces
{
    public interface ICommandsBarView
    {
       event EventHandler<DocumentLoadedEventArgs> DocumentLoaded;
       event EventHandler<SearchRequestedEventArgs> SearchRequested;
       event EventHandler DocumentUnloaded;
       
    }
}
