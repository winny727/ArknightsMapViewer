using System;
using System.Collections.Generic;
using System.Drawing;
using ArknightsMap;

namespace ArknightsMapViewer
{
    [Serializable]
    public class DrawConfig
    {
        [Serializable]
        public class SizeConfig
        {
            public int tilePixle = 50;
            public int lineWidth = 3;
            public int circleRadius = 20;
        }

        [Serializable]
        public class FontConfig
        {
            public string textFont = "Consolas";
            public float textFontSize = 12.0f;
            public string textFontStyle = "Regular";
            public string indexFont = "Consolas";
            public float indexFontSize = 8.0f;
            public string indexFontStyle = "Bold";
            public string timeFont = "Consolas";
            public float timeFontSize = 10.0f;
            public string timeFontStyle = "Regular";
        }

        [Serializable]
        public class ColorConfig
        {
            public string textColor = "#ffffff";
            public string lineColor = "#ff0000";
            public string circleColor = "#767676";
        }

        public SizeConfig Size = new SizeConfig();
        public FontConfig Font = new FontConfig();
        public ColorConfig Color = new ColorConfig();
    }

    public static class GlobalDefine
    {
        public static int TILE_PIXLE = 50;
        public static int LINE_WIDTH = 3;
        public static int CIRCLE_RADIUS = 20;

        public static Font TEXT_FONT = new Font("Consolas", 12f);
        public static Font INDEX_FONT = new Font("Consolas", 8f, FontStyle.Bold);
        public static Font TIME_FONT = new Font("Consolas", 10f);

        public static Color TEXT_COLOR = Color.White;
        public static Color LINE_COLOR = Color.Red;
        public static Color CIRCLE_COLOR = Color.FromArgb(255, 118, 118, 118);

        //TileDefine.txt
        public readonly static Dictionary<string, Color> TileColor = new Dictionary<string, Color>();
        public readonly static Dictionary<string, (string, Color)> TileString = new Dictionary<string, (string, Color)>();
    }
}
