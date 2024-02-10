using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Text.RegularExpressions;
using Xceed.Wpf.Toolkit;
using System.Linq;
using CustomExtensions;
using Microsoft.VisualBasic;
using System.Reflection;

namespace LayoutEditor.GUI
{
    partial class Builder
    {
        private static readonly JObject json = JObject.Parse(Utility.LoadInternalFile.TextFile("MyGUI_Trace.json"));
        private static JObject ElementList = null;
        private static readonly Dictionary<string, Action> CallBackList = [];
        public static JObject Trace => json;

        public static Border BuildPropertiesItem(string GUID)
        {
            // Init base container
            string widgetType = (string)ElementList[GUID]["type"];
            StackPanel main = new() {
                Orientation = Orientation.Vertical,
                Tag = GUID
            };

            // Title
            StackPanel titleStackPanel = new() {
                Orientation = Orientation.Horizontal
            };
            titleStackPanel.Children.Add(new TextBlock() {
                Foreground = Brushes.White,
                Margin = new Thickness(5, 2, 5, 2),
                FontSize = 24,
                Text = widgetType
            });
            main.Children.Add(titleStackPanel);

            // Attributes
            Dictionary<string, string[]> AttributeList = [];
            foreach (var item in json["Widget"][widgetType]["Attributes"].Cast<JProperty>())
            {
                List<string> content = [.. item.Value.Cast<string>()];
                AttributeList.Add(item.Name, [.. content]);
            }

            foreach (var (attrName, attrValue) in AttributeList)
            {
                if (!attrName.Equals("type") && !attrName.Equals("position"))
                {
                    string defaultAttrValue = attrValue[0];
                    bool isChecked = false;

                    if (ElementList.TryGetValue(GUID, StringComparison.InvariantCultureIgnoreCase, out var gvalue)
                        && gvalue is JObject jvalue
                        && jvalue.TryGetValue(attrName, StringComparison.InvariantCultureIgnoreCase, out var stringValue))
                    {
                        defaultAttrValue = stringValue.ToString();
                        isChecked = true;
                    }

                    DockPanel Attribute = new();

                    int defaultWidth = 125;

                    if (!attrName.Equals("position_real"))
                    {
                        CheckBox _cb = new() { IsChecked = isChecked, Margin = new Thickness(5, 2, 5, 2) };
                        _cb.Checked += EnableProperty;
                        _cb.Unchecked += DisableProperty;
                        Attribute.Children.Add(_cb);
                        defaultWidth -= 25;
                    }

                    Attribute.Children.Add(new TextBlock()
                    {
                        Foreground = Brushes.White,
                        Width = defaultWidth,
                        Margin = new Thickness(5, 2, 5, 2),
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = attrName
                    });

                    (FrameworkElement AttrPanel, Action<FrameworkElement> Hook) Container = attrName.ToLowerInvariant() switch
                    {
                        "position" or "position_real" => (new TextBlock()
                        {
                            Foreground = Brushes.White,
                            Background = new SolidColorBrush(Color.FromRgb(36, 35, 41)),
                            TextAlignment = TextAlignment.Center,
                            Margin = new Thickness(5, 2, 5, 2),
                            FontSize = 18,
                            Padding = new Thickness(5),
                            Text = defaultAttrValue
                        }, null),
                        "name" => (new TextBox()
                        {
                            Foreground = Brushes.Black,
                            Margin = new Thickness(5, 2, 5, 2),
                            Text = isChecked ? defaultAttrValue : string.Empty
                        }, ctrl => (ctrl as TextBox).TextChanged += TextChanged),
                        "skin" or _ => (new ComboBox()
                        {
                            Foreground = Brushes.Black,
                            Margin = new Thickness(5, 2, 5, 2),
                            SelectedIndex = int.TryParse(defaultAttrValue, out int index) ? index : -1
                        }, ctrl => {
                            ComboBox cb = ctrl as ComboBox;
                            Array.ForEach(attrValue, value => cb.Items.Add(new ComboBoxItem() { Content = value }));
                            cb.SelectionChanged += SelectionChanged;
                        })
                    };

                    if (Container.AttrPanel is not null)
                    {
                        Container.Hook?.Invoke(Container.AttrPanel);
                        Attribute.Children.Add(Container.AttrPanel);
                    }


                    if (attrName.Equals("position_real"))
                        main.Children.Insert(1, Attribute);
                    else
                        main.Children.Add(Attribute);
                    
                }
            }

            if (json["Widget"][widgetType] is JObject jType && jType.ContainsKey("Property"))
            {
                Dictionary<string, object> PropertyList = [];

                foreach (var widgetJProp in json["Widget"][widgetType]["Property"].Cast<JProperty>())
                {
                    if (widgetJProp.Value.Type == JTokenType.String)
                    {
                        PropertyList.Add(widgetJProp.Name, widgetJProp.Value.ToString());
                        continue;
                    }

                    if (widgetJProp.Value is not JArray jtypeArray)
                        continue;

                    string typeIndex1 = jtypeArray[0].ToStringLowerInvariant().Trim();
                    string typeIndex2 = jtypeArray[1].ToStringLowerInvariant().Trim();

                    bool typeIndexIsNum = typeIndex1.Contains("int", "float") && typeIndex2.Contains("int", "float");
                    if (jtypeArray.Count == 0 || jtypeArray.Count > 2 || !typeIndexIsNum)
                    {
                        PropertyList.Add(widgetJProp.Name, string.Empty);
                        continue;
                    }

                    string dtype = typeIndex1;
                    if (jtypeArray.Count > 1 && typeIndex2.Length > dtype.Length)
                        dtype = typeIndex2;

                    switch (dtype)
                    {
                        case "float":
                            PropertyList.Add(widgetJProp.Name, 0f);
                            break;
                        case "float, float, float, float":
                            PropertyList.Add(widgetJProp.Name, new float[4]);
                            break;
                        case "float, float, float":
                            PropertyList.Add(widgetJProp.Name, new float[3]);
                            break;
                        case "bool":
                            PropertyList.Add(widgetJProp.Name, true);
                            break;
                        default:
                            PropertyList.Add(widgetJProp.Name, string.Empty);
                            break;
                    }
                }

                foreach (var (propName, propValue) in PropertyList)
                {
                    string propStrValue = propValue.ToString();
                    bool isChecked = false;
                    
                    if (ElementList.TryGetValue(GUID, StringComparison.InvariantCultureIgnoreCase, out var gvalue)
                        && gvalue is JObject jvalue
                        && jvalue.TryGetValue(propName, StringComparison.InvariantCultureIgnoreCase, out var stringValue))
                    {
                        propStrValue = stringValue.ToString();
                        isChecked = true;
                    }

                    DockPanel Attribute = new();
                    CheckBox _cb = new() { IsChecked = isChecked, Margin = new Thickness(5, 2, 5, 2) };
                    _cb.Checked += EnableProperty;
                    _cb.Unchecked += DisableProperty;
                    
                    Attribute.Children.Add(_cb);
                    Attribute.Children.Add(new TextBlock()
                    {
                        Foreground = Brushes.White,
                        Margin = new Thickness(5, 2, 5, 2),
                        Width = 100,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = propName
                    });

                    float[] parsedColors = propValue is float[] pvFLarry ? pvFLarry : propValue is float pvFL ? [pvFL] : [];
                    byte[] colors = isChecked ? [.. parsedColors.Select(f => f * byte.MaxValue).Cast<byte>()] : [255, 255, 255];

                    // populates the attribute panel with the correct input type
                    (FrameworkElement AttrPanel, Action<FrameworkElement> Hook) Container = propValue switch
                    {
                        string => (new TextBox()
                        {
                            Foreground = Brushes.Black,
                            Margin = new Thickness(5, 2, 5, 2),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Text = propStrValue
                        }, tb => (tb as TextBox).TextChanged += TextChanged),
                        bool selectedIndex => (new ComboBox()
                        {
                            Foreground = Brushes.Black,
                            Margin = new Thickness(5, 2, 5, 2),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            SelectedIndex = selectedIndex ? 0 : 1
                        }, ctrl => {
                            ComboBox cb = ctrl as ComboBox;
                            cb.Items.Add(new ComboBoxItem() { Content = "true" });
                            cb.Items.Add(new ComboBoxItem() { Content = "false" });
                            cb.SelectionChanged += SelectionChanged;
                        }),
                        float or float[] => propName.ToLowerInvariant() switch
                        {
                            "color" or "colour" or "textcolour" or "textcolor" or "textshadowcolour" or "textshadowcolor" => (new ColorPicker()
                            {
                                Foreground = Brushes.Black,
                                SelectedColor = Color.FromRgb(colors[0], colors[1], colors[2]),
                                Margin = new Thickness(5, 2, 5, 2)
                            }, ctrl => (ctrl as ColorPicker).SelectedColorChanged += SelectedColorChanged),
                            "alpha" or _ => (NumberInputBuilder(parsedColors, 0, 999999999, parsedColors.Length), null)
                        },
                        _ => (null, null)
                    };

                    if (Container.AttrPanel is not null)
                    {
                        Container.Hook?.Invoke(Container.AttrPanel);
                        Attribute.Children.Add(Container.AttrPanel);
                    }

                    main.Children.Add(Attribute);
                }
            }

            return new Border()
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Top,
                Child = main
            };
        }

