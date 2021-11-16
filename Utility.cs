using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace SM_Layout_Editor
{
    public enum MenuState
    {
        Closed = 0,
        Settings = 1,
        File = 2,
        View = 3,
        Library = 4
    }
    class Utility
    {
        public static ContentControl FindContentControl(string s)
        {
            return (ContentControl)MainWindow.Get.FindResource(s);
        }
        public static T GetRegVal<T>(string path, string value)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(path);
                if (key != null)
                    if (typeof(T) == typeof(bool))
                        return (T)(object)Convert.ToBoolean((int)key.GetValue(value));
                    else
                        return (T)key.GetValue(value);
                else
                    throw new();
            }
            catch (Exception)
            {
                return default;
            }
        }
        public static class LoadInternalFile
        {
            public static string TextFile(string resourceName)
            {
                string ResourceFileName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith(resourceName));
                return new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceFileName)).ReadToEnd();
            }
            public static IHighlightingDefinition HighlightingDefinition(string resourceName)
            {
                string ResourceFileName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith(resourceName));
                var stream = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceFileName));
                using var reader = new XmlTextReader(stream);
                return HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }
    }
    public class MouseUtil
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
        public static Point GetMousePosition()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }
        public static Vector GetMouseMovement(bool resetPosition = false, bool MoveSensitivity = false, bool Scale = false, bool GridSize = false)
        {
            Vector returnVec = GetMousePosition() - MainWindow.MouseStart;
            if (MoveSensitivity)
                returnVec *= MainWindow.MoveSensitivity;
            if (Scale)
                returnVec /= MainWindow.Scale;
            if (GridSize)
                returnVec /= MainWindow.GridSize;
            if(resetPosition)
                ResetMousePos();
            return returnVec;
        }
        public static void ResetMousePos()
        {
            MainWindow.MouseStart = GetMousePosition();
        }
    }
}
