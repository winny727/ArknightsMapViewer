using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Text.RegularExpressions;

namespace ArknightsMapViewer
{
    public class PredefineDrawer : IDrawer
    {
        public Predefine.PredefineInst Predefine { get; private set; }
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }

        public PredefineDrawer(Predefine.PredefineInst predefine, int mapWidth, int mapHeight)
        {
            Predefine = predefine;
            MapWidth = mapWidth;
            MapHeight = mapHeight;
        }

        public void Draw(Bitmap bitmap)
        {
            if (Predefine == null)
            {
                return;
            }

            Point point = GetPoint(Predefine.position);

            string predefineName = Predefine.inst.characterKey;

            if (GlobalDefine.CharacterTable.TryGetValue(predefineName, out CharacterData characterData) && 
                !string.IsNullOrEmpty(characterData.appellation) && characterData.profession == "TRAP")
            {
                predefineName = characterData.appellation;
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

            DrawPosition(bitmap, point, predefineName);
        }

        private void DrawPosition(Bitmap bitmap, Point point, string name)
        {
            DrawUtil.FillCircle(bitmap, point, GlobalDefine.CIRCLE_RADIUS, GlobalDefine.PREDEFINED_BACKCOLOR);
            DrawUtil.DrawCircle(bitmap, point, GlobalDefine.CIRCLE_RADIUS, GlobalDefine.PREDEFINED_FORECOLOR, GlobalDefine.CIRCLE_EDGE_WIDTH);

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
