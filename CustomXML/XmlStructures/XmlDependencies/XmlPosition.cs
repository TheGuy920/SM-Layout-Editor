using CustomExtensions;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LayoutEditor.CustomXML
{
    public class XmlPosition
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public XmlPosition(double[] fA)
        {
            Left = fA[0];
            Top = fA[1];
            Width = fA[2];
            Height = fA[3];
        }
        public XmlPosition(double x, double y, double width, double height)
        {
            Left = x;
            Top = y;
            Width = width;
            Height = height;
        }
        public XmlPosition(ActualSize size)
        {
            Left = size.Left;
            Top = size.Top;
            Width = size.Width;
            Height = size.Height;
        }
        public IEnumerable<ColumnDefinition> GetWidth()
        {
            List<ColumnDefinition> columns = new();
            double Right =  Math.Max(1f - (Left + Width), 0);
            columns.Add(new ColumnDefinition() { Width = new GridLength(Left, GridUnitType.Star) });
            columns.Add(new ColumnDefinition() { Width = new GridLength(Width, GridUnitType.Star) });
            columns.Add(new ColumnDefinition() { Width = new GridLength(Right, GridUnitType.Star) });
            return columns;
        }
        public IEnumerable<RowDefinition> GetHeight()
        {
            List<RowDefinition> rows = new();
            double Bottom = Math.Max(1f - (Top + Height), 0);
            rows.Add(new RowDefinition() { Height = new GridLength(Top, GridUnitType.Star) });
            rows.Add(new RowDefinition() { Height = new GridLength(Height, GridUnitType.Star) });
            rows.Add(new RowDefinition() { Height = new GridLength(Bottom, GridUnitType.Star) });
            return rows;
        }
        public static XmlPosition Parse(string pos)
        {
            return new XmlPosition(
                Array.ConvertAll(
                    pos.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), p => double.Parse(p)
                ));
        }
        public override string ToString()
        {
            return $"{Left} {Top} {Width} {Height}";
        }
    }
}
