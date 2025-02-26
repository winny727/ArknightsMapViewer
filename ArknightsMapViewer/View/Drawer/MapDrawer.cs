using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace ArknightsMapViewer
{
    public class MapDrawer : IDrawer
    {
        public Tile[,] Map { get; private set; }
        public bool HaveUndefinedTiles { get; private set; }
        public bool MarkUndefinedTile { get; set; }
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }

        private static readonly StringFormat stringFormat = new StringFormat
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Far,
        };

        private MapDrawer(Tile[,] map)
        {
            Map = map;

            HaveUndefinedTiles = false;
            MapWidth = map.GetLength(0);
            MapHeight = map.GetLength(1);
        }

        public static MapDrawer Create(Tile[,] map)
        {
            if (map == null)
            {
                MainForm.Instance.Log("Create MapDrawer Failed, Invalid Map", MainForm.LogType.Warning);
                return null;
            }

            return new MapDrawer(map);
        }

        public void Draw(Bitmap bitmap)
        {
            for (int row = 0; row < MapHeight; row++)
            {
                for (int col = 0; col < MapWidth; col++)
                {
                    DrawTile(bitmap, row, col);
                }
            }
        }

        private void DrawTile(Bitmap bitmap, int rowIndex, int colIndex)
        {
            Tile tile = Map[colIndex, rowIndex];
            TileInfo tileInfo = tile.GetTileInfo();

            Color tileColor = Color.White;
            if (tileInfo != null && tileInfo.tileColor != null)
            {
                tileColor = tileInfo.tileColor.Value;
            }
            else
            {
                if (tile.heightType == HeightType.LOWLAND)
                {
                    if (tile.buildableType != BuildableType.NONE && tile.buildableType != BuildableType.E_NUM)
                    {
                        tileColor = GlobalDefine.DEFAULT_ROAD_COLOR;
                    }
                    else
                    {
                        tileColor = GlobalDefine.DEFAULT_FLOOR_COLOR;
                    }
                }
                else if (tile.heightType == HeightType.HIGHLAND)
                {
                    if (tile.buildableType != BuildableType.NONE && tile.buildableType != BuildableType.E_NUM)
                    {
                        tileColor = GlobalDefine.DEFAULT_WALL_COLOR;
                    }
                    else
                    {
                        tileColor = GlobalDefine.DEFAULT_FORBIDDEN_COLOR;
                    }
                }

                HaveUndefinedTiles = true;
                MainForm.Instance.Log("Undefined Tile: " + tile.tileKey, MainForm.LogType.Warning);
            }

            int length = GlobalDefine.TILE_PIXLE;
            Rectangle rectangle = new Rectangle(colIndex * length, (MapHeight - rowIndex - 1) * length, length, length);

            DrawUtil.FillRectangle(bitmap, rectangle, tileColor);
            DrawUtil.DrawRectangle(bitmap, rectangle);

            //DrawHatch
            if (MarkUndefinedTile && tileInfo != null && tileInfo.tileColor == null && string.IsNullOrEmpty(tileInfo.tileText))
            {
                DrawUtil.FillRectangleHatch(bitmap, rectangle);
            }

            //Draw TileText
            if (tileInfo != null && !string.IsNullOrEmpty(tileInfo.tileText))
            {
                Color textColor = tileInfo.textColor ?? Color.Black;
                RectangleF textRectangle = new RectangleF((colIndex - 0.5f) * length, (MapHeight - rowIndex - 1) * length, length * 2, length);
                DrawUtil.DrawString(bitmap, tileInfo.tileText, textRectangle, GlobalDefine.TEXT_FONT, textColor);
            }

            //Draw Index
            string indexText = GetIndexText(colIndex, rowIndex);
            DrawUtil.DrawString(bitmap, indexText, rectangle, GlobalDefine.INDEX_FONT, GlobalDefine.TEXT_COLOR, stringFormat);
        }

        private string GetIndexText(int colIndex, int rowIndex)
        {
            return $"{colIndex},{rowIndex}";
            //return $"{(char)('A' + rowIndex)}{colIndex + 1}";
        }
    }
}
