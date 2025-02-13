using System;
using System.Collections.Generic;
using ArknightsMap;
using System.Windows.Forms;
using System.Drawing;

namespace ArknightsMapViewer
{
    public class WinformMapDrawer : IMapDrawer
    {
        public PictureBox PictureBox { get; private set; }
        public Tile[,] Map { get; private set; }

        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }

        public WinformMapDrawer(PictureBox pictureBox, Tile[,] map)
        {
            PictureBox = pictureBox;
            Map = map;

            MapWidth = map.GetLength(0);
            MapHeight = map.GetLength(1);
        }

        public void InitCanvas()
        {
            Bitmap bitmap = new Bitmap(MapWidth * GlobalDefine.TILE_PIXLE, MapHeight * GlobalDefine.TILE_PIXLE);
            PictureBox.BackgroundImage?.Dispose();
            PictureBox.BackgroundImage = bitmap;
            PictureBox.Width = bitmap.Width;
            PictureBox.Height = bitmap.Height;
        }

        public void RefreshCanvas()
        {
            PictureBox.Refresh();
        }

        public void DrawMap()
        {
            InitCanvas();
            for (int row = 0; row < MapHeight; row++)
            {
                for (int col = 0; col < MapWidth; col++)
                {
                    DrawTile(row, col);
                }
            }
            RefreshCanvas();
        }

        private void DrawTile(int rowIndex, int colIndex)
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

                MainForm.Instance.Log("Undefined Tile: " + tile.tileKey, MainForm.LogType.Warning);
            }

            Bitmap bitmap = (Bitmap)PictureBox.BackgroundImage;

            int length = GlobalDefine.TILE_PIXLE;
            Rectangle rectangle = new Rectangle(colIndex * length, (MapHeight - rowIndex - 1) * length, length, length);

            DrawUtil.FillRectangle(bitmap, rectangle, tileColor);
            DrawUtil.DrawRectangle(bitmap, rectangle);

            //Draw TileText
            if (tileInfo != null && !string.IsNullOrEmpty(tileInfo.tileText))
            {
                Color textColor = tileInfo.textColor ?? Color.Black;
                DrawUtil.DrawString(bitmap, tileInfo.tileText, rectangle, GlobalDefine.TEXT_FONT, textColor);
            }

            //Draw Index
            string indexText = GetIndexText(colIndex, rowIndex);
            DrawUtil.DrawString(bitmap, indexText, rectangle, GlobalDefine.INDEX_FONT, GlobalDefine.TEXT_COLOR, TextFormatFlags.Right | TextFormatFlags.Bottom);
        }

        private string GetIndexText(int colIndex, int rowIndex)
        {
            return $"{colIndex},{rowIndex}";
            //return $"{(char)('A' + rowIndex)}{colIndex + 1}";
        }
    }
}
