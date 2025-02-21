using System;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json.Linq;

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
            public int circleEdgeWidth = 3;
            public int circleRadius = 20;
            public int pointRadius = 6;
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
            public string lenghtFont = "Consolas";
            public float lenghtFontSize = 8.0f;
            public string lenghtFontStyle = "Regular";
        }

        [Serializable]
        public class ColorConfig
        {
            public string textColor = "#ffffff";
            public string lineColor = "#ff0000";
            public string circleColor = "#767676";
            public string lengthColor = "#00ff00";

            public string defaultRoadColor = "#767676";
            public string defaultWallColor = "#bbbbbb";
            public string defaultFloorColor = "#343434";
            public string defaultForbiddenColor = "#161616";

            public string predefinedForeColor = "#ff4500";
            public string predefinedBackColor = "#808080";
        }

        public SizeConfig Size = new SizeConfig();
        public FontConfig Font = new FontConfig();
        public ColorConfig Color = new ColorConfig();
    }

    [Serializable]
    public class TileInfo
    {
        public string tileKey;
        public string name;
        public string description;
        public Color? tileColor;
        public string tileText;
        public Color? textColor;
        public string comment;
    }


    public static class GlobalDefine
    {
        public static int TILE_PIXLE = 50;
        public static int LINE_WIDTH = 3;
        public static int CIRCLE_EDGE_WIDTH = 3;
        public static int CIRCLE_RADIUS = 20;
        public static int POINT_RADIUS = 6;

        public static Font TEXT_FONT = new Font("Consolas", 12.0f);
        public static Font INDEX_FONT = new Font("Consolas", 8.0f, FontStyle.Bold);
        public static Font TIME_FONT = new Font("Consolas", 10.0f);
        public static Font LENGTH_FONT = new Font("Consolas", 8.0f);

        public static Color TEXT_COLOR = Color.White;
        public static Color LINE_COLOR = Color.Red;
        public static Color CIRCLE_COLOR = Color.FromArgb(255, 118, 118, 118);
        public static Color LENGTH_COLOR = Color.Green;

        //默认地块颜色
        public static Color DEFAULT_ROAD_COLOR = ColorTranslator.FromHtml("#767676");
        public static Color DEFAULT_WALL_COLOR = ColorTranslator.FromHtml("#bbbbbb");
        public static Color DEFAULT_FLOOR_COLOR = ColorTranslator.FromHtml("#343434");
        public static Color DEFAULT_FORBIDDEN_COLOR = ColorTranslator.FromHtml("#161616");

        //装置颜色
        public static Color PREDEFINED_FORECOLOR = Color.OrangeRed;
        public static Color PREDEFINED_BACKCOLOR = Color.Gray;

        //tileInfo
        public readonly static Dictionary<string, TileInfo> TileInfo = new Dictionary<string, TileInfo>();

        //Database
        public readonly static Dictionary<string, Dictionary<int, DbData>> EnemyDBData = new Dictionary<string, Dictionary<int, DbData>>();
        public readonly static Dictionary<string, TrapData> TrapDBData = new Dictionary<string, TrapData>();
    }
}
