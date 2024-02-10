using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Win32;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Xml;
using System.Text;

namespace LayoutEditor
{
    public enum MenuState
    {
        Closed = 0,
        Settings = 1,
        File = 2,
        View = 3,
        Library = 4
    }

    internal class Utility    
    {
        public static T GetRegVal<T>(string path, string value)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(path) ?? throw new();
                
                if (typeof(T) == typeof(bool))
                    return (T)(object)Convert.ToBoolean((int)key.GetValue(value));
                else
                    return (T)key.GetValue(value);
                    
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

            public static IHighlightingDefinition FormatHighlightingDefinition(string resourceName, string pattern, string format)
            {
                string ResourceFileName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith(resourceName));
                var stream = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceFileName));
                string content = stream.ReadToEnd().Replace(pattern, format);
                using var reader = new XmlTextReader(new MemoryStream(Encoding.UTF8.GetBytes(content)));
                return HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }
    }

    public partial class MouseUtil
    {
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetCursorPos(ref Win32Point pt);

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
    }
}
