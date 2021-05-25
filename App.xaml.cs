using System;
using System.Windows;
using Microsoft.ApplicationInsights;
using MyToolkit.Composition;
using MyToolkit.Messaging;
using MyToolkit.Mvvm;
using MyToolkit.Storage;
using MyToolkit.UI;
using SM_Layout_Editor.Json.Localization;
using SM_Layout_Editor.Messages;

namespace SM_Layout_Editor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>Raises the <see cref="E:System.Windows.Application.Startup"/> event. </summary>
        /// <param name="e">A <see cref="T:System.Windows.StartupEventArgs"/> that contains the event data.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            ServiceLocator.Default.RegisterSingleton<IDispatcher, UiDispatcher>(new UiDispatcher(Dispatcher));
        }
    }
}
