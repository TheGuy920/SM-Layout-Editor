using System;
using System.Windows;
using Microsoft.Win32;
using MyToolkit.Composition;
using MyToolkit.Messaging;
using MyToolkit.Mvvm;
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

            Messenger.Default.Register(DefaultActions.GetTextMessageAction());
            Messenger.Default.Register<OpenJsonDocumentMessage>(OpenJsonDocument);
            Messenger.Default.Register<SaveJsonDocumentMessage>(SaveJsonDocument);

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            //Telemetry.TrackException(args.ExceptionObject as Exception);
        }

        private void SaveJsonDocument(SaveJsonDocumentMessage msg)
        {
            var dlg = new SaveFileDialog();
            dlg.FileName = msg.FileName;
            dlg.Filter = Strings.FileDialogFilter;
            dlg.RestoreDirectory = true;
            dlg.AddExtension = true;
            if (dlg.ShowDialog() == true)
            {
                msg.CallSuccessCallback(dlg.FileName);
                //Telemetry.TrackEvent("FileSave");
            }
            else
                msg.CallFailCallback(null);
        }

        private void OpenJsonDocument(OpenJsonDocumentMessage msg)
        {
            var dlg = new OpenFileDialog();
            dlg.Title = msg.Title;
            dlg.Filter = Strings.FileDialogFilter;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == true)
            {
                msg.CallSuccessCallback(dlg.FileName);
                //Telemetry.TrackEvent("FileOpen");
            }
            else
                msg.CallFailCallback(null);
        }
    }
}
