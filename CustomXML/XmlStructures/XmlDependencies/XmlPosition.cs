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
            this.Left = fA[0];
            this.Top = fA[1];
            this.Width = fA[2];
            this.Height = fA[3];
        }

        public XmlPosition(double x, double y, double width, double height)
        {
            this.Left = x;
            this.Top = y;
            this.Width = width;
            this.Height = height;
        }

        public XmlPosition(ActualSize size)
        {
            this.Left = size.Left;
            this.Top = size.Top;
            this.Width = size.Width;
            this.Height = size.Height;
        }

        public IEnumerable<ColumnDefinition> GetWidth()
        {
            List<ColumnDefinition> columns = [];
            
            double Right =  Math.Max(1f - (this.Left + this.Width), 0);
            columns.Add(new ColumnDefinition() { Width = new GridLength(this.Left.Min(0), GridUnitType.Star) });
            columns.Add(new ColumnDefinition() { Width = new GridLength(this.Width, GridUnitType.Star) });
            columns.Add(new ColumnDefinition() { Width = new GridLength(Right, GridUnitType.Star) });
            
            return columns;
        }

        public IEnumerable<RowDefinition> GetHeight()
        {
            List<RowDefinition> rows = [];
            
            double Bottom = Math.Max(1f - (this.Top + this.Height), 0);
            rows.Add(new RowDefinition() { Height = new GridLength(this.Top.Min(0), GridUnitType.Star) });
            rows.Add(new RowDefinition() { Height = new GridLength(this.Height, GridUnitType.Star) });
            rows.Add(new RowDefinition() { Height = new GridLength(Bottom, GridUnitType.Star) });
            
            return rows;
        }

        public static implicit operator XmlPosition(double[] arry) => new(arry);

        public static XmlPosition Parse(string pos) => Array.ConvertAll(pos.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), double.Parse);

        public override string ToString() =>  $"{this.Left} {this.Top} {this.Width} {this.Height}";
    }
}
