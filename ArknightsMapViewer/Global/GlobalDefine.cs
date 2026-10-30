using System;
using System.Collections.Generic;
using System.Drawing;

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
            public int circleEdgeWidth = 2;
            public int circleRadius = 18;
            public int pointRadius = 5;
        }

        [Serializable]
        public class FontConfig
        {
            public string textFont = "Segoe UI";
            public float textFontSize = 10.0f;
            public string textFontStyle = "Regular";
            public string indexFont = "Consolas";
            public float indexFontSize = 7.0f;
            public string indexFontStyle = "Regular";
            public string timeFont = "Segoe UI";
            public float timeFontSize = 9.0f;
            public string timeFontStyle = "Regular";
            public string predefinedFont = "Segoe UI";
            public float predefinedFontSize = 9.0f;
            public string predefinedFontStyle = "Regular";
            public string lenghtFont = "Consolas";
            public float lenghtFontSize = 8.0f;
            public string lenghtFontStyle = "Regular";
        }

        [Serializable]
        public class ColorConfig
        {
            public string textColor = "#EFEFEF";
            public string lineColor = "#FF0000";
            public string circleColor = "#383838";
            public string lengthColor = "#FFE1A3";

            public string defaultRoadColor = "#767676";
            public string defaultWallColor = "#bbbbbb";
            public string defaultFloorColor = "#343434";
            public string defaultForbiddenColor = "#161616";

            public string predefinedForeColor = "#B8B8B8";
            public string predefinedBackColor = "#3B3B3B";
            public string predefinedSelectedBackColor = "#DADADA";
            public string predefinedSelectedTextColor = "#202020";
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
        public static int CIRCLE_EDGE_WIDTH = 2;
        public static int CIRCLE_RADIUS = 18;
        public static int POINT_RADIUS = 5;

        public static Font TEXT_FONT = new Font("Segoe UI", 10.0f, FontStyle.Regular);
        public static Font INDEX_FONT = new Font("Consolas", 7.0f, FontStyle.Regular);
        public static Font TIME_FONT = new Font("Segoe UI", 9.0f, FontStyle.Regular);
        public static Font PREDEFINED_FONT = new Font("Segoe UI", 9.0f, FontStyle.Regular);
        public static Font LENGTH_FONT = new Font("Consolas", 8.0f);

        public static Color TEXT_COLOR;
        public static Color LINE_COLOR;
        public static Color CIRCLE_COLOR;
        public static Color LENGTH_COLOR;

        //默认地块颜色
        public static Color DEFAULT_ROAD_COLOR;
        public static Color DEFAULT_WALL_COLOR;
        public static Color DEFAULT_FLOOR_COLOR;
        public static Color DEFAULT_FORBIDDEN_COLOR;

        //装置颜色
        public static Color PREDEFINED_LINECOLOR;
        public static Color PREDEFINED_BACKCOLOR;
        public static Color PREDEFINED_SELECTED_BACKCOLOR;
        public static Color PREDEFINED_SELECTED_TEXTCOLOR;

        static GlobalDefine()
        {
            // 初始颜色与配置缺失/非法时的回退共用同一套内置默认值。
            // 加载文件后，DrawConfigValidator.Apply 再设置校验过的运行时颜色。
            var colors = new DrawConfig.ColorConfig();
            TEXT_COLOR = ColorTranslator.FromHtml(colors.textColor);
            LINE_COLOR = ColorTranslator.FromHtml(colors.lineColor);
            CIRCLE_COLOR = ColorTranslator.FromHtml(colors.circleColor);
            LENGTH_COLOR = ColorTranslator.FromHtml(colors.lengthColor);
            DEFAULT_ROAD_COLOR = ColorTranslator.FromHtml(colors.defaultRoadColor);
            DEFAULT_WALL_COLOR = ColorTranslator.FromHtml(colors.defaultWallColor);
            DEFAULT_FLOOR_COLOR = ColorTranslator.FromHtml(colors.defaultFloorColor);
            DEFAULT_FORBIDDEN_COLOR = ColorTranslator.FromHtml(colors.defaultForbiddenColor);
            PREDEFINED_LINECOLOR = ColorTranslator.FromHtml(colors.predefinedForeColor);
            PREDEFINED_BACKCOLOR = ColorTranslator.FromHtml(colors.predefinedBackColor);
            PREDEFINED_SELECTED_BACKCOLOR = ColorTranslator.FromHtml(colors.predefinedSelectedBackColor);
            PREDEFINED_SELECTED_TEXTCOLOR = ColorTranslator.FromHtml(colors.predefinedSelectedTextColor);
        }

        //TileInfo <tileKey, tileInfo>
        public readonly static Dictionary<string, TileInfo> TileInfo = new Dictionary<string, TileInfo>();

        //StageInfo <category, <zoneId, <stageId, stageInfo>>>
        public readonly static Dictionary<string, List<StageInfo>> StageInfo = new Dictionary<string, List<StageInfo>>();

        //Database
        public readonly static Dictionary<string, Dictionary<int, DbData>> EnemyDatabase = new Dictionary<string, Dictionary<int, DbData>>();
        public readonly static Dictionary<string, CharacterData> CharacterTable = new Dictionary<string, CharacterData>();

    }
}
