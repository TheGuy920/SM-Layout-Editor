using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Xml.Linq;

namespace LayoutEditor.Font
{
    public class FontInfo
    {
        public List<string> Tags { get; init; }
        public FontFamily FontFamily { get; init; }
        public FileInfo FontFile { get; init; }
        public double FontSize { get; init; }
        private PrivateFontCollection FontCollection { get; init; } = new PrivateFontCollection();
        public FontInfo(FileInfo fontFile, double fontSize, List<string> tags)
        {
            this.FontFile = fontFile;
            this.FontSize = fontSize;
            this.Tags = tags ?? new();
            // Load the font into the PrivateFontCollection
            this.FontCollection.AddFontFile(this.FontFile.FullName);
            // Create a FontFamily in WPF using the font's name
            var fontFamilyName = this.FontCollection.Families.First().Name;
            this.FontFamily = new FontFamily(fontFamilyName);
        }

        public static Dictionary<string, FontInfo> LoadFontInformation(string searchDir)
        {
            var fontFile1 = Utility.LoadInternalFile.TextFile("Fonts.xml");
            var fontFile2 = Utility.LoadInternalFile.TextFile("ManualFontDataInput.xml");

            var xDocument = XDocument.Parse(fontFile1);
            var fontResources = xDocument.Descendants("Resource")
                                         .Where(r => (string)r.Attribute("type") == "ResourceTrueTypeFont");

            var xDocument2 = XDocument.Parse(fontFile2);
            var fontDataDict = new Dictionary<string, List<string>>();
            foreach (var fontElement in xDocument2.Descendants("MyGUI").Elements("Font"))
            {
                var fontName = fontElement.Attribute("name")?.Value;
                if (!string.IsNullOrWhiteSpace(fontName))
                {
                    var tags = fontElement.Descendants("Data")
                                          .Where(d => (string)d.Attribute("type") == "Tag")
                                          .Select(d => d.Attribute("value")?.Value)
                                          .ToList();
                    if (!fontDataDict.ContainsKey(fontName))
                        fontDataDict.Add(fontName, tags);
                    else
                        fontDataDict[fontName].AddRange(tags);
                }
            }

            var fonts = new Dictionary<string, FontInfo>();

            foreach (var fontResource in fontResources)
            {
                string specialName = (string)fontResource.Attribute("name");
                string fileName = fontResource.Elements("Property")
                                              .FirstOrDefault(p => (string)p.Attribute("key") == "Source")
                                              ?.Attribute("value")?.Value;

                DirectoryInfo search = new(searchDir);

                FileInfo fontPath = !string.IsNullOrEmpty(fileName)
                                  ? search.GetFiles(fileName, SearchOption.AllDirectories).FirstOrDefault()
                                  : null;

                double? fontSize = (double?)fontResource.Elements("Property")
                                                    .FirstOrDefault(p => (string)p.Attribute("key") == "Size")
                                                    ?.Attribute("value");

                if (fontPath.Exists && !fonts.ContainsKey(specialName))
                {
                    var tags = fontDataDict.TryGetValue(specialName, out var t) ? t : null;
                    var fontInfo = new FontInfo(fontPath, fontSize ?? 12, tags);
                    fonts.Add(specialName, fontInfo);
                }
            }

            return fonts;
        }
    }
}
