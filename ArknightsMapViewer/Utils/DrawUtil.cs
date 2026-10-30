using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

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

        public static void DrawRouteLine(Bitmap bitmap, Point startPoint, Point endPoint, Color color, float width)
        {
            using Graphics graphics = GetGraphics(bitmap);
            using Pen halo = new Pen(Color.FromArgb(color.A * 7 / 10, 18, 18, 18), width + Math.Max(1.5f, width * 0.65f));
            using Pen line = new Pen(color, width);
            halo.StartCap = halo.EndCap = line.StartCap = line.EndCap = LineCap.Round;
            halo.LineJoin = line.LineJoin = LineJoin.Round;
            graphics.DrawLine(halo, startPoint, endPoint);
            graphics.DrawLine(line, startPoint, endPoint);
        }

        public static void DrawRouteLabel(Bitmap bitmap, string text, RectangleF area, Font font, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            using Graphics graphics = GetGraphics(bitmap);
            using StringFormat format = new StringFormat { FormatFlags = StringFormatFlags.NoWrap };
            SizeF measured = graphics.MeasureString(text, font, PointF.Empty, format);
            float padding = Math.Max(2f, GlobalDefine.TILE_PIXLE * 0.07f);
            float width = Math.Min(measured.Width + 2 * padding, Math.Max(8f, GlobalDefine.TILE_PIXLE * 1.25f));
            float height = Math.Min(measured.Height + padding, Math.Max(6f, GlobalDefine.TILE_PIXLE * 0.45f));
            width = Math.Min(width, bitmap.Width);
            height = Math.Min(height, bitmap.Height);
            if (width <= 2 * padding || height <= 2 * padding / 3) return;
            RectangleF badge = new RectangleF(area.X + (area.Width - width) / 2, area.Y + (area.Height - height) / 2, width, height);
            badge.X = Math.Max(0, Math.Min(badge.X, bitmap.Width - width));
            badge.Y = Math.Max(0, Math.Min(badge.Y, bitmap.Height - height));
            float corner = Math.Min(3f, height / 2);
            using GraphicsPath outline = new GraphicsPath();
            outline.AddArc(badge.Left, badge.Top, corner * 2, corner * 2, 180, 90);
            outline.AddArc(badge.Right - corner * 2, badge.Top, corner * 2, corner * 2, 270, 90);
            outline.AddArc(badge.Right - corner * 2, badge.Bottom - corner * 2, corner * 2, corner * 2, 0, 90);
            outline.AddArc(badge.Left, badge.Bottom - corner * 2, corner * 2, corner * 2, 90, 90);
            outline.CloseFigure();
            using Brush background = new SolidBrush(Color.FromArgb(color.A * 220 / 255, 26, 26, 26));
            graphics.FillPath(background, outline);
            badge.Inflate(-padding, -padding / 3);
            DrawFittedString(graphics, text, badge, font, color);
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
            using Brush brush = new SolidBrush(color ?? Color.Black);
            using Graphics graphics = GetGraphics(bitmap);
            Rectangle rectangle = new Rectangle(origin.X - radius, origin.Y - radius, 2 * radius, 2 * radius);
            graphics.FillEllipse(brush, rectangle);
        }

        public static void DrawPoint(Bitmap bitmap, Point origin, Color? color = null, int width = 1)
        {
            FillCircle(bitmap, origin, width, color);
        }

        internal static void DrawCircleLabel(Bitmap bitmap, Point point, string text, Font font,
            Color textColor, Color backgroundColor, Color edgeColor, float edgeWidth)
        {
            int radius = Math.Max(1, Math.Min(GlobalDefine.CIRCLE_RADIUS, GlobalDefine.TILE_PIXLE / 2 - 3));
            DrawCircle(bitmap, point, radius, Color.FromArgb(220, 18, 18, 18), edgeWidth + 2);
            FillCircle(bitmap, point, radius, backgroundColor);
            DrawCircle(bitmap, point, radius, edgeColor, edgeWidth);
            float inset = Math.Min(3f, radius * 0.2f);
            var rectangle = new RectangleF(point.X - radius + inset, point.Y - radius + inset,
                2 * (radius - inset), 2 * (radius - inset));
            DrawFittedString(bitmap, text, rectangle, font, textColor);
        }

        public static void DrawFittedString(Bitmap bitmap, string text, RectangleF rectangle, Font font, Color color)
        {
            using Graphics graphics = GetGraphics(bitmap);
            DrawFittedString(graphics, text, rectangle, font, color);
        }

        internal static void DrawFittedString(Graphics graphics, string text, RectangleF rectangle, Font font, Color color)
        {
            if (string.IsNullOrEmpty(text) || rectangle.Width <= 0 || rectangle.Height <= 0) return;
            using Brush brush = new SolidBrush(color);
            using StringFormat format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap,
                Trimming = StringTrimming.EllipsisCharacter,
            };
            SizeF measured = graphics.MeasureString(text, font, PointF.Empty, format);
            float scale = Math.Min(1f, Math.Min(rectangle.Width / Math.Max(1f, measured.Width), rectangle.Height / Math.Max(1f, measured.Height)));
            using Font fitted = scale < 0.98f ? new Font(font.FontFamily, Math.Max(Math.Min(font.Size, 6f), font.Size * scale), font.Style, font.Unit) : null;
            GraphicsState state = graphics.Save();
            try
            {
                graphics.SetClip(rectangle, CombineMode.Intersect);
                graphics.DrawString(text, fitted ?? font, brush, rectangle, format);
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        private static Graphics GetGraphics(Bitmap bitmap)
        {
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            return graphics;
        }

        private static Pen GetPen(Color? color, float width) => new Pen(color ?? Color.Black, width);
    }
}
