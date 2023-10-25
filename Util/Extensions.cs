using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Search;
using LayoutEditor;
using LayoutEditor.CustomXML;
using LayoutEditor.Windows.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace CustomExtensions
{
    // Extension methods must be defined in a static class.
    public static class MarginExtensions
    {

// ==================================== BUTTONS ====================================

        /// <summary>
        /// This adds the existing margin for the button to a new margin
        /// </summary>
        /// <param name="button"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public static void AddMargin(this Button button, double left, double top, double right, double bottom)
        {
            button.Margin = new Thickness(button.Margin.Left + left, button.Margin.Top + top, button.Margin.Right + right, button.Margin.Bottom + bottom);
        }

        /// <summary>
        /// This sets the existing margin right to the new margin right
        /// </summary>
        /// <param name="button"></param>
        /// <param name="right"></param>
        public static void SetMarginR(this Button button, double right)
        {
            button.Margin = new Thickness(button.Margin.Left, button.Margin.Top, right, button.Margin.Bottom);
        }
        /// <summary>
        /// This sets the existing margin top to the new margin top
        /// </summary>
        /// <param name="button"></param>
        /// <param name="bottom"></param>
        public static void SetMarginB(this Button button, double bottom)
        {
            button.Margin = new Thickness(button.Margin.Left, button.Margin.Top, button.Margin.Right, bottom);
        }

// ==================================== CANVAS ===================================

        /// <summary>
        /// This sets the existing margin left and top to the new margin left and top
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        public static void SetMarginLT(this Canvas grid, double left, double top)
        {
            if (grid.Margin.Left != left || grid.Margin.Top != top)
                grid.Margin = new Thickness(left, top, grid.Margin.Right, grid.Margin.Bottom);
        }
        /// <summary>
        /// This sets the existing width and height to the new width and heigh
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void SetWidthAndHeight(this Canvas cc, double width, double height)
        {
            if (cc.Width != width)
                cc.Width = width;
            if (cc.Height != height)
                cc.Height = height;
        }
        /// <summary>
        /// This sets the existing margin left to the new margin left
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="left"></param>
        public static void SetMarginL(this Canvas grid, double left)
        {
            if (grid.Margin.Left != left)
                grid.Margin = new Thickness(left, grid.Margin.Top, grid.Margin.Right, grid.Margin.Bottom);
        }
        /// <summary>
        /// This sets the existing margin top to the new margin top
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="top"></param>
        public static void SetMarginT(this Canvas grid, double top)
        {
            if (grid.Margin.Top != top)
                grid.Margin = new Thickness(grid.Margin.Left, top, grid.Margin.Right, grid.Margin.Bottom);
        }

// ==================================== GRIDS ====================================

        /// <summary>
        /// This adds the existing margin for the grid to a new margin
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public static void AddMargin(this Grid grid, double left, double top, double right, double bottom)
        {
            grid.Margin = new Thickness(grid.Margin.Left + left, grid.Margin.Top + top, grid.Margin.Right + right, grid.Margin.Bottom + bottom);
        }
        /// <summary>
        /// This sets the existing margin left and top to the new margin left and top
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        public static void SetMarginLT(this Grid grid, double left, double top)
        {
            grid.Margin = new Thickness(left, top, grid.Margin.Right, grid.Margin.Bottom);
        }
        /// <summary>
        /// This sets the existing margin left and top to the new margin left and top
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        public static bool SetMarginLT(this Grid grid, Thickness? b)
        {
            if (!b.HasValue)
                return false;

            if (grid.Margin.Top != b.Value.Top || grid.Margin.Left != b.Value.Left)
            {
                grid.Margin = new Thickness(b.Value.Left, b.Value.Top, grid.Margin.Right, grid.Margin.Bottom);
                return true;
            }
            return false;
        }
        /// <summary>
        /// This sets the existing margin bottom and right to the new margin bottom and right
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="bottom"></param>
        /// <param name="right"></param>
        public static void SetMarginBR(this Grid grid, double bottom, double right)
        {
            grid.Margin = new Thickness(grid.Margin.Left, grid.Margin.Top, right, bottom);
        }
        /// <summary>
        /// This sets the existing margin left to the new margin left
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="left"></param>
        public static void SetMarginL(this Grid grid, double left)
        {
            grid.Margin = new Thickness(left, grid.Margin.Top, grid.Margin.Right, grid.Margin.Bottom);
        }
        /// <summary>
        /// This sets the existing margin top to the new margin top
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="top"></param>
        public static void SetMarginT(this Grid grid, double top)
        {
            grid.Margin = new Thickness(grid.Margin.Left, top, grid.Margin.Right, grid.Margin.Bottom);
        }
        /// <summary>
        /// This sets the existing margin right to the new margin right
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="right"></param>
        public static void SetMarginR(this Grid grid, double right)
        {
            grid.Margin = new Thickness(grid.Margin.Left, grid.Margin.Top, right, grid.Margin.Bottom);
        }
        /// <summary>
        /// This sets the existing margin right to the new margin right
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="bottom"></param>
        public static void SetMarginB(this Grid grid, double bottom)
        {
            grid.Margin = new Thickness(grid.Margin.Left, grid.Margin.Top, grid.Margin.Right, bottom);
        }
        /// <summary>
        /// This sets the existing width and height to the new width and heigh
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void SetWidthAndHeight(this Grid grid, double width, double height)
        {
            grid.Width = width;
            grid.Height = height;
        }
        /// <summary>
        /// This sets the existing width and height to the new width and heigh
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static bool SetWidthAndHeight(this Grid grid, Thickness b)
        {
            bool changed = false;
            if (grid.Width != b.Right)
            {
                grid.Width = b.Right;
                changed = true;
            }
            if (grid.Height != b.Bottom)
            {
                grid.Height = b.Bottom;
                changed = true;
            }
            return changed;
        }
        /// <summary>
        /// This sets the existing width and height to the new width and height
        /// </summary>
        public static string GetLocationAsString(this Grid grid)
        {
            Grid parent = grid.Parent as Grid;
            double _height = grid.ActualHeight / parent.ActualHeight;
            double _width = grid.ActualWidth / parent.ActualWidth;
            double _left = grid.Margin.Left / parent.ActualWidth;
            double _top = grid.Margin.Top / parent.ActualHeight;
            return $"{_left} {_top} {_height} {_width}";
        }

// ==================================== THICKNESS ======================================= 

        /// <summary>
        /// This sets the existing width and height to the new width and heigh
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void Add(this Thickness a, Thickness b)
        {
            a.Left += b.Left;
            a.Top += b.Top;
            a.Right += b.Right;
            a.Bottom += b.Bottom;
        }
        /// <summary>
        /// This sets the existing width and height to the new width and heigh
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static Thickness AddMargin(this Thickness a, Thickness b)
        {
            a.Left += b.Left;
            a.Top += b.Top;
            a.Right += b.Right;
            a.Bottom += b.Bottom;
            return a;
        }

        /// <summary>
        /// This adds the width and height * factor to the existing
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static Thickness AddMarginF(this Thickness a, Thickness b, double factor)
        {
            a.Left += b.Left * factor;
            a.Top += b.Top * factor;
            a.Right += b.Right * factor;
            a.Bottom += b.Bottom * factor;
            return a;
        }

        /// <summary>
        /// This truncates the LTRB to the nearest decimal place
        /// </summary>
        /// <param name="decimal_places">the number of decimal places to truncate to</param>
        public static Thickness Truncate(this Thickness a, int decimal_places)
        {
            double factor = Math.Pow(10, decimal_places);
            a.Left = Math.Floor(a.Left * factor) / 100;
            a.Top = Math.Floor(a.Top * factor) / 100;
            a.Right = Math.Floor(a.Right * factor) / 100;
            a.Bottom = Math.Floor(a.Bottom * factor) / 100;
            return a;
        }

        /// <summary>
        /// takes the smaller of the left and top margin values indivisually
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void MinMargin(this ref Thickness a, Thickness b)
        {
            if (a.Right == -1)
                a.Right = b.Right;

            if (a.Bottom == -1)
                a.Bottom = b.Bottom;

            if (a.Left == -1)
                a.Left = b.Left;

            if (a.Top == -1)
                a.Top = b.Top;

            if (b.Left < a.Left)
            {
                a.Left = b.Left;
                a.Right = b.Right;
            }

            if (b.Top < a.Top)
            {
                a.Top = b.Top;
                a.Bottom = b.Bottom;
            }
        }
        /// <summary>
        /// takes the bigger of the left and top margin values indivisually
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void MaxMargin(this ref Thickness a, Thickness b)
        {
            if (a.Right == -1)
                a.Right = b.Right;

            if (a.Bottom == -1)
                a.Bottom = b.Bottom;

            if (a.Left == -1)
                a.Left = b.Left;

            if (a.Top == -1)
                a.Top = b.Top;

            if (b.Left + b.Right > a.Left + a.Right)
            {
                a.Left = b.Left;
                a.Right = b.Right;
            }

            if (b.Top + b.Bottom > a.Top + a.Bottom)
            {
                a.Top = b.Top;
                a.Bottom = b.Bottom;
            }
        }

// ==================================== SCROLLVIEWER ==================================== 

        /// <summary>
        /// This sets the existing width and height to the new width and heigh
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void SetWidthAndHeight(this ScrollViewer scrollViewer, double width, double height)
        {
            scrollViewer.Width = width;
            scrollViewer.Height = height;
        }

// ====================================== FLOAT ========================================= 

        /// <summary>
        /// Moves decimal so there is no more decimal
        /// </summary>
        /// <param name="f">float</param>
        public static float MoveDecimal(this float f)
        {
            while (f != Math.Floor(f))
                f *= 10;
            return f;
        }

// ==================================== STRING ====================================

        public static string CapitilzeFirst(this string s)
        {
            return System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(s);
        }
        public static string ToValidPath(this string s)
        {
            return System.IO.Path.GetFullPath(CapitilzeFirst(s).Replace("/", "\\"));
        }
        public static string DecodeHtmlEntities(this string m)
        {
            m = m.Replace("&", "&amp;");
            m = m.Replace("\"", "&quot;");
            m = m.Replace("\'", "&apos;");
            m = m.Replace("<", "&lt;");
            m = m.Replace(">", "&gt;");
            return m;
        }

// ================================= CONTENT CONTROL================================

        /// <summary>
        /// This sets the existing width and height to the new width and heigh
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void SetWidthAndHeight(this ContentControl cc, double width, double height)
        {
            cc.Width = width;
            cc.Height = height;
        }

        /// <summary>
        /// This sets the margin to a new margin
        /// </summary>
        /// <param name="button"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public static void SetMargin(this ContentControl cc, double left, double top, double right, double bottom)
        {
            cc.Margin = new Thickness(left, top, right, bottom);
        }
        /// <summary>
        /// This sets the margin to a new margin
        /// </summary>
        /// <param name="button"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public static void SetMargin(this ContentControl cc, Thickness m)
        {
            cc.Margin = m;
        }

// ================================= XML DOCUMENT ==================================
        public static string PrettyXml(this XElement xml/*this XDocument doc*/, bool strip_id = true)
        {
            var stringBuilder = new StringBuilder();
            //XElement xml = XElement.Parse(doc.OuterXml);
            if (xml != null)
            {
                XmlWriterSettings settings = new()
                {
                    OmitXmlDeclaration = false,
                    Indent = true,
                    NewLineOnAttributes = false,
                    IndentChars = "    "
                };

                using XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, settings);
                xml.Save(xmlWriter);
            }
            return stringBuilder.ToString();
        }

// ================================ XmlOverlay =====================================

        /// <summary>
        /// This sets the existing margin left to the new margin left
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="left"></param>
        public static void AddMarginL(this XmlOverlay grid, double left)
        {
            grid.Margin = new Thickness(grid.Margin.Left + left, grid.Margin.Top, grid.Margin.Right, grid.Margin.Bottom);
        }
        /// <summary>
        /// This sets the existing margin top to the new margin top
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="top"></param>
        public static void AddMarginT(this XmlOverlay grid, double top)
        {
            grid.Margin = new Thickness(grid.Margin.Left, grid.Margin.Top + top, grid.Margin.Right, grid.Margin.Bottom);
        }

        /// <summary>
        /// This sets the existing margin left and top to the new margin left and top
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        public static bool AddMarginLT(this XmlOverlay grid, double left, double top)
        {
            if (left != 0 || top != 0)
            {
                grid.Margin = new Thickness(grid.Margin.Left + left, grid.Margin.Top + top, grid.Margin.Right, grid.Margin.Bottom);
                return true;
            }
            return false;
        }


        /// <summary>
        /// This sets the existing margin left and top to the new margin left and top
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        public static bool AddMarginLT(this XmlOverlay grid, ActualSize size)
        {
            if (size.Left != 0 || size.Top != 0)
            {
                grid.Margin = new Thickness(grid.Margin.Left + size.Left, grid.Margin.Top + size.Top, grid.Margin.Right, grid.Margin.Bottom);
                return true;
            }
            return false;
        }

        /// <summary>
        /// This sets the existing margin left and top to the new margin left and top
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        public static bool SetSize(this XmlOverlay grid, ActualSize size)
        {
            var o1 = grid.AddMarginLT(size);
            if (grid.Width != size.Width)
            {
                grid.Width = size.Width;
                o1 = true;
            }
            if (grid.Height != size.Height)
            {
                grid.Height = size.Height;
                o1 = true;
            }
            return o1;
        }

// ================================ MOUSE UTIL =====================================
        public static Vector GetMouseMovement(this MainEditor env, bool resetPosition = false, bool MoveSensitivity = false, bool Scale = false, bool GridSize = false)
        {
            Vector returnVec = MouseUtil.GetMousePosition() - env.MouseStart;
            if (MoveSensitivity)
                returnVec *= MainWindow.Get.MoveSensitivity;
            if (Scale)
                returnVec /= env.Scale;
            if (GridSize)
                returnVec /= MainWindow.Get.GridSize;
            if (resetPosition)
                env.MouseStart = MouseUtil.GetMousePosition();
            return returnVec;
        }
        public static MouseButtonState GetButtonState(this MouseDevice Mouse, MouseButton Button)
        {
            return Button switch
            {
                MouseButton.Left => Mouse.LeftButton,
                MouseButton.Right => Mouse.RightButton,
                MouseButton.Middle => Mouse.MiddleButton,
                MouseButton.XButton1 => Mouse.XButton1,
                MouseButton.XButton2 => Mouse.XButton2,
                _ => MouseButtonState.Released,
            };
        }
// ================================== POINT =======================================
        public static Point Subtract(this Point first, Point second)
        {
            return new(first.X - second.X, first.Y - second.Y);
        }
        public static Point Divide(this Point first, double divisor)
        {
            return new(first.X / divisor, first.Y / divisor);
        }
// ================================= ActualSize ===================================
        public static ActualSize GetActualSize(this Grid g, Point LeftTop = default)
        {
            return new(g.ActualWidth, g.ActualHeight, LeftTop.X, LeftTop.Y);
        }
    }
}
