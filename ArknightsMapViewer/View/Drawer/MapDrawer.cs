using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;

namespace ArknightsMapViewer
{
    public class MapDrawer : IDrawer
    {
        public Tile[,] Map { get; private set; }
        public bool HaveUndefinedTiles { get; private set; }
        public bool MarkUndefinedTile { get; set; }
        public bool ShowTileName { get; set; }
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }
        private readonly HashSet<string> reportedUnknownTiles = new HashSet<string>();

        private MapDrawer(Tile[,] map)
        {
            Map = map;
            MapWidth = map.GetLength(0);
            MapHeight = map.GetLength(1);
        }

        public static MapDrawer Create(Tile[,] map)
        {
            if (map == null)
            {
                MainForm.Instance?.Log("Create MapDrawer Failed, Invalid Map", MainForm.LogType.Warning);
                return null;
            }
            return new MapDrawer(map);
        }

        public void Draw(Bitmap bitmap)
        {
            HaveUndefinedTiles = false;
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using var centered = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            using var coordinates = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far,
                FormatFlags = StringFormatFlags.NoWrap };
            for (int row = 0; row < MapHeight; row++)
                for (int col = 0; col < MapWidth; col++) DrawTile(graphics, row, col, centered, coordinates);
        }

        private static bool CanDeploy(Tile tile) => tile.buildableType == BuildableType.MELEE ||
            tile.buildableType == BuildableType.RANGED || tile.buildableType == BuildableType.ALL;

        private static bool ShouldDrawDeploymentHatch(Tile tile)
        {
            if (CanDeploy(tile)) return false;
            switch (tile.tileKey)
            {
                case "tile_end":
                case "tile_flystart":
                case "tile_hole":
                case "tile_start":
                case "tile_telin":
                case "tile_telout": return false;
                default: return true;
            }
        }

        private static Color SurfaceColor(Tile tile, TileInfo info)
        {
            if (info?.tileColor != null) return info.tileColor.Value;
            bool highland = tile.heightType == HeightType.HIGHLAND;
            bool restricted = !CanDeploy(tile) && (tile.passableMask & PassableMask.WALK_ONLY) == 0;
            Color basis = highland ? GlobalDefine.DEFAULT_WALL_COLOR : restricted ? GlobalDefine.DEFAULT_FORBIDDEN_COLOR :
                CanDeploy(tile) ? GlobalDefine.DEFAULT_ROAD_COLOR : GlobalDefine.DEFAULT_FLOOR_COLOR;
            uint hash = StableHash(tile.tileKey ?? "");
            double hue = (basis.GetHue() + hash % 360) % 360;
            // 自动色以灰阶为主，仅保留轻微色偏来辅助区分类型。
            double saturation = 0.035 + ((hash >> 9) % 4) / 100.0;
            double offset = ((hash >> 17) % 51 - 25.0) / 1000.0;
            double lightness = highland
                ? Math.Max(0.62, Math.Min(0.78, basis.GetBrightness() + offset))
                : Math.Max(0.24, Math.Min(0.42, basis.GetBrightness() + offset));
            return FromHsl(basis.A, hue, saturation, lightness);
        }

        // 固定的 UTF-16 FNV-1a，不依赖进程、地图顺序或运行时的 string.GetHashCode。
        private static uint StableHash(string key)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char character in key) hash = (hash ^ character) * 16777619;
                return hash;
            }
        }

        private static Color FromHsl(int alpha, double hue, double saturation, double lightness)
        {
            double chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
            double sector = hue / 60;
            double secondary = chroma * (1 - Math.Abs(sector % 2 - 1));
            double r = 0, g = 0, b = 0;
            if (sector < 1) { r = chroma; g = secondary; }
            else if (sector < 2) { r = secondary; g = chroma; }
            else if (sector < 3) { g = chroma; b = secondary; }
            else if (sector < 4) { g = secondary; b = chroma; }
            else if (sector < 5) { r = secondary; b = chroma; }
            else { r = chroma; b = secondary; }
            double m = lightness - chroma / 2;
            return Color.FromArgb(alpha, (int)Math.Round((r + m) * 255),
                (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
        }

        internal IReadOnlyList<TileLegendEntry> GetLegendEntries()
        {
            return Map.Cast<Tile>().GroupBy(tile => tile.tileKey ?? "", StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    TileInfo info = group.First().GetTileInfo();
                    string name = !string.IsNullOrWhiteSpace(info?.name) ? info.name :
                        !string.IsNullOrWhiteSpace(info?.tileText) ? info.tileText :
                        string.IsNullOrEmpty(group.Key) ? "未命名地块" : group.Key;
                    var variants = group.GroupBy(tile => new
                    {
                        Color = SurfaceColor(tile, tile.GetTileInfo()).ToArgb(),
                        tile.heightType,
                        Hatched = ShouldDrawDeploymentHatch(tile),
                        Deployable = CanDeploy(tile)
                    }).OrderBy(variant => variant.Key.heightType).ThenBy(variant => variant.Key.Deployable)
                        .ThenBy(variant => variant.Key.Color)
                        .Select(variant => new TileLegendVariant(Color.FromArgb(variant.Key.Color),
                            variant.Key.heightType, variant.Key.Hatched, variant.Key.Deployable, variant.Count())).ToArray();
                    return new TileLegendEntry(group.Key, name, variants);
                }).ToArray();
        }

        internal static void DrawDeploymentHatch(Graphics g, Rectangle rect, Color surface)
        {
            using var hatch = new HatchBrush(HatchStyle.ForwardDiagonal,
                Color.FromArgb(surface.A * 35 / 255, Ink(surface)), Color.Transparent);
            g.FillRectangle(hatch, rect);
        }

        private bool IsMatchingHighland(int col, int row, string tileKey) =>
            col >= 0 && col < MapWidth && row >= 0 && row < MapHeight &&
            Map[col, row].heightType == HeightType.HIGHLAND &&
            string.Equals(Map[col, row].tileKey, tileKey, StringComparison.Ordinal);

        // 只绘制相连高台的外露边。
        private static void DrawRaisedEdges(Graphics g, Rectangle rect, Color surface, int bevel,
            bool top, bool right, bool bottom, bool left)
        {
            GraphicsState state = g.Save();
            try
            {
                // 相接的面延伸到本格之外再裁剪，避免整数接缝被抗锯齿只覆盖一半。
                g.SetClip(rect, CombineMode.Intersect);
                if (right)
                {
                    // 右侧阴影填满到顶边缘，右上角与左下角均使用直角收边。
                    using var side = new SolidBrush(Mix(surface, Color.Black, 0.32f));
                    g.FillPolygon(side, new[] { new Point(rect.Right - bevel - 1, rect.Y + (top ? 1 : -1)),
                        new Point(rect.Right - 1, rect.Y + (top ? 1 : -1)), new Point(rect.Right - 1, rect.Bottom - (bottom ? 1 : -1)),
                        new Point(rect.Right - bevel - 1, rect.Bottom - (bottom ? bevel + 1 : -1)) });
                }
                if (bottom)
                {
                    // 底部阴影填满到左边缘，去掉左下角的斜切留白。
                    using var shadow = new SolidBrush(Mix(surface, Color.Black, 0.55f));
                    g.FillPolygon(shadow, new[] { new Point(rect.X + (left ? 1 : -1), rect.Bottom - bevel - 1),
                        new Point(rect.Right - (right ? bevel + 1 : -1), rect.Bottom - bevel - 1),
                        new Point(rect.Right - (right ? 1 : -1), rect.Bottom - 1), new Point(rect.X + (left ? 1 : -1), rect.Bottom - 1) });
                }
                // 深色底面少混入白色，避免禁入区亮边泛白，同时保留地块原有色调。
                float highlightAmount = 0.08f + 0.2f * surface.GetBrightness();
                using var rim = new Pen(Mix(surface, Color.White, highlightAmount), Math.Max(1, rect.Width / 28f));
                if (top) g.DrawLine(rim, rect.X + (left ? 2 : -1), rect.Y + 2, rect.Right - (right ? bevel + 2 : -1), rect.Y + 2);
                if (left) g.DrawLine(rim, rect.X + 2, rect.Y + (top ? 2 : -1), rect.X + 2, rect.Bottom - (bottom ? bevel + 2 : -1));
            }
            finally
            {
                g.Restore(state);
            }
        }

        // 三个同类高台围住不同类型或低格时，两条外沿在本类型内部相交，需要补齐凹角。
        // 只接四邻连通的凹角，不把仅对角接触的平台连接起来。
        private static void DrawInnerCorners(Graphics g, Rectangle rect, Color surface, int bevel,
            bool topLeft, bool topRight, bool bottomRight, bool bottomLeft)
        {
            if (!topLeft && !topRight && !bottomRight && !bottomLeft) return;
            float x = rect.Left, y = rect.Top, right = rect.Right, bottom = rect.Bottom;
            float inset = bevel + 1;
            using var side = new SolidBrush(Mix(surface, Color.Black, 0.32f));
            using var shadow = new SolidBrush(Mix(surface, Color.Black, 0.55f));
            using var rim = new Pen(Mix(surface, Color.White, 0.08f + 0.2f * surface.GetBrightness()), Math.Max(1, rect.Width / 28f));
            GraphicsState state = g.Save();
            try
            {
                // 顶点落在格子边界，限制抗锯齿覆盖范围，避免侵入相邻低格。
                g.SetClip(rect, CombineMode.Intersect);
                if (bottomRight)
                {
                    var inner = new PointF(right - inset, bottom - inset);
                    var corner = new PointF(right + 1, bottom + 1);
                    g.FillPolygon(shadow, new[] { inner, new PointF(right + 1, inner.Y), corner });
                    g.FillPolygon(side, new[] { inner, corner, new PointF(inner.X, bottom + 1) });
                }
                if (topRight)
                {
                    var inner = new PointF(right - inset, y + 2);
                    g.FillPolygon(side, new[] { new PointF(inner.X, y - 1), new PointF(right + inset / 2, y - 1), inner });
                    g.DrawLine(rim, inner, new PointF(right + 1, inner.Y));
                }
                if (bottomLeft)
                {
                    var inner = new PointF(x + 2, bottom - inset);
                    g.FillPolygon(shadow, new[] { new PointF(x - 1, inner.Y), new PointF(x - 1, bottom + inset / 2), inner });
                    g.DrawLine(rim, inner, new PointF(inner.X, bottom + 1));
                }
                if (topLeft)
                    g.DrawLines(rim, new[] { new PointF(x - 1, y + 2), new PointF(x + 2, y + 2), new PointF(x + 2, y - 1) });
            }
            finally
            {
                g.Restore(state);
            }
        }

        private static Color Mix(Color from, Color to, float amount) => Color.FromArgb(from.A,
            (int)(from.R + (to.R - from.R) * amount), (int)(from.G + (to.G - from.G) * amount),
            (int)(from.B + (to.B - from.B) * amount));

        private static Color Ink(Color background) =>
            background.R * 0.2126 + background.G * 0.7152 + background.B * 0.0722 > 140
                ? Color.FromArgb(28, 28, 28) : Color.FromArgb(238, 238, 238);

        private void DrawTile(Graphics g, int row, int col, StringFormat centered, StringFormat coordinates)
        {
            Tile tile = Map[col, row];
            TileInfo info = tile.GetTileInfo();
            bool unknown = info == null;
            if (unknown)
            {
                HaveUndefinedTiles = true;
                if (reportedUnknownTiles.Add(tile.tileKey ?? ""))
                    MainForm.Instance?.Log("Undefined Tile: " + tile.tileKey, MainForm.LogType.Warning);
            }
            int length = GlobalDefine.TILE_PIXLE;
            var rect = new Rectangle(col * length, (MapHeight - row - 1) * length, length, length);
            Color surface = SurfaceColor(tile, info);
            // 底色精确覆盖整数格，避免后绘制的格子半透明地盖住相邻已画好的侧面。
            using (var brush = new SolidBrush(surface))
            {
                SmoothingMode smoothing = g.SmoothingMode;
                g.SmoothingMode = SmoothingMode.None;
                g.FillRectangle(brush, rect);
                if (ShouldDrawDeploymentHatch(tile)) DrawDeploymentHatch(g, rect, surface);
                g.SmoothingMode = smoothing;
            }
            bool highland = tile.heightType == HeightType.HIGHLAND;
            // 地图 row 向上递增，与图片 Y 方向相反。仅同 tileKey 的四邻接高台共享外轮廓。
            bool joinTop = highland && IsMatchingHighland(col, row + 1, tile.tileKey);
            bool joinRight = highland && IsMatchingHighland(col + 1, row, tile.tileKey);
            bool joinBottom = highland && IsMatchingHighland(col, row - 1, tile.tileKey);
            bool joinLeft = highland && IsMatchingHighland(col - 1, row, tile.tileKey);
            bool innerTopLeft = joinTop && joinLeft && !IsMatchingHighland(col - 1, row + 1, tile.tileKey);
            bool innerTopRight = joinTop && joinRight && !IsMatchingHighland(col + 1, row + 1, tile.tileKey);
            bool innerBottomRight = joinBottom && joinRight && !IsMatchingHighland(col + 1, row - 1, tile.tileKey);
            bool innerBottomLeft = joinBottom && joinLeft && !IsMatchingHighland(col - 1, row - 1, tile.tileKey);
            int bevel = highland && length >= 12 ? Math.Max(3, length / 9) : 0;
            if (bevel > 0)
            {
                DrawRaisedEdges(g, rect, surface, bevel, !joinTop, !joinRight, !joinBottom, !joinLeft);
                DrawInnerCorners(g, rect, surface, bevel, innerTopLeft, innerTopRight, innerBottomRight, innerBottomLeft);
            }
            using (var outerGrid = new Pen(Mix(surface, Color.FromArgb(12, 12, 12), 0.6f)))
            using (var innerGrid = new Pen(Mix(surface, Color.FromArgb(12, 12, 12), 0.18f)))
            {
                // 内部格线只画一次，并止于顶面，避免穿过外沿及凹角的接合处。
                int topStart = bevel > 0 && joinTop && (!joinLeft || innerTopLeft) ? 3 : 0;
                int topEnd = bevel > 0 && joinTop && (!joinRight || innerTopRight) ? bevel + (innerTopRight ? 1 : 0) : 0;
                int leftStart = bevel > 0 && joinLeft && (!joinTop || innerTopLeft) ? 3 : 0;
                int leftEnd = bevel > 0 && joinLeft && (!joinBottom || innerBottomLeft) ? bevel + (innerBottomLeft ? 1 : 0) : 0;
                g.DrawLine(joinTop ? innerGrid : outerGrid, rect.Left + topStart, rect.Top, rect.Right - 1 - topEnd, rect.Top);
                g.DrawLine(joinLeft ? innerGrid : outerGrid, rect.Left, rect.Top + leftStart, rect.Left, rect.Bottom - 1 - leftEnd);
                if (!joinRight) g.DrawLine(outerGrid, rect.Right - 1, rect.Top, rect.Right - 1, rect.Bottom - 1);
                if (!joinBottom) g.DrawLine(outerGrid, rect.Left, rect.Bottom - 1, rect.Right - 1, rect.Bottom - 1);
            }
            if (length < 20) return;

            Color ink = Ink(surface);
            int pad = Math.Max(3, length / 14);
            int markerSize = Math.Max(12, length / 4);
            var tileNameBounds = new RectangleF(rect.X + pad, rect.Y + markerSize + pad,
                Math.Max(1, length - 2 * pad), Math.Max(1, length - markerSize - 2 * pad - 10));
            string tileName = info?.tileText;
            if (ShowTileName && string.IsNullOrWhiteSpace(tileName) && !string.IsNullOrWhiteSpace(info?.name))
                tileName = info.name.Length > 4 ? info.name.Substring(0, 3) + "…" : info.name;
            if (!string.IsNullOrEmpty(tileName))
            {
                DrawUtil.DrawFittedString(g, tileName, tileNameBounds, GlobalDefine.TEXT_FONT, info.textColor ?? ink);
            }

            // 坐标留在顶面右下角，也避开右下凹角新接合的侧面。
            using (var brush = new SolidBrush(Mix(ink, surface, 0.3f)))
                g.DrawString($"{col},{row}", GlobalDefine.INDEX_FONT, brush,
                    new RectangleF(rect.X + pad, rect.Y + pad, length - 2 * pad - (joinRight && !innerBottomRight ? 0 : bevel),
                        length - pad - 2 - (joinBottom && !innerBottomRight ? 0 : bevel)), coordinates);
            if (unknown && MarkUndefinedTile)
            {
                using var pen = new Pen(Color.FromArgb(245, 192, 99), Math.Max(1, length / 32f)) { DashStyle = DashStyle.Dot };
                g.DrawRectangle(pen, rect.X + 2, rect.Y + 2, length - 5, length - 5);
                using var brush = new SolidBrush(Color.FromArgb(245, 192, 99));
                g.DrawString("?", GlobalDefine.INDEX_FONT, brush, new RectangleF(rect.Right - markerSize - pad, rect.Y + pad, markerSize, markerSize), centered);
            }
        }

    }
    internal sealed class TileLegendEntry
    {
        public string TileKey { get; }
        public string Name { get; }
        public IReadOnlyList<TileLegendVariant> Variants { get; }

        public TileLegendEntry(string tileKey, string name, IReadOnlyList<TileLegendVariant> variants)
        {
            TileKey = tileKey;
            Name = name;
            Variants = variants;
        }
    }

    internal sealed class TileLegendVariant
    {
        public Color Color { get; }
        public HeightType HeightType { get; }
        public bool Hatched { get; }
        public bool CanDeploy { get; }
        public int Count { get; }

        public TileLegendVariant(Color color, HeightType heightType, bool hatched, bool canDeploy, int count)
        {
            Color = color;
            HeightType = heightType;
            Hatched = hatched;
            CanDeploy = canDeploy;
            Count = count;
        }
    }
}
