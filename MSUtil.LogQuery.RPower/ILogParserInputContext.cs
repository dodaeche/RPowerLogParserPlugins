using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSUtil.LogQuery.RPower
{

    public enum DATA_TYPE
    {
        TYPE_INTEGER = 1,
        TYPE_REAL,
        TYPE_STRING,
        TYPE_TIMESTAMP,
        TYPE_NULL
    };


    public interface ILogParserInputContext
    {       
        // Takes the "from string" from the sql query and initializes the input source.
        void OpenInput(string from);
        // Close input source and cleans up. Abort = true if it is closing because of an error.
        void CloseInput(bool abort);
        // Returns the field count of the table/input source.
        int GetFieldCount();
        // Returns the table field name by index.
        string GetFieldName(int index);
        // Returns the type of field by index
        int GetFieldType(int index);
        // Reads the next record from the input source. Returns true if successfull.
        bool ReadRecord();
        // Takes an index and returns the value for that index in the current record.
        object GetValue(int index);
    }
}
