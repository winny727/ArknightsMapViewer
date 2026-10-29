using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace ArknightsMapViewer
{
    internal static class DrawConfigValidator
    {
        // 从默认结构逐项读取，单个字段类型错误不应丢弃其他有效设置。
        internal static DrawConfig Parse(string json, Action<string> warn)
        {
            var defaults = new DrawConfig();
            var source = JToken.Parse(json) as JObject;
            if (source == null)
            {
                warn("配置根节点必须是对象，使用默认配置。");
                return defaults;
            }
            var normalized = JObject.FromObject(defaults);
            foreach (var group in normalized.Properties())
            {
                if (!source.TryGetValue(group.Name, StringComparison.OrdinalIgnoreCase, out JToken input)) continue;
                if (!(input is JObject values))
                {
                    warn($"{group.Name}: 必须是对象，使用该组默认值。");
                    continue;
                }
                foreach (var field in ((JObject)group.Value).Properties())
                {
                    if (!values.TryGetValue(field.Name, StringComparison.OrdinalIgnoreCase, out JToken value)) continue;
                    bool valid = false;
                    switch (field.Value.Type)
                    {
                        case JTokenType.Integer:
                            valid = value.Type == JTokenType.Integer &&
                                int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
                            break;
                        case JTokenType.Float:
                            valid = (value.Type == JTokenType.Integer || value.Type == JTokenType.Float) &&
                                float.TryParse(value.ToString(Newtonsoft.Json.Formatting.None), NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out float number) &&
                                !float.IsNaN(number) && !float.IsInfinity(number);
                            break;
                        case JTokenType.String:
                            valid = value.Type == JTokenType.String;
                            break;
                    }
                    if (valid) field.Value = value.DeepClone();
                    else warn($"{group.Name}.{field.Name}: 类型错误或数值溢出，使用默认值 {field.Value}。");
                }
            }
            return normalized.ToObject<DrawConfig>();
        }

        internal static void Apply(DrawConfig config, float scale, Action<string> warn)
        {
            var defaults = new DrawConfig();
            config ??= defaults;
            if (config.Size == null) { warn("Size: 不能为空，使用默认值。"); config.Size = defaults.Size; }
            if (config.Font == null) { warn("Font: 不能为空，使用默认值。"); config.Font = defaults.Font; }
            if (config.Color == null) { warn("Color: 不能为空，使用默认值。"); config.Color = defaults.Color; }
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0 || scale > 8)
            {
                warn("DPI缩放值无效，使用1倍缩放。");
                scale = 1;
            }

            int Size(int value, int fallback, int maximum, string name)
            {
                if (value < 1 || value > maximum)
                {
                    warn($"Size.{name}: 必须在1到{maximum}之间，使用默认值 {fallback}。");
                    value = fallback;
                }
                // 保留原有DPI比例，同时防止小尺寸取整为0（格宽还会用作除数）。
                return Math.Max(1, (int)(value * scale));
            }

            var size = config.Size;
            var ds = defaults.Size;
            int tile = Size(size.tilePixle, ds.tilePixle, 512, nameof(size.tilePixle));
            int line = Size(size.lineWidth, ds.lineWidth, 128, nameof(size.lineWidth));
            int edge = Size(size.circleEdgeWidth, ds.circleEdgeWidth, 128, nameof(size.circleEdgeWidth));
            int circle = Size(size.circleRadius, ds.circleRadius, 512, nameof(size.circleRadius));
            int point = Size(size.pointRadius, ds.pointRadius, 512, nameof(size.pointRadius));

            var c = config.Color;
            var dc = defaults.Color;
            Color[] colors =
            {
                ReadColor(c.textColor, dc.textColor, nameof(c.textColor), warn),
                ReadColor(c.lineColor, dc.lineColor, nameof(c.lineColor), warn),
                ReadColor(c.circleColor, dc.circleColor, nameof(c.circleColor), warn),
                ReadColor(c.lengthColor, dc.lengthColor, nameof(c.lengthColor), warn),
                ReadColor(c.defaultRoadColor, dc.defaultRoadColor, nameof(c.defaultRoadColor), warn),
                ReadColor(c.defaultWallColor, dc.defaultWallColor, nameof(c.defaultWallColor), warn),
                ReadColor(c.defaultFloorColor, dc.defaultFloorColor, nameof(c.defaultFloorColor), warn),
                ReadColor(c.defaultForbiddenColor, dc.defaultForbiddenColor, nameof(c.defaultForbiddenColor), warn),
                ReadColor(c.predefinedForeColor, dc.predefinedForeColor, nameof(c.predefinedForeColor), warn),
                ReadColor(c.predefinedBackColor, dc.predefinedBackColor, nameof(c.predefinedBackColor), warn),
                ReadColor(c.predefinedSelectedBackColor, dc.predefinedSelectedBackColor, nameof(c.predefinedSelectedBackColor), warn),
                ReadColor(c.predefinedSelectedTextColor, dc.predefinedSelectedTextColor, nameof(c.predefinedSelectedTextColor), warn),
            };

            var fonts = new List<Font>();
            try
            {
                var f = config.Font;
                var df = defaults.Font;
                fonts.Add(ReadFont(f.textFont, f.textFontSize, f.textFontStyle, df.textFont, df.textFontSize, df.textFontStyle, "textFont", warn));
                fonts.Add(ReadFont(f.indexFont, f.indexFontSize, f.indexFontStyle, df.indexFont, df.indexFontSize, df.indexFontStyle, "indexFont", warn));
                fonts.Add(ReadFont(f.timeFont, f.timeFontSize, f.timeFontStyle, df.timeFont, df.timeFontSize, df.timeFontStyle, "timeFont", warn));
                fonts.Add(ReadFont(f.lenghtFont, f.lenghtFontSize, f.lenghtFontStyle, df.lenghtFont, df.lenghtFontSize, df.lenghtFontStyle, "lenghtFont", warn));
                fonts.Add(ReadFont(f.predefinedFont, f.predefinedFontSize, f.predefinedFontStyle, df.predefinedFont, df.predefinedFontSize, df.predefinedFontStyle, "predefinedFont", warn));

                // 所有资源准备成功后再统一替换；准备失败时全局配置保持原样。
                var oldFonts = new[] { GlobalDefine.TEXT_FONT, GlobalDefine.INDEX_FONT,
                    GlobalDefine.TIME_FONT, GlobalDefine.LENGTH_FONT, GlobalDefine.PREDEFINED_FONT };
                GlobalDefine.TILE_PIXLE = tile;
                GlobalDefine.LINE_WIDTH = line;
                GlobalDefine.CIRCLE_EDGE_WIDTH = edge;
                GlobalDefine.CIRCLE_RADIUS = circle;
                GlobalDefine.POINT_RADIUS = point;
                GlobalDefine.TEXT_FONT = fonts[0];
                GlobalDefine.INDEX_FONT = fonts[1];
                GlobalDefine.TIME_FONT = fonts[2];
                GlobalDefine.LENGTH_FONT = fonts[3];
                GlobalDefine.PREDEFINED_FONT = fonts[4];
                GlobalDefine.TEXT_COLOR = colors[0];
                GlobalDefine.LINE_COLOR = colors[1];
                GlobalDefine.CIRCLE_COLOR = colors[2];
                GlobalDefine.LENGTH_COLOR = colors[3];
                GlobalDefine.DEFAULT_ROAD_COLOR = colors[4];
                GlobalDefine.DEFAULT_WALL_COLOR = colors[5];
                GlobalDefine.DEFAULT_FLOOR_COLOR = colors[6];
                GlobalDefine.DEFAULT_FORBIDDEN_COLOR = colors[7];
                GlobalDefine.PREDEFINED_LINECOLOR = colors[8];
                GlobalDefine.PREDEFINED_BACKCOLOR = colors[9];
                GlobalDefine.PREDEFINED_SELECTED_BACKCOLOR = colors[10];
                GlobalDefine.PREDEFINED_SELECTED_TEXTCOLOR = colors[11];
                fonts.Clear(); // 所有权已转交给全局绘图配置。
                foreach (Font font in oldFonts) font?.Dispose();
            }
            finally
            {
                foreach (Font font in fonts) font.Dispose();
            }
        }

        private static Color ReadColor(string value, string fallback, string name, Action<string> warn)
        {
            if (TryColor(value, out Color color)) return color;
            warn($"Color.{name}: 颜色无效，使用默认值 {fallback}。");
            TryColor(fallback, out color);
            return color;
        }

        private static bool TryColor(string value, out Color color)
        {
            color = Color.Empty;
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) return false;
            if (Regex.IsMatch(value, @"\A#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})\z"))
            {
                string hex = value.Substring(1);
                if (hex.Length == 3) hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
                uint argb = uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (hex.Length == 6) argb |= 0xff000000;
                color = Color.FromArgb(unchecked((int)argb));
                return true;
            }
            if (!Regex.IsMatch(value, @"\A[A-Za-z]+\z")) return false;
            try
            {
                color = ColorTranslator.FromHtml(value);
                return !color.IsEmpty && color.IsKnownColor;
            }
            // ColorConverter对未知名称也可能抛出普通Exception，统一作为无效颜色处理。
            catch (Exception) { return false; }
        }

        private static Font ReadFont(string name, float size, string styleText, string defaultName,
            float defaultSize, string defaultStyle, string key, Action<string> warn)
        {
            if (float.IsNaN(size) || float.IsInfinity(size) || size < 1 || size > 256)
            {
                warn($"Font.{key}Size: 字号必须在1到256之间且为有限数，使用默认值 {defaultSize}。");
                size = defaultSize;
            }
            const FontStyle allowed = FontStyle.Bold | FontStyle.Italic | FontStyle.Underline | FontStyle.Strikeout;
            if (!Enum.TryParse(styleText, true, out FontStyle style) || (style & ~allowed) != 0)
            {
                warn($"Font.{key}Style: 字体样式无效，使用默认值 {defaultStyle}。");
                style = (FontStyle)Enum.Parse(typeof(FontStyle), defaultStyle);
            }

            FontFamily family = OpenFamily(name);
            if (family == null)
            {
                warn($"Font.{key}: 字体为空、无效或未安装，使用默认字体 {defaultName}（不可用时使用系统无衬线字体）。");
                family = OpenFamily(defaultName) ?? FontFamily.GenericSansSerif;
            }
            using (family)
            {
                FontStyle face = style & (FontStyle.Bold | FontStyle.Italic);
                if (!family.IsStyleAvailable(face))
                {
                    warn($"Font.{key}Style: 字体不支持指定样式，使用该字体可用样式。");
                    foreach (FontStyle candidate in new[] { FontStyle.Regular, FontStyle.Bold, FontStyle.Italic, FontStyle.Bold | FontStyle.Italic })
                    {
                        if (!family.IsStyleAvailable(candidate)) continue;
                        style = candidate | (style & (FontStyle.Underline | FontStyle.Strikeout));
                        break;
                    }
                }
                try { return new Font(family, size, style); }
                catch (Exception ex) when (ex is ArgumentException || ex is ExternalException)
                {
                    warn($"Font.{key}: 字体资源创建失败，使用系统默认字体。{ex.Message}");
                    return (Font)SystemFonts.DefaultFont.Clone();
                }
            }
        }

        private static FontFamily OpenFamily(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            foreach (char character in name) if (char.IsControl(character)) return null;
            try { return new FontFamily(name.Trim()); }
            catch (ArgumentException) { return null; }
            catch (ExternalException) { return null; }
        }
    }
}
