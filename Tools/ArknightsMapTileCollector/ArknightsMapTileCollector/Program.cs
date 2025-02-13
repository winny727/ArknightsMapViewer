using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    private static Dictionary<string, bool> tileDict = new Dictionary<string, bool>();

    private static void Main(string[] args)
    {
        if (args != null && args.Length > 0)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string path = args[i];
                CollectMapTile(path);
            }
        }
        else
        {
            CollectMapTile(Directory.GetCurrentDirectory());
        }

        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "MapTileCollect.txt");
        SaveResult(outputPath);
        Console.WriteLine($"\nDone! Output: {outputPath}");
        Console.ReadKey();
    }

    private static void CollectMapTile(string path)
    {
        tileDict.Clear();
        if (Path.GetExtension(path) == "json")
        {
            ParseFile(path);
        }
        else if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                ParseFile(file);
            }
        }
    }

    private static void ParseFile(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            Console.WriteLine($"Parsing File: {path}");
            string text = File.ReadAllText(path);
            var matches = Regex.Matches(text, "\"tileKey\": \"(.+?)\"");
            foreach (Match match in matches)
            {
                string key = match.Groups[1].ToString();
                if (!tileDict.ContainsKey(key))
                {
                    tileDict.Add(key, true);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[Error] ParseFile Error: {ex.Message}, {path}");
        }
    }

    private static void SaveResult(string path)
    {
        string text = "";
        foreach (var item in tileDict)
        {
            text += item.Key + "\n";
        }

        try
        {
            File.WriteAllText(path, text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[Error] WriteFile Error: {ex.Message}, {path}");
        }
    }
}