using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace EntityOH.Controllers
{
    public sealed class SmartReader : IDataReader
    {
        private IDataReader _Reader;

        public SmartReader(IDataReader reader)
        {
            _Reader = reader;
        }



        /// <summary>
        /// Returns the value if exist in reader for specific type.
        /// </summary>
        /// <typeparam name="TargetType"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public object GetFieldValue<TargetType>(string name)
        {
            int i = GetOrdinal(name);

            if (i >= 0)
            {
                if (GetFieldType(i) == typeof(TargetType))
                {
                    return this.GetValue(i);
                }
            }
            
            return default(TargetType);
        }

        #region IDataRecord Members

        public int FieldCount
        {
            get { return _Reader.FieldCount; }
        }

        public bool GetBoolean(int i)
        {
            return _Reader.GetBoolean(i);
        }

        public byte GetByte(int i)
        {
            return _Reader.GetByte(i);
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            return _Reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        }

        public char GetChar(int i)
        {
            return _Reader.GetChar(i);
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return _Reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        }

        public IDataReader GetData(int i)
        {
            return _Reader.GetData(i);
        }

        public string GetDataTypeName(int i)
        {
            return _Reader.GetDataTypeName(i);
        }

        public DateTime GetDateTime(int i)
        {
            return _Reader.GetDateTime(i);
        }

        public decimal GetDecimal(int i)
        {
            return _Reader.GetDecimal(i);
        }

        public double GetDouble(int i)
        {
            return _Reader.GetDouble(i);
        }

        public Type GetFieldType(int i)
        {
            return _Reader.GetFieldType(i);
        }

        public float GetFloat(int i)
        {
            return _Reader.GetFloat(i);
        }

        public Guid GetGuid(int i)
        {
            return _Reader.GetGuid(i);
        }

        public short GetInt16(int i)
        {
            return _Reader.GetInt16(i);
        }

        public int GetInt32(int i)
        {
            return _Reader.GetInt32(i);
        }

        public long GetInt64(int i)
        {
            return _Reader.GetInt64(i);
        }
        
        public string GetName(int i)
        {
            return _Reader.GetName(i);
        }


        /// <summary>
        /// Search for the name and ignore the case.
        /// it returns -1 in case of no matching occured instead of throwing exception.
        /// this custom reader was meant to be used in getting partial set of data from the reader.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetOrdinal(string name)
        {
            int i = _Reader.FieldCount - 1;

            while (i >= 0)
            {
                string tname = GetName(i);

                if (tname.Equals(name, StringComparison.OrdinalIgnoreCase)) break;

                i--;
            }
            return i;
        }

        public string GetString(int i)
        {
            return _Reader.GetString(i);
        }

        public object GetValue(int i)
        {
            return _Reader.GetValue(i);
        }

        public int GetValues(object[] values)
        {
            return _Reader.GetValues(values);
        }

        public bool IsDBNull(int i)
        {
            return _Reader.IsDBNull(i);
        }

        public object this[string name]
        {
            get { return _Reader[name]; }
        }

        public object this[int i]
        {
            get { return _Reader[i]; }
        }

        #endregion

        #region IDataReader Members

        public void Close()
        {
            _Reader.Close();
        }

        public int Depth
        {
            get { return _Reader.Depth; }
        }

        public DataTable GetSchemaTable()
        {
            return _Reader.GetSchemaTable();
        }

        public bool IsClosed
        {
            get { return _Reader.IsClosed;  }
        }

        public bool NextResult()
        {
            return _Reader.NextResult();
        }

        public bool Read()
        {
            return _Reader.Read();
        }

        public int RecordsAffected
        {
            get { return _Reader.RecordsAffected; }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            _Reader.Dispose();
        }

        #endregion
    }
}
