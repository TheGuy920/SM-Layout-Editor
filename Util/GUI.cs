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

namespace LayoutEditor.GUI
{
    partial class Builder
    {
        private static readonly JObject json = JObject.Parse(Utility.LoadInternalFile.TextFile("MyGUI_Trace.json"));
        private static JObject ElementList = null;
        private static readonly Dictionary<string, Action> CallBackList = new();
        public static JObject getTrace()
        {
            return json;
        }
        public static Border BuildPropertiesItem(string GUID)
        {
            string type = (string)ElementList[GUID]["type"];

            StackPanel main = new() {
                Orientation = Orientation.Vertical,
                Tag = GUID
            };
            
            StackPanel s = new() {
                Orientation = Orientation.Horizontal
            };
            
            s.Children.Add(new TextBlock() {
                Foreground = Brushes.White,
                Margin = new Thickness(5, 2, 5, 2),
                FontSize = 24,
                Text = type
            });
            
            main.Children.Add(s);

            Dictionary<string, string[]> AttributeList = new();
            foreach (JProperty item in json["Widget"][type]["Attributes"])
            {
                List<string> content = new();
                foreach (string value in item.Value)
                {
                    content.Add(value);
                }
                AttributeList.Add(item.Name, content.ToArray());
            }
            foreach (var item in AttributeList)
            {
                if (!item.Key.Equals("type") && !item.Key.Equals("position"))
                {
                    object cValue = item.Value[0];
                    bool isChecked = false;
                    if (ElementList.ContainsKey(GUID))
                    {
                        if ((ElementList[GUID] as JObject).ContainsKey(item.Key))
                        {
                            cValue = ElementList[GUID][item.Key];
                            isChecked = true;
                        }
                    }

                    DockPanel Attribute = new();

                    int w = 125;

                    if (!item.Key.Equals("position_real"))
                    {
                        CheckBox _cb = new() { IsChecked = isChecked, Margin = new Thickness(5, 2, 5, 2) };
                        _cb.Checked += EnableProperty;
                        _cb.Unchecked += DisableProperty;
                        Attribute.Children.Add(_cb);
                        w -= 25;
                    }

                    Attribute.Children.Add(new TextBlock()
                    {
                        Foreground = Brushes.White,
                        Width = w,
                        Margin = new Thickness(5, 2, 5, 2),
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = item.Key
                    });                    

                    switch (item.Key.ToLower())
                    {
                        case "position":
                        case "position_real":
                            Attribute.Children.Add(new TextBlock()
                            {
                                Foreground = Brushes.White,
                                Background = new SolidColorBrush(Color.FromRgb(36, 35, 41)),
                                TextAlignment = TextAlignment.Center,
                                Margin = new Thickness(5, 2, 5, 2),
                                FontSize = 18,
                                Padding = new Thickness(5),
                                Text = cValue.ToString()
                            });
                            break;
                        case "name":
                        default:
                            if (item.Value.Length > 1 && !item.Key.Equals("name") || item.Key.Equals("skin"))
                            {
                                ComboBox cb = new()
                                {
                                    Foreground = Brushes.Black,
                                    Margin = new Thickness(5, 2, 5, 2)
                                };
                                foreach (string value in item.Value)
                                {
                                    cb.Items.Add(new ComboBoxItem() { Content = value });
                                }
                                int index = -1;
                                if(isChecked && int.TryParse(cValue.ToString(), out index)) { }
                                cb.SelectedIndex = index;

                                cb.SelectionChanged += SelectionChanged;
                                Attribute.Children.Add(cb);
                            }
                            else
                            {
                                if (!isChecked)
                                    cValue = "";
                                var tb = new TextBox()
                                {
                                    Foreground = Brushes.Black,
                                    Margin = new Thickness(5, 2, 5, 2),
                                    Text = cValue.ToString()
                                };
                                tb.TextChanged += TextChanged;
                                Attribute.Children.Add(tb);
                            }
                            break;
                    }
                    if (item.Key.Equals("position_real"))
                    {
                        main.Children.Insert(1, Attribute);
                    }
                    else
                    {
                        main.Children.Add(Attribute);
                    }
                }
            }
            if ((json["Widget"][type] as JObject).ContainsKey("Property"))
            {
                Dictionary<string, object> PropertyList = new();
                foreach (JProperty item in json["Widget"][type]["Property"])
                {
                    
                    string str = "";
                    if (item.Value.Type == JTokenType.String)
                    {
                        str = item.Value.ToString();
                        PropertyList.Add(item.Name, str);
                    }
                    else
                    {
                        JArray jry = item.Value as JArray;
                        if (jry.Count == 1 || jry.Count == 2 && (
                            jry[0].ToString().ToLower().Contains("int") ||
                            jry[0].ToString().ToLower().Contains("float")
                            ) && (
                            jry[1].ToString().ToLower().Contains("int") ||
                            jry[1].ToString().ToLower().Contains("float")
                            ))
                        {
                            string sk = jry[0].ToString().ToLower().Trim();
                            if(jry.Count > 1 && jry[1].ToString().ToLower().Trim().Length > sk.Length)
                                sk = jry[1].ToString().ToLower().Trim();

                            switch (sk)
                            {
                                case "float":
                                    PropertyList.Add(item.Name, 0f);
                                    break;
                                case "float, float, float, float":
                                    PropertyList.Add(item.Name, new float[4]);
                                    break;
                                case "float, float, float":
                                    PropertyList.Add(item.Name, new float[3]);
                                    break;
                                case "bool":
                                    PropertyList.Add(item.Name, true);
                                    break;
                                default:
                                    PropertyList.Add(item.Name, string.Empty);
                                    break;
                            }
                        }
                        else
                        {
                            PropertyList.Add(item.Name, string.Empty);
                        }
                    }
                }
                foreach (var item in PropertyList)
                {
                    object cValue = item.Value;
                    bool isChecked = false;
                    if (ElementList.ContainsKey(GUID))
                    {
                        if ((ElementList[GUID] as JObject).ContainsKey(item.Key))
                        {
                            cValue = ElementList[GUID][item.Key];
                            isChecked = true;
                        }
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
                        Text = item.Key
                    });
                    if (item.Value is string)
                    {

                        var tb = new TextBox()
                        {
                            Foreground = Brushes.Black,
                            Margin = new Thickness(5, 2, 5, 2),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Text = cValue.ToString()
                        };
                        tb.TextChanged += TextChanged;
                        Attribute.Children.Add(tb);
                    }
                    else if (item.Value is bool)
                    {
                        ComboBox cb = new()
                        {
                            Foreground = Brushes.Black,
                            Margin = new Thickness(5, 2, 5, 2),
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };
                        cb.Items.Add(new ComboBoxItem() { Content = "true" });
                        cb.Items.Add(new ComboBoxItem() { Content = "false" });

                        cb.SelectedIndex = -1;

                        if (isChecked && cValue.ToString().ToLower().Equals("true"))
                        {
                            cb.SelectedIndex = 0;
                        }
                        else if (cValue.ToString().ToLower().Equals("false"))
                        {
                            cb.SelectedIndex = 1;
                        }

                        cb.SelectionChanged += SelectionChanged;
                        Attribute.Children.Add(cb);
                    }
                    else if (item.Value is float or float[])
                    {
                        string z = item.Key.ToLower();
                        if (z.Equals("color") || z.Equals("colour") ||
                            z.Equals("textcolour") || z.Equals("textcolor") ||
                            z.Equals("textshadowcolour") || z.Equals("textshadowcolor"))
                        {
                            byte[] colors = new byte[3]{ 255, 255, 255 };
                            if (isChecked)
                            {
                                colors = Array.ConvertAll(cValue.ToString().Split(" "), s =>
                                {
                                    Debug.WriteLine(s);
                                    return (byte)(float.Parse(s) * 255);
                                });
                            }

                            ColorPicker cp = new()
                            {
                                Foreground = Brushes.Black,
                                SelectedColor = Color.FromRgb(colors[0] , colors[1], colors[2]),
                                Margin = new Thickness(5, 2, 5, 2)
                            };
                            cp.SelectedColorChanged += SelectedColorChanged;
                            Attribute.Children.Add(cp);
                        }
                        else
                        {
                            float min = 0;
                            float max = 999999999;

                            if (item.Key.ToLower().Equals("alpha"))
                                continue;

                            float[] kpls = Array.Empty<float>();

                            if (item.Value as float? == null)
                                kpls = Array.ConvertAll(cValue.ToString().Split(" "), s => float.Parse(s));
                            else
                                kpls = new[] { float.Parse(cValue.ToString()) };

                            Attribute.Children.Add(NumberInputBuilder(kpls, min, max, kpls.Length));
                        }
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
            if (properties.ContainsKey(index))
            {
                if (e != null)
                {
                    properties[index] = (e.AddedItems[0] as ComboBoxItem).Content.ToString();
                    (propertyType.Children[0] as CheckBox).IsChecked = true;
                }
            }
            else
            {
                if (e != null)
                {
                    properties.Add(index, (e.AddedItems[0] as ComboBoxItem).Content.ToString());
                    (propertyType.Children[0] as CheckBox).IsChecked = true;
                }
            }
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
            {
                TextChanged(propertyType.Children[2], null);
            }
            else if (propertyType.Children[2] is ComboBox)
            {
                SelectionChanged(propertyType.Children[2], null);
            }
            
        }

        public static void UpdateItemProperties(string GUID, string property, string value)
        {
            if (ElementList[GUID] != null)
            {
                JObject properties = ElementList[GUID] as JObject;
                if (properties.ContainsKey(property))
                {
                    properties[property] = value;
                }
                else
                {
                    properties.Add(property, value);
                }
            }
        }

        public static Grid BuildToolBoxItem(string type, MainWindow App, MouseButtonEventHandler B_PreviewMouseDown, MouseButtonEventHandler B_PreviewMouseUp)
        {
            ElementList ??= new();

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

            JObject j = new()
            {
                { "type", type }
            };
            ElementList.Add(GUID, j);
            //CallBackList.Add(GUID, UpdatedProperties);
            return g;
        }
        public static JObject GetXmlElementList()
        {
            return ElementList;
        }
        public static Grid GetParentUntil(Grid child, Grid stop)
        {
            if (child.Equals(stop))
                return null;
            Grid ret = child.Parent as Grid;
            if (ret != null)
                if (ret.Equals(stop))
                    return null;
            return ret;
        }
        public static XmlElement GetElementById(XmlElement root, string id)
        {
            if (root.GetAttribute("_xx_unique_guid").Equals(id))
            {
                return root;
            }
            if (root.HasChildNodes)
            {
                foreach (var node in root.ChildNodes)
                {
                    if (node is XmlElement)
                    {
                        XmlElement xlm = node as XmlElement;
                        if (xlm.GetAttribute("_xx_unique_guid").Equals(id))
                        {
                            return xlm;
                        }
                        if (xlm.HasChildNodes)
                        {
                            XmlElement ret = GetElementById(xlm, id);
                            if (ret != null)
                            {
                                return ret;
                            }
                        }
                    }
                }
            }
            return null;
        }

        [GeneratedRegex("[^0-9]+")]
        private static partial Regex NumberMatch();
    }
}
