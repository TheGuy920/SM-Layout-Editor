using System.Windows;
using System.Windows.Controls;

namespace CustomExtensions
{
    // Extension methods must be defined in a static class.
    public static class MarginExtensions
    {
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
        /// This sets the existing margin right to the new margin right
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="bottom"></param>
        public static void SetWidthAndHeight(this Grid grid, double width, double height)
        {
            grid.Width = width;
            grid.Height = height;
        }
    }
}
