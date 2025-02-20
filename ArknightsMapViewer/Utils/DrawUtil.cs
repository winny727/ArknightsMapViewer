using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ArknightsMapViewer
{
    public static class DrawUtil
    {
        public static void DrawLine(Bitmap bitmap, Point startPoint, Point endPoint, Color? color = null, float width = 1)
        {
            using Pen pen = GetPen(color, width);
            using Graphics graphics = GetGraphics(bitmap);
            graphics.DrawLine(pen, startPoint, endPoint);
        }

        public static void DrawRectangle(Bitmap bitmap, Rectangle rectangle, Color? color = null, float width = 1)
        {
            using Pen pen = GetPen(color, width);
            using Graphics graphics = GetGraphics(bitmap);
            graphics.DrawRectangle(pen, rectangle);
        }

        public static void FillRectangle(Bitmap bitmap, Rectangle rectangle, Color? color = null)
        {
            using Brush brush = GetSolidBrush(color);
            using Graphics graphics = GetGraphics(bitmap);
            graphics.FillRectangle(brush, rectangle);
        }

        public static void FillRectangleHatch(Bitmap bitmap, Rectangle rectangle, HatchStyle? hatchStyle = null, Color? color = null)
        {
            using Brush brush = GetHatchBrush(hatchStyle, color);
            using Graphics graphics = GetGraphics(bitmap);
            graphics.FillRectangle(brush, rectangle);
        }

        public static void DrawCircle(Bitmap bitmap, Point origin, int radius, Color? color = null, float width = 1)
        {
            using Pen pen = GetPen(color, width);
            using Graphics graphics = GetGraphics(bitmap);
            Rectangle rectangle = new Rectangle(origin.X - radius, origin.Y - radius, 2 * radius, 2 * radius);
            graphics.DrawEllipse(pen, rectangle);
        }

        public static void FillCircle(Bitmap bitmap, Point origin, int radius, Color? color = null)
        {
            using Brush brush = GetSolidBrush(color);
            using Graphics graphics = GetGraphics(bitmap);
            Rectangle rectangle = new Rectangle(origin.X - radius, origin.Y - radius, 2 * radius, 2 * radius);
            graphics.FillEllipse(brush, rectangle);
        }

        public static void FillCircleHatch(Bitmap bitmap, Point origin, int radius, HatchStyle? hatchStyle = null, Color? color = null)
        {
            using Brush brush = GetHatchBrush(hatchStyle, color);
            using Graphics graphics = GetGraphics(bitmap);
            Rectangle rectangle = new Rectangle(origin.X - radius, origin.Y - radius, 2 * radius, 2 * radius);
            graphics.FillEllipse(brush, rectangle);
        }

        public static void DrawPoint(Bitmap bitmap, Point origin, Color? color = null, int width = 1)
        {
            FillCircle(bitmap, origin, width, color);
        }

        //public static void DrawString(Bitmap bitmap, string text, Rectangle rectangle, Font font, Color? color = null, TextFormatFlags textFormatFlags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter)
        //{
        //    using Graphics graphics = GetGraphics(bitmap);
        //    TextRenderer.DrawText(graphics, text, font, rectangle, color ?? Color.Black, textFormatFlags); //单次绘制耗时较大~3ms
        //}

        public static void DrawString(Bitmap bitmap, string text, Rectangle rectangle, Font font, Color? color = null, StringFormat format = null)
        {
            using Graphics graphics = GetGraphics(bitmap);
            using Brush brush = GetSolidBrush(color);
            format ??= new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            graphics.DrawString(text, font, brush, rectangle, format);
        }

        private static Graphics GetGraphics(Bitmap bitmap)
        {
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            return graphics;
        }

        private static Pen GetPen(Color? color, float width)
        {
            Pen pen = new Pen(new SolidBrush(color ?? Color.Black), width);
            pen.DashStyle = DashStyle.Solid;
            return pen;
        }

        private static SolidBrush GetSolidBrush(Color? color)
        {
            SolidBrush brush = new SolidBrush(color ?? Color.Black);
            return brush;
        }

        private static HatchBrush GetHatchBrush(HatchStyle? hatchStyle, Color? color)
        {
            HatchBrush brush = new HatchBrush(hatchStyle ?? HatchStyle.ForwardDiagonal, color ?? Color.Black, Color.Transparent);
            return brush;
        }
    }
}
