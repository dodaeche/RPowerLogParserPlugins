using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MSUtil.LogQuery.RPower
{
    struct FieldInfo
    {
        public int size;
        public int offset;
        public string name;
    }

    public class RpowerDB : ILogParserInputContext
    {
        BinaryReader file = null;
        byte[] record;
        byte[] header = new byte[32];
        int firstRec = 0;
        int recLength = 0;
        List<FieldInfo> fieldList;

        public void OpenInput(string filePath)
        {
            try
            {
                fieldList = new List<FieldInfo>();

                // trim whitespace and unquote if needed.
                filePath = filePath.Trim();
                if (filePath[0] == '\'' && filePath[filePath.Length - 1] == '\'')
                    filePath = filePath.Substring(1, filePath.Length - 2);

                if (File.Exists(filePath))
                {
                    file = new BinaryReader(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));
                    header = file.ReadBytes(32);
                    firstRec = (header[9] << 8) | header[8];
                    recLength = (header[11] << 8) | header[10];

                    var field = file.ReadBytes(32);
                    int count = 1;

                    while (field[0] != 0x0d)
                    {
                        FieldInfo fi;
                        byte[] n = new byte[11];

                        Buffer.BlockCopy(field, 0, n, 0, 11);
                        fi.size = field[16];
                        fi.offset = count;
                        fi.name = Encoding.ASCII.GetString(n).Trim();
                        fieldList.Add(fi);
                        count += fi.size;
                        field = file.ReadBytes(32);
                    }

                    file.BaseStream.Seek(firstRec, SeekOrigin.Begin);
                } else
                {
                    file = null;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception in OpenInput: {0}", ex.Message);
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
            if (file == null)
                return 0;
            else return fieldList.Count;
        }

        public string GetFieldName(int index)
        {
            if (file == null)
                return "";

            return fieldList[index].name;
        }

        public int GetFieldType(int index)
        {
            return (int)DATA_TYPE.TYPE_STRING;
        }

        public object GetValue(int index)
        {
            //if (file == null)
            //    return "";

            //byte[] value = new byte[fieldList[index].size];
            //Buffer.BlockCopy(record, fieldList[index].offset, value, 0, fieldList[index].size);
            //return Encoding.ASCII.GetString(value).Trim();

            var sb = new StringBuilder();

            for (int i = fieldList[index].offset; i < fieldList[index].offset + fieldList[index].size; i++)
                sb.Append((char) record[i]);

            return sb.ToString().Trim();
        }

        public bool ReadRecord()
        {
            if (file == null)
                return false;

            try
            {
                record = file.ReadBytes(recLength);
                if (record.Length == recLength)
                    return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception in ReadRecord: {0}", ex.Message);
            }

            return false;
        }
    }
}
