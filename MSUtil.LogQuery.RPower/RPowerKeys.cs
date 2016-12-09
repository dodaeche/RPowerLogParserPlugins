using System;
using System.Collections.Generic;
using System.IO;


namespace MSUtil.LogQuery.RPower
{
    public class RpowerKeys : ILogParserInputContext
    {
        static readonly string[] DELIMITERS = { " ", " ", " :", ":" };

        StreamReader file = null;
        string filename = "";
        List<string> record = null;
        string[] files = { };
        int filesIndex = 0;

        public void OpenInput(string filePath)
        {
            // trim whitespace and unquote if needed.
            filePath = filePath.Trim();
            if (filePath[0] == '\'' && filePath[filePath.Length - 1] == '\'')
                filePath = filePath.Substring(1, filePath.Length - 2);

            var dir = Path.GetDirectoryName(filePath).Trim();

            if (dir.Length == 0)
                dir = @".\";

            try
            {
                files = Directory.GetFiles(dir, Path.GetFileName(filePath));
            }
            catch (Exception)
            {
                files = new string[] { };
            }

            filesIndex = 0;

            if (files.Length > 0)
            {
                try
                {
                    file = new StreamReader(files[filesIndex]);
                    filename = Path.GetFileName(files[filesIndex]);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Exception in OpenInput: {0}", ex.Message);
                }
            }
        }

        public void CloseInput(bool abort)
        {
            try
            {
                if (file != null)
                    file.Close();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception in CloseInput: {0}", ex.Message);
            }
        }

        public int GetFieldCount()
        {
            return 7;
        }

        public string GetFieldName(int index)
        {
            switch (index)
            {
                case 0:
                    return "FILENAME";
                case 1:
                    return "DATE";
                case 2:
                    return "TIME";
                case 3:
                    return "MACHINE";
                case 4:
                    return "ACTION";
                case 5:
                    return "KEY";
                case 6:
                    return "DESCRIPTION";
                default:
                    return "";
            }
        }

        public int GetFieldType(int index)
        {
            var TYPE_INTEGER = 1;
            var TYPE_REAL = 2;
            var TYPE_STRING = 3;
            var TYPE_TIMESTAMP = 4;
            var TYPE_NULL = 5;

            return TYPE_STRING;
        }

        public object GetValue(int index)
        {
            if (index == 0)
                return filename;

            if (index > record.Count)
                return "";

            return record[index - 1];
        }

        private static List<string> SplitRecord(string recStr, string[] delim)
        {
            List<string> strList = new List<string>();

            for (int i = 0; i < delim.Length; i++)
            {
                int ind = recStr.IndexOf(delim[i]);

                if (ind < 0)
                {
                    strList.Add(recStr);
                    return strList;
                }
                else
                {
                    strList.Add(recStr.Substring(0, ind));
                    recStr = recStr.Substring(ind + delim[i].Length);
                }
            }

            strList.Add(recStr);
            return strList;
        }

        public bool ReadRecord()
        {
            if (file == null)
                return false;

            if (file.EndOfStream)
            {
                filesIndex++;
                if (filesIndex < files.Length)
                {
                    file = new StreamReader(files[filesIndex]);
                    filename = Path.GetFileName(files[filesIndex]);
                }
                else return false;
            }

            try
            {
                string recStr = file.ReadLine();

                if (recStr != null)
                {
                    record = SplitRecord(recStr, DELIMITERS);

                    if (record.Count >= 5)
                        switch (record[3])
                        {
                            case "BTN ":
                            case "SWIP":
                                var s = SplitRecord(record[4], new string[] { ":" });

                                if (s.Count == 2)
                                {
                                    record.Remove(record[4]);
                                    record.Add(s[0]);
                                    record.Add(s[1]);
                                }
                                break;

                            default:
                                break;
                        }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception in ReadRecord: {0}", ex.Message);
            }

            return false;
        }
    }

}
