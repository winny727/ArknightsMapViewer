using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Text.RegularExpressions;

namespace ArknightsMapViewer
{
    public class PredefineDrawer : IDrawer
    {
        public PictureBox PictureBox { get; private set; }
        public Predefine.PredefineInst Predefine { get; private set; }
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }

        public PredefineDrawer(PictureBox pictureBox, Predefine.PredefineInst predefine, int mapWidth, int mapHeight)
        {
            PictureBox = pictureBox;
            Predefine = predefine;
            MapWidth = mapWidth;
            MapHeight = mapHeight;
        }


        public void InitCanvas()
        {
            Bitmap bitmap = new Bitmap(PictureBox.BackgroundImage.Width, PictureBox.BackgroundImage.Height);
            PictureBox.Image?.Dispose();
            PictureBox.Image = bitmap;
        }

        public void RefreshCanvas()
        {
            PictureBox.Refresh();
        }

        public void DrawPredefine()
        {
            Point point = GetPoint(Predefine.position);

            string predefineName = Predefine.inst.characterKey;

            if (GlobalDefine.TrapDBData.TryGetValue(predefineName, out TrapData trapDBData) && !string.IsNullOrEmpty(trapDBData.appellation))
            {
                predefineName = trapDBData.appellation;
                if (predefineName.Contains(" "))
                {
                    predefineName = Helper.GetAbbreviation(predefineName);
                }

                if (predefineName.Length > 4)
                {
                    predefineName = predefineName.Substring(0, 4);
                }
            }
            else
            {
                var match = Regex.Match(Predefine.inst.characterKey, ".+?_.+?_(.+)");
                if (match.Success)
                {
                    predefineName = match.Groups[1].ToString();
                }
            }

            DrawPosition(point, predefineName);
        }

        private void DrawPosition(Point point, string name)
        {
            Bitmap bitmap = (Bitmap)PictureBox.Image;
            DrawUtil.FillCircle(bitmap, point, GlobalDefine.CIRCLE_RADIUS, Color.Gray);
            DrawUtil.DrawCircle(bitmap, point, GlobalDefine.CIRCLE_RADIUS, Color.OrangeRed, GlobalDefine.CIRCLE_EDGE_WIDTH);

            int length = GlobalDefine.TILE_PIXLE;
            Rectangle rectangle = new Rectangle(point.X - length, point.Y - length, 2 * length, 2 * length);
            DrawUtil.DrawString(bitmap, name, rectangle, GlobalDefine.TIME_FONT, GlobalDefine.TEXT_COLOR);
        }

        private Point GetPoint(Position position)
        {
            return Helper.PositionToPoint(position, new Offset() { x = 0, y = 0 }, MapHeight);
        }
    }
}
