using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using ArknightsMap;
using System.Drawing;

namespace ArknightsMapViewer
{
    public static class Global
    {
        public static void InitTileColorConfig()
        {
            Color ParseColor(string value)
            {
                Color color = Color.White;
                value = value.Replace(" ", "").Replace("\"", "");
                string[] colors = value.Split(',');
                int a, r, g, b;
                if (colors.Length == 3)
                {
                    int.TryParse(colors[0], out r);
                    int.TryParse(colors[1], out g);
                    int.TryParse(colors[2], out b);
                    color = Color.FromArgb(r, g, b);
                }
                else if(colors.Length == 4)
                {
                    int.TryParse(colors[0], out a);
                    int.TryParse(colors[1], out r);
                    int.TryParse(colors[2], out g);
                    int.TryParse(colors[3], out b);
                    color = Color.FromArgb(a, r, g, b);
                }
                return color;
            }

            string path = Environment.CurrentDirectory + "/TileDefine.txt";
            if (File.Exists(path))
            {
                try
                {
                    string text = File.ReadAllText(path);
                    string[] lines = text.Split('\n');
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string[] values = lines[i].Split('\t');
                        if (values.Length >= 2 && Enum.TryParse(values[0], out TileKey tileKey))
                        {
                            Color tileColor = ParseColor(values[1]);
                            if (!GlobalDefine.TileColor.ContainsKey(tileKey))
                            {
                                GlobalDefine.TileColor.Add(tileKey, tileColor);
                            }
                            else
                            {
                                GlobalDefine.TileColor[tileKey] = tileColor;
                            }

                            string tileText = values[2];
                            if (values.Length >= 4 && !string.IsNullOrEmpty(tileText))
                            {
                                Color textColor = ParseColor(values[3]);
                                if (!GlobalDefine.TileString.ContainsKey(tileKey))
                                {
                                    GlobalDefine.TileString.Add(tileKey, (tileText, textColor));
                                }
                                else
                                {
                                    GlobalDefine.TileString[tileKey] = (tileText, textColor);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"TileDefine.txt Open Failed, {ex.Message}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}
