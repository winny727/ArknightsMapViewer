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

        //TileDefine.txt
        public readonly static Dictionary<string, Color> TileColor = new Dictionary<string, Color>();
        public readonly static Dictionary<string, (string, Color)> TileString = new Dictionary<string, (string, Color)>();
    }
}
