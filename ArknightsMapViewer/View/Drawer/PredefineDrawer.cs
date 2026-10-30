using System.Drawing;
using System.Text.RegularExpressions;

namespace ArknightsMapViewer
{
    public class PredefineDrawer : IDrawer
    {
        public Predefine.PredefineInst Predefine { get; private set; }
        public bool IsSelected { get; set; } = false;
        public int MapHeight { get; private set; }

        private PredefineDrawer(Predefine.PredefineInst predefine, int mapHeight)
        {
            Predefine = predefine;
            MapHeight = mapHeight;
        }

        public static PredefineDrawer Create(Predefine.PredefineInst predefine, int mapHeight)
        {
            if (predefine == null)
            {
                MainForm.Instance.Log("Create PredefineDrawer Failed, Invalid Predefine", MainForm.LogType.Warning);
                return null;
            }

            return new PredefineDrawer(predefine, mapHeight);
        }

        public void Draw(Bitmap bitmap)
        {
            if (Predefine == null)
            {
                return;
            }

            Point point = GetPoint(Predefine.position);

            string predefineName = Predefine.inst.characterKey;

            if (GlobalDefine.CharacterTable.TryGetValue(predefineName, out CharacterData characterData) && 
                !string.IsNullOrEmpty(characterData.appellation) && characterData.profession == "TRAP")
            {
                predefineName = characterData.appellation;
                if (predefineName.Contains(" "))
                {
                    predefineName = Helper.GetAbbreviation(predefineName);
                }

                if (predefineName.Length > 4)
                {
                    predefineName = predefineName.Substring(0, 4);
                }
            }
            else
            {
                var match = Regex.Match(Predefine.inst.characterKey, ".+?_.+?_(.+)");
                if (match.Success)
                {
                    predefineName = match.Groups[1].ToString();
                }
            }

            DrawPosition(bitmap, point, predefineName);
        }

        private void DrawPosition(Bitmap bitmap, Point point, string name)
        {
            DrawUtil.DrawCircleLabel(bitmap, point, name, GlobalDefine.PREDEFINED_FONT,
                IsSelected ? GlobalDefine.PREDEFINED_SELECTED_TEXTCOLOR : GlobalDefine.TEXT_COLOR,
                IsSelected ? GlobalDefine.PREDEFINED_SELECTED_BACKCOLOR : GlobalDefine.PREDEFINED_BACKCOLOR,
                GlobalDefine.PREDEFINED_LINECOLOR, GlobalDefine.CIRCLE_EDGE_WIDTH + (IsSelected ? 0.5f : 0f));
        }

        private Point GetPoint(Position position)
        {
            return Helper.PositionToPoint(position, new Offset() { x = 0, y = 0 }, MapHeight);
        }
    }
}
