using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SM_Layout_Editor.GUI
{
    class Builder
    {
        public static Border BuildPropertiesItem(string info, TextChangedEventHandler Tb_TextChanged)
        {
            var s = new StackPanel()
            {
                Orientation = Orientation.Horizontal
            };
            s.Children.Add(new TextBlock()
            {
                Foreground = Brushes.White,
                Margin = new Thickness(5, 2, 5, 2),
                Text = "Text"
            });
            var tb = new TextBox()
            {
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Text = info
            };
            tb.TextChanged += Tb_TextChanged;
            s.Children.Add(tb);
            return new Border()
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Top,
                Child = s
            };
        }
        public static Grid BuildToolBoxItem(string type, MainWindow App, MouseButtonEventHandler B_PreviewMouseDown, MouseButtonEventHandler B_PreviewMouseUp)
        {
            Grid g = new()
            {
                Tag = type,
                MinHeight = MainWindow.GridSize,
                MinWidth = MainWindow.GridSize,
                Cursor = Cursors.SizeAll,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            TextBox tb = new()
            {
                Text = type,
                FontSize = 24,
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

            return g;
        }
    }
}
