using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LayoutEditor.CustomXML
{
    public struct ActualSize
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public ActualSize()
        {
            Width = 0;
            Height = 0;
            Left = 0;
            Top = 0;
        }

        public ActualSize(double w, double h)
        {
            Width = w;
            Height = h;
            Left = 0;
            Top = 0;
        }

        public ActualSize(double w, double h, double l, double t)
        {
            Width = w;
            Height = h; 
            Left = l;
            Top = t;
        }

        public static ActualSize operator +(ActualSize Source, ActualSize Target)
        {
            return new(Source.Width + Target.Width, Source.Height + Target.Height, Source.Left + Target.Left, Source.Top + Target.Top);
        }

        public static ActualSize operator -(ActualSize Source, ActualSize Target)
        {
            return new(Source.Width - Target.Width, Source.Height - Target.Height, Source.Left - Target.Left, Source.Top - Target.Top);
        }

        public static ActualSize operator /(ActualSize Source, ActualSize Target)
        {
            return new(Source.Width / Target.Width, Source.Height / Target.Height, Source.Left / Target.Left, Source.Top / Target.Top);
        }

        public ActualSize Divide(Point divisor)
        {
            return new(this.Width / divisor.X, this.Height / divisor.Y, this.Left / divisor.X, this.Top / divisor.Y);
        }

        public ActualSize Min(double min)
        {
            return new ActualSize(Math.Max(this.Width, min), Math.Max(this.Height, min), Math.Max(this.Left, min), Math.Max(this.Top, min));
        }

        public override string ToString()
        {
            return $"{Width}, {Height}, {Left}, {Top}";
        }
    }
}
