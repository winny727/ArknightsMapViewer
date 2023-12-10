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

        public int Width { get; private set; }
        public int Height { get; private set; }

        public WinformMapDrawer(PictureBox pictureBox, Tile[,] map)
        {
            PictureBox = pictureBox;
            Map = map;

            Width = map.GetLength(0);
            Height = map.GetLength(1);
        }

        public void InitCanvas()
        {
            Bitmap bitmap = new Bitmap(Width * GlobalDefine.TILE_PIXLE + Width, Height * GlobalDefine.TILE_PIXLE + Height);
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
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    DrawTile(row, col);
                }
            }
            RefreshCanvas();
        }

        private void DrawTile(int rowIndex, int colIndex)
        {
            Tile tile = Map[colIndex, rowIndex];
            if (!GlobalDefine.TileColor.TryGetValue(tile.tileKey, out Color color))
            {
                //Console.WriteLine("Tile Color Undefine: " + tile.tileKey);
                MainForm.Instance.Log("Tile Color Undefine: " + tile.tileKey, MainForm.LogType.Warning);
                color = Color.White;
            }
            GlobalDefine.TileString.TryGetValue(tile.tileKey, out (string, Color) tileString);

            Bitmap bitmap = (Bitmap)PictureBox.BackgroundImage;

            int length = GlobalDefine.TILE_PIXLE;
            Rectangle rectangle = new Rectangle(colIndex * length, rowIndex * length, length, length);

            DrawUtil.FillRectangle(bitmap, rectangle, color);
            DrawUtil.DrawRectangle(bitmap, rectangle);

            //Draw TileText
            if (!string.IsNullOrEmpty(tileString.Item1))
            {
                DrawUtil.DrawString(bitmap, tileString.Item1, rectangle, GlobalDefine.TEXT_FONT, tileString.Item2);
            }

            //Draw Index
            string indexText = GetIndexText(colIndex, rowIndex);
            DrawUtil.DrawString(bitmap, indexText, rectangle, GlobalDefine.INDEX_FONT, GlobalDefine.TEXT_COLOR, TextFormatFlags.Right | TextFormatFlags.Bottom);
        }

        private string GetIndexText(int colIndex, int rowIndex)
        {
            return $"{colIndex},{Height - rowIndex - 1}";
            //return $"{(char)('A' + (Height - rowIndex - 1))}{colIndex + 1}";
        }
    }
}
