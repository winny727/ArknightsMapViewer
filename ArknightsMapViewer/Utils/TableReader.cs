using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class TableReader
{
    public class TableLine : ICloneable
    {
        public string Key => key;
        private string key;
        private string[] values;
        private Dictionary<string, int> colIndexDict;

        public string this[string title]
        {
            get
            {
                return GetValue(title);
            }
        }

        public TableLine(string key, string[] values, Dictionary<string, int> colIndexDict)
        {
            this.key = key;
            this.values = values;
            this.colIndexDict = colIndexDict;
        }

        public string GetValue(string title)
        {
            if (colIndexDict == null || !colIndexDict.TryGetValue(title, out int colIndex))
            {
                return null;
            }

            if (colIndex < 0 || colIndex >= values.Length)
            {
                return null;
            }

            return values[colIndex];
        }

        public T GetValue<T>(string title, T defaultValue = default)
        {
            string data = GetValue(title);
            return data != null ? data.ConvertTo(defaultValue) : defaultValue;
        }

        public object Clone()
        {
            return new TableLine(key, (string[])values.Clone(), colIndexDict);
        }
    }

    private Dictionary<string, TableLine> datas;
    private Dictionary<string, int> colIndexDict;

    public Dictionary<string, TableLine> Datas
    {
        get
        {
            var datas = new Dictionary<string, TableLine>();
            foreach (var item in this.datas)
            {
                datas.Add(item.Key, (TableLine)item.Value.Clone());
            }
            return datas;
        }
    }

    public TableLine this[string key]
    {
        get
        {
            return GetLine(key);
        }
    }


    public TableReader(string filePath)
    {
        string[] lines = ReadFile(filePath);
        InitDatas(lines);
    }

    private string[] ReadFile(string filePath)
    {
        if (filePath == null)
        {
            throw new ArgumentNullException();
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(filePath);
        }

        // 读取数据文件
        // var lines = File.ReadAllLines(filePath, Encoding.UTF8);

        // Excel会占用文件导致File.ReadAllLines读取不了，还是用FileStream
        List<string> lines = new List<string>();
        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) //FileShare.ReadWrite参数表示可以与其他进程共享读写权限
        {
            using (StreamReader reader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
        }
        return lines.ToArray();
    }

    private void InitDatas(string[] lines, int titleRowIndex = 0, int dataStartRowIndex = 1)
    {
        if (lines == null || lines.Length <= 0)
        {
            throw new ArgumentException("Line Count Error");
        }

        string[] GetValues(string line) => line.Split('\t');

        //Init titles
        colIndexDict = new Dictionary<string, int>();
        string[] titles = GetValues(lines[titleRowIndex]);
        int maxColIndex = -1;
        for (int i = 0; i < titles.Length; i++)
        {
            string title = titles[i];
            if (string.IsNullOrEmpty(title))
            {
                continue;
            }

            if (colIndexDict.ContainsKey(title))
            {
                throw new ArgumentException($"Title {title} is Already Exist");
            }

            maxColIndex = i;
            colIndexDict.Add(title, i);
        }

        if (maxColIndex < 0)
        {
            throw new ArgumentException($"Column Count Error");
        }

        //Init Datas
        datas = new Dictionary<string, TableLine>();
        for (int i = dataStartRowIndex; i < lines.Length; i++)
        {
            string[] line = GetValues(lines[i]);
            if (line.Length <= 0)
            {
                continue;
            }

            string key = line[0];
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            if (datas.ContainsKey(key))
            {
                throw new ArgumentException($"Data Key {key} is Already Exist");
            }

            string[] values = new string[maxColIndex];
            for (int j = 0; j < maxColIndex; j++)
            {
                values[j] = line[j];
            }
            datas.Add(key, new TableLine(key, values, colIndexDict));
        }
    }

    public TableLine GetLine(string key)
    {
        if (datas == null || !datas.TryGetValue(key, out TableLine line))
        {
            return null;
        }
        return line;
    }

    public string GetData(string key, string title)
    {
        TableLine line = GetLine(key);
        return line != null ? line[title] : null;
    }

    public string GetData(int key, string title)
    {
        return GetData(key.ToString(), title);
    }

    public T GetData<T>(string key, string title, T defaultValue = default)
    {
        string data = GetData(key, title);
        return data != null ? data.ConvertTo(defaultValue) : defaultValue;
    }

    public T GetData<T>(int key, string title, T defaultValue = default)
    {
        return GetData(key.ToString(), title, defaultValue);
    }

    public void ForEach(Action<string, TableLine> callback)
    {
        if (callback == null)
        {
            return;
        }

        foreach (var item in datas)
        {
            callback(item.Key, item.Value);
        }
    }
}

