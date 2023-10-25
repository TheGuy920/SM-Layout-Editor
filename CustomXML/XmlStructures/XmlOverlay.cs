using CustomExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LayoutEditor.CustomXML
{
    public class XmlOverlay : Grid
    {
        public new bool IsEnabled = true;
        private readonly List<Button> OverlayButtons = new();
        public event EventHandler DisplayItemChanged;
        public List<XmlDOM> BindingPair { get; private set; }
        private double Scale = 1;
        private double GridSize = 1;
        private bool IsScalingSelf = false;
        private int ScaleMode = -1;
        private Point MouseStart = new(0, 0);

        public XmlOverlay(double GridSize)
        {
            this.GridSize = GridSize;
            this.BindingPair = new();
            this.Init();
        }

        public void BindToDOM(XmlDOM dom, bool add = false)
        {
            if (add)
            {
                if (this.BindingPair.Contains(dom))
                {
                    dom.UnSelected();
                    this.BindingPair.Remove(dom);
                }
                else
                {
                    dom.Selected();
                    this.BindingPair.Add(dom);
                }
                this.UpdateSize();
            }
            else
            {
                if (!this.BindingPair.Contains(dom))
                {
                    if (this.BindingPair.Count > 0)
                    {
                        this.BindingPair.ForEach(d => d.UnSelected());
                        this.BindingPair.Clear();
                    }
                    if (dom is not null)
                    {
                        dom.Selected();
                        this.BindingPair.Add(dom);
                        this.UpdateSize();
                    }
                }
            }
            if (this.BindingPair.Count is 0)
                this.Visibility = Visibility.Collapsed;
            else
                this.Visibility = Visibility.Visible;
        }

        internal void UpdateSize(bool sender = false)
        {
            if (this.IsEnabled)
            {
                if (this.BindingPair.Count > 0)
                {
                    Thickness MarginSize = new(-1);
                    Thickness ActualSize = new(-1);

                    foreach (var Pair in this.BindingPair)
                    {
                        Grid main = Pair.Children[0] as Grid;

                        Point p = main.TransformToVisual(this).Transform(new(0, 0));

                        Thickness TargetSize = new(
                            this.Margin.Left + p.X,
                            this.Margin.Top + p.Y,
                            main.ActualWidth * this.Scale,
                            main.ActualHeight * this.Scale
                        );

                        MarginSize.MinMargin(TargetSize.Truncate(2));
                        ActualSize.MaxMargin(TargetSize.Truncate(2));
                    }

                    var change2 = this.SetMarginLT(MarginSize.Truncate(2));

                    ActualSize = new(0, 0,
                        (ActualSize.Left - MarginSize.Left) + ActualSize.Right,
                        (ActualSize.Top - MarginSize.Top) + ActualSize.Bottom
                        );

                    var change1 = this.SetWidthAndHeight(ActualSize.Truncate(2));

                    if ((change1 || change2) && sender is not true)
                        this.DisplayItemChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        private void Init()
        {
            // Init Grid
            this.Width = 1;
            this.Height = 1;
            this.Background = null;
            this.ClipToBounds = false;
            this.Visibility = Visibility.Visible;
            this.VerticalAlignment = VerticalAlignment.Top;
            this.HorizontalAlignment = HorizontalAlignment.Left;
            // Column Def
            this.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(7, GridUnitType.Pixel) });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(90, GridUnitType.Star) });
            this.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(7, GridUnitType.Pixel) });
            // Row Def
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(7, GridUnitType.Pixel) });
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(90, GridUnitType.Star) });
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(7, GridUnitType.Pixel) });
            // Border
            Border rec = new() { BorderBrush = Brushes.LightBlue, BorderThickness = new(1), Background = null, Margin = new(-8.5), ClipToBounds = false };
            // Column and row (span)
            Grid.SetRow(rec, 1);
            Grid.SetColumn(rec, 1);
            // Grid.SetRowSpan(rec, 3);
            // Grid.SetColumnSpan(rec, 3);
            // Add border
            this.Children.Add(rec);
            // row and column list
            ReadOnlySpan<int> ColumnList = new int[] { 0, 1, 2, 2, 2, 1, 0, 0 };
            ReadOnlySpan<int> RowList = new int[] { 0, 0, 0, 1, 2, 2, 2, 1 };
            int fac = 5;
            // buttnn margin list
            ReadOnlySpan<Thickness> BorderList = new Thickness[]
            {
                new(-2.5 * fac, -2.5 * fac, 0, 0),
                new(0, -2.5 * fac, 0, 0),
                new(0, -2.5 * fac, -2.5 * fac, 0),
                new(0, 0, -2.5 * fac, 0),
                new(0, 0, -2.5 * fac, -2.5 * fac),
                new(0, 0, 0, -2.5 * fac),
                new(-2.5 * fac, 0, 0, -2.5 * fac),
                new(-2.5 * fac, 0, 0, 0)
            };
            ReadOnlySpan<Brush> ColorList = new[]{ Brushes.White, Brushes.White };
            // cursor list
            ReadOnlySpan<Cursor> CursorList = new Cursor[] { Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE };
            // Add scale Buttons
            this.OverlayButtons.Clear();
            for (int i = 0; i < 8; i++)
            {
                Button button = new()
                {
                    Style = Application.Current.FindResource("ScaleButton") as Style,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = ColorList[i % 2],
                    Cursor = CursorList[i % 4],
                    Margin = BorderList[i],
                    Height = 5,
                    Width = 5,
                    Tag = i + 1
                };
                button.PreviewMouseDown += this.MouseDown;
                Grid.SetColumn(button, ColumnList[i]);
                Grid.SetRow(button, RowList[i]);
                this.OverlayButtons.Add(button);
                this.Children.Add(button);
            }
        }
        public void Show()
        {
            if (!this.IsEnabled)
            {
                this.Visibility = Visibility.Visible;
                this.IsEnabled = true;
                this.UpdateSize();
            }
        }

        public void Hide()
        {
            if (this.IsEnabled)
            {
                this.Visibility = Visibility.Collapsed;
                this.IsEnabled = false;
            }
        }

        public void ChangeScale(object sender, double e)
        {
            this.Scale = e;
            this.UpdateSize(true);
        }

        public new void MouseMove(Point mouse_start, MouseEventArgs e, bool mouse_down)
        {
            if (this.IsScalingSelf)
            {
                Point Diff = e.GetPosition(this).Divide(this.GridSize);

                ActualSize NewSize = new(this.Width, this.Height);

                bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
                bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
                double factor = 1;

                if (this.ScaleMode % 2 == 0)
                {
                    switch (this.ScaleMode)
                    {
                        case 2:
                            NewSize.Top = Diff.Y;
                            NewSize.Height = Math.Max(this.ActualHeight - Diff.Y, MainWindow.Get.MinGridSizeY);
                            break;
                        case 4:
                            NewSize.Width = Math.Max(this.ActualWidth + (Diff.X - this.Width), MainWindow.Get.MinGridSizeX);
                            break;
                        case 6:
                            NewSize.Height = Math.Max(this.ActualHeight + (Diff.Y - this.Height), MainWindow.Get.MinGridSizeY);
                            break;
                        case 8:
                            NewSize.Left = Diff.X;
                            NewSize.Width = Math.Max(this.ActualWidth - Diff.X, MainWindow.Get.MinGridSizeX);
                            break;
                    }
                }
                else
                {
                    if (ctrl && !shift) factor = 0.5;
                    switch (this.ScaleMode)
                    {
                        case 1:
                            NewSize.Top = Diff.Y;
                            NewSize.Height = Math.Max(this.ActualHeight - (Diff.Y / factor), MainWindow.Get.MinGridSizeY);
                            NewSize.Left = Diff.X;
                            NewSize.Width = Math.Max(this.ActualWidth - (Diff.X / factor), MainWindow.Get.MinGridSizeX);
                            break;
                        case 3:
                            if (ctrl && !shift) NewSize.Left = -(Diff.X - this.Width) * factor;
                            NewSize.Top = Diff.Y;
                            NewSize.Height = Math.Max(this.ActualHeight - (Diff.Y / factor), MainWindow.Get.MinGridSizeY);
                            NewSize.Width = Math.Max(this.ActualWidth + (Diff.X - this.Width), MainWindow.Get.MinGridSizeX);
                            break;
                        case 5:
                            if (ctrl && !shift)
                            {
                                NewSize.Left = -(Diff.X - this.Width) * factor;
                                NewSize.Top = -(Diff.Y - this.Height) * factor;
                            }
                            NewSize.Height = Math.Max(this.ActualHeight + (Diff.Y - this.Height), MainWindow.Get.MinGridSizeY);
                            NewSize.Width = Math.Max(this.ActualWidth + (Diff.X - this.Width), MainWindow.Get.MinGridSizeX);
                            break;
                        case 7:
                            NewSize.Height = Math.Max(this.ActualHeight + (Diff.Y - this.Height), MainWindow.Get.MinGridSizeY);
                            NewSize.Left = Diff.X * factor;
                            if (ctrl && !shift) NewSize.Top = -(Diff.Y - this.Height) * factor;
                            NewSize.Width = Math.Max(this.ActualWidth - Diff.X, MainWindow.Get.MinGridSizeX);
                            break;
                    }
                }

                this.Dispatcher.Invoke(() => 
                {
                    this.UpdatePairs(new()
                    {
                        Width = (NewSize.Width - this.Width) / this.Scale,
                        Height = (NewSize.Height - this.Height) / this.Scale,
                        Left = NewSize.Left / this.Scale,
                        Top = NewSize.Top / this.Scale
                    });
                });

                if (this.SetSize(NewSize))
                    this.DisplayItemChanged.Invoke(null, EventArgs.Empty);
            }
            else if (BindingPair.Count > 0 && mouse_down)
            {
                this.Dispatcher.Invoke(() =>
                {
                    Point Diff = (Point)(e.GetPosition(this) - mouse_start);
                    this.UpdatePairs(new()
                    {
                        Left = Diff.X / this.Scale,
                        Top = Diff.Y / this.Scale
                    });
                    if (this.AddMarginLT(Diff.X, Diff.Y))
                        this.DisplayItemChanged.Invoke(null, EventArgs.Empty);
                });
            }
        }

        public void MoveByPixels(Point diff)
        {
            this.UpdatePairs(new()
            {
                Left = diff.X / this.Scale,
                Top = diff.Y / this.Scale
            });
            this.AddMarginLT(diff.X, diff.Y);
        }

        private void UpdatePairs(ActualSize diff)
        {
            this.BindingPair.ForEach(dom =>
            {
                dom.UpdateSizeByPixels(diff);
            });
        }

        public new void MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Set scaling boolean
            this.IsScalingSelf = false;
        }

        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed && e.RightButton != MouseButtonState.Pressed)
            {
                // Fetch the scale mode
                this.ScaleMode = int.Parse((sender as Button).Tag.ToString());
                // Set scaling boolean
                this.IsScalingSelf = true;
                // end
                e.Handled = true;
            }
        }
    }
}