using System;
using System.Collections.Generic;
using System.IO;

namespace MSUtil.LogQuery.RPower
{

    public class RpowerCC : ILogParserInputContext
    {
        static readonly string[] DELIMITERS = { " ", " (", ") ", " : ", " :", ": ", " : ", " : ", " : ", " : ", " : ", " : ", " : ", " : ", " : ", " : ", " : ", " : " };

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
            } catch (Exception ex)
            {
                Console.Error.WriteLine("Exception in CloseInput: {0}", ex.Message);
            }
        }

        public int GetFieldCount()
        {
            return 30;
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
                    return "T2";
                case 4:
                    return "MACHINE";
                case 5:
                    return "ACTION";
                case 6:
                    return "CARD_NUM";
                case 7:
                    return "SWIPE";
                case 8:
                    return "TICKET";
                case 9:
                    return "INV_NUM";
                case 10:
                    return "TNUM";
                case 11:
                    return "AMOUNT";
                case 12:
                    return "TIP_AMT";
                case 13:
                    return "TOTAL";
                case 14:
                    return "APP_CODE";
                case 15:
                    return "APP_SECS";
                case 16:
                    return "SERVER_DATA";
                case 17:
                    return "BATCH_NUM";
                case 18:
                    return "SALE_TYPE";
                case 19:
                    return "TTYPE";
                case 20:
                    return "ORIG_APPROVAL";
                case 21:
                    return "SERVER_ID";
                case 22:
                    return "BATCH_TYPE";
                default:
                    return "FIELD" + (index - 22);
            }

        }

         public int GetFieldType(int index)
        {
            //var TYPE_INTEGER = 1;
            //var TYPE_REAL = 2;
            var TYPE_STRING = 3;
            //var TYPE_TIMESTAMP = 4;
            //var TYPE_NULL = 5;

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
                    strList.Add(recStr.Trim());
                    return strList;
                }
                else
                {
                    strList.Add(recStr.Substring(0, ind).Trim());
                    recStr = recStr.Substring(ind + delim[i].Length);
                }
            }

            strList.Add(recStr);
            return strList;
        }

        private void adjustApprovalRecord()
        {
            var outList = new List<string>();
            var splitTransData = SplitRecord(record[7], new string[] { "-", "-" });
            var splitPaymentData = SplitRecord(record[8].Replace("_", ""), new string[] { "+", "=" });

            for (int i = 0; i < 7; i++)
                outList.Add(record[i]);

            foreach (var s in splitTransData)
                outList.Add(s);

            foreach (var s in splitPaymentData)
                outList.Add(s);

            for (int i = 9; i < record.Count; i++)
                outList.Add(record[i]);

            record = outList;
        }

        private void adjustCaptureRecord()
        {
            if (record.Count < 17)
                return;

            int[] listPositions = { 0, 1, 2, 3, 4, 12, 13, 11, 14, 15, -1, 16, 6, 7, 8, 9, 10, 5 };

            var outList = new List<string>();

            foreach (int i in listPositions)
            {
                if (i < 0)
                    outList.Add("");
                else outList.Add(record[i]);
            }

            record = outList;

            adjustApprovalRecord();
        }

        private void adjustDefaultRecord()
        {
            var outList = new List<string>();

            for (int i = 0; i < 5; i++)
                outList.Add(record[i]);

            for (int i = 0; i < 17; i++)
                outList.Add("");

            for (int i = 5; i < record.Count; i++)
                outList.Add(record[i]);

            record = outList;
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


                    if (record.Count >= 4)
                    {
                        // Because misc sale types sometimes don't have server data.
                        if (record.Count == 16)
                            record.Add("");

                        switch (record[4])
                        {
                            case "Approval":
                            case "*Offline Retry Approved":
                            case "*OFFLINE RETRY DECLINED":
                                adjustApprovalRecord();
                                break;
                            case "Capture":
                                adjustCaptureRecord();
                                break;
                            default:
                                if (record[4].StartsWith("911"))
                                {

                                    adjustCaptureRecord();
                                }
                                else
                                    adjustDefaultRecord();
                                break;
                        }
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