        private static void SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker cp = sender as ColorPicker;
            DockPanel propertyType = cp.Parent as DockPanel;
            StackPanel MainParent = propertyType.Parent as StackPanel;
            
            string GUID = MainParent.Tag.ToString();
            JObject properties = ElementList[GUID] as JObject;
            string index = (propertyType.Children[1] as TextBlock).Text;
            
            if (properties.ContainsKey(index))
            {
                properties[index] = $"{cp.SelectedColor.Value.R / 255f} {cp.SelectedColor.Value.G / 255f} {cp.SelectedColor.Value.B / 255f}";
                properties["Alpha"] = cp.SelectedColor.Value.A / 255f;
                (propertyType.Children[0] as CheckBox).IsChecked = true;
            }
            else
            {
                properties.Add(index, $"{cp.SelectedColor.Value.R / 255f} {cp.SelectedColor.Value.G / 255f} {cp.SelectedColor.Value.B / 255f}");
                properties["Alpha"] = cp.SelectedColor.Value.A / 255f;
                (propertyType.Children[0] as CheckBox).IsChecked = true;
            }

            CallBackList[GUID].Invoke();
        }

        private static StackPanel NumberInputBuilder(float[] values, float min = 0, float max = 0, int nOfNumberInput = 1)
        {
            StackPanel grid = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Stretch };
            for (int i = 0; i < nOfNumberInput; i++)
            {
                SingleUpDown p = new()
                {
                    Height = 20,
                    Value = values[i],
                    Minimum = min,
                    Maximum = max,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(5, 2, 5, 2)
                };
                p.PreviewTextInput += NumberValidationTextBox;
                p.ValueChanged += P_ValueChanged;
                grid.Children.Add(p);
            }
            return grid;
        }

        private static void P_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SingleUpDown numberBox = sender as SingleUpDown;
            StackPanel nbp = numberBox.Parent as StackPanel;
            DockPanel propertyType = nbp.Parent as DockPanel;

            StackPanel MainParent = propertyType.Parent as StackPanel;
            string GUID = MainParent.Tag.ToString();
            JObject properties = ElementList[GUID] as JObject;
            string index = (propertyType.Children[1] as TextBlock).Text;
            
            if (properties.ContainsKey(index))
            {
                properties[index] = numberBox.Value;
                (propertyType.Children[0] as CheckBox).IsChecked = true;
            }
            else
            {
                properties.Add(index, numberBox.Value);
                (propertyType.Children[0] as CheckBox).IsChecked = true;
            }

            CallBackList[GUID].Invoke();
        }

        private static void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = NumberMatch();
            e.Handled = regex.IsMatch(e.Text);
        }

        private static void TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            DockPanel propertyType = textBox.Parent as DockPanel;
            StackPanel MainParent = propertyType.Parent as StackPanel;

            string GUID = MainParent.Tag.ToString();
            JObject properties = ElementList[GUID] as JObject;
            string index = (propertyType.Children[1] as TextBlock).Text;

            if (properties.ContainsKey(index))
            {
                properties[index] = textBox.Text;
                (propertyType.Children[0] as CheckBox).IsChecked = true;
            }
            else
            {
                properties.Add(index, textBox.Text);
                (propertyType.Children[0] as CheckBox).IsChecked = true;
            }

            CallBackList[GUID].Invoke();
        }

        private static void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            DockPanel propertyType = comboBox.Parent as DockPanel;
            StackPanel MainParent = propertyType.Parent as StackPanel;

            string GUID = MainParent.Tag.ToString();
            JObject properties = ElementList[GUID] as JObject;
            string index = (propertyType.Children[1] as TextBlock).Text;

            if (e is null)
                return;

            if (properties.ContainsKey(index))
                properties[index] = (e.AddedItems[0] as ComboBoxItem).Content.ToString();
            else
                properties.Add(index, (e.AddedItems[0] as ComboBoxItem).Content.ToString());

            (propertyType.Children[0] as CheckBox).IsChecked = true;

            CallBackList[GUID].Invoke();
        }

        private static void DisableProperty(object sender, RoutedEventArgs e)
        {
            CheckBox comboBox = sender as CheckBox;
            DockPanel propertyType = comboBox.Parent as DockPanel;
            StackPanel MainParent = propertyType.Parent as StackPanel;

            string GUID = MainParent.Tag.ToString();
            JObject properties = ElementList[GUID] as JObject;
            string index = (propertyType.Children[1] as TextBlock).Text;
            
            if (properties.ContainsKey(index))
                properties.Remove(index);

            CallBackList[GUID].Invoke();
        }

        private static void EnableProperty(object sender, RoutedEventArgs e)
        {
            CheckBox comboBox = sender as CheckBox;
            DockPanel propertyType = comboBox.Parent as DockPanel;

            if (propertyType.Children[2] is TextBox)
                TextChanged(propertyType.Children[2], null);
            else if (propertyType.Children[2] is ComboBox)
                SelectionChanged(propertyType.Children[2], null);
        }

        public static void UpdateItemProperties(string GUID, string property, string value)
        {
            if (ElementList[GUID] is not JObject properties)
                return;
                
            if (properties.ContainsKey(property))
                properties[property] = value;
            else
                properties.Add(property, value);
            
        }

        public static Grid BuildToolBoxItem(string type, MainWindow App, MouseButtonEventHandler B_PreviewMouseDown, MouseButtonEventHandler B_PreviewMouseUp)
        {
            ElementList ??= [];

            string GUID = Guid.NewGuid().ToString();

            Grid g = new()
            {
                Tag = GUID,
                MinHeight = MainWindow.Get.MinGridSizeX,
                MinWidth = MainWindow.Get.MinGridSizeY,
                Cursor = Cursors.SizeAll,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            TextBox tb = new()
            {
                Text = type,
                FontSize = 24,
                Background = Brushes.Transparent,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                AcceptsReturn = true,
                AcceptsTab = true
            };

            Button b = new()
            {
                Background = Brushes.Transparent,
                Style = App.Resources["InvisibleButton"] as Style
            };

            g.PreviewMouseDown += B_PreviewMouseDown;
            g.PreviewMouseUp += B_PreviewMouseUp;

            g.Children.Add(tb);
            g.Children.Add(b);
            g.Children.Add(new Grid());

            JObject j = new() { { "type", type } };
            ElementList.Add(GUID, j);
            // CallBackList.Add(GUID, UpdatedProperties);
            return g;
        }

        public static JObject GetXmlElementList() => ElementList;

        public static Grid GetParentUntil(Grid child, Grid stop)
        {
            if (child.Parent is not Grid g)
                return null;

            if (g.Equals(stop) || child.Equals(stop))
                return null;

            return g;
        }

        public static XmlElement GetElementById(XmlElement root, string id)
        {
            if (root.GetAttribute("_xx_unique_guid").Equals(id))
                return root;

            if (!root.HasChildNodes)
                return null;
            
            foreach (var node in root.ChildNodes)
            {
                if (node is not XmlElement xlm)
                    continue;

                if (xlm.GetAttribute("_xx_unique_guid").Equals(id))
                    return xlm;

                if (!xlm.HasChildNodes)
                    continue;

                XmlElement ret = GetElementById(xlm, id);
                if (ret is not null)
                    return ret;
            }

            return null;
        }

        [GeneratedRegex("[^0-9]+")]
        private static partial Regex NumberMatch();
    }
}
