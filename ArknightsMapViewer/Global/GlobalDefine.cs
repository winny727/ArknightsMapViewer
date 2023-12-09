using System;
using System.Collections.Generic;
using System.Drawing;
using ArknightsMap;

namespace ArknightsMapViewer
{
    public static class GlobalDefine
    {
        public const int TILE_PIXLE = 50;
        public const int LINE_WIDTH = 3;
        public const int CIRCLE_RADIUS = 20;
        public static readonly Font TEXT_FONT = new Font("Consolas", 12f);
        public static readonly Font INDEX_FONT = new Font("Consolas", 8f, FontStyle.Bold);
        public static readonly Font TIME_FONT = new Font("Consolas", 10f);
        public static readonly Color TEXT_COLOR = Color.White;
        public static readonly Color LINE_COLOR = Color.Red;
        public static readonly Color CIRCLE_COLOR = Color.FromArgb(255, 118, 118, 118);

        public readonly static Dictionary<TileKey, Color> TileColor = new Dictionary<TileKey, Color>
        {
            { TileKey.tile_start, Color.FromArgb(255, 231, 15, 50) },
            { TileKey.tile_end, Color.FromArgb(255, 53, 157, 222) },

            { TileKey.tile_forbidden, Color.FromArgb(255, 22, 22, 22) },
            { TileKey.tile_floor, Color.FromArgb(255, 52, 52, 52) },
            { TileKey.tile_road, Color.FromArgb(255, 118, 118, 118) },
            { TileKey.tile_wall, Color.FromArgb(255, 187, 187, 187) },

            { TileKey.tile_telin, Color.FromArgb(255, 22, 22, 22) },
            { TileKey.tile_telout, Color.FromArgb(255, 22, 22, 22) },
            { TileKey.tile_hole, Color.FromArgb(255, 20, 20, 20) },
        };

        public readonly static Dictionary<TileKey, (string, Color)> TileString = new Dictionary<TileKey, (string, Color)>
        {
            { TileKey.tile_telin, ("in", Color.FromArgb(255, 176, 176, 176)) },
            { TileKey.tile_telout, ("out", Color.FromArgb(255, 176, 176, 176)) },
            { TileKey.tile_hole, ("hole", Color.FromArgb(255, 176, 176, 176)) },
        };
    }
}
