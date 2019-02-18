using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DBF_to_MySQL__CSV_and_XML
{
    public class ParseDBF
    {
        BinaryReader recReader;
        StreamWriter fsWrite;
        //BinaryWriter bw;
        string number;
        string year;
        string month;
        string day;
        long lDate;
        long lTime;
        int fieldIndex;
        string fields_as_string_for_create;
        string fields_as_string_for_insert;
        MainWindow OwnerWin;
        DataRow row;
        bool _DataBaseDone = false;
        public bool DataBaseDone { get { if (!_DataBaseDone || !br.BaseStream.CanRead) { System.Windows.MessageBox.Show(g.dict["MessCantOpenDBF"].ToString(), g.dict["MessCantOpenDBF_Title"].ToString()); } return _DataBaseDone; } }
        public long Length_File { get { if (!DataBaseDone) { return -1; } return ((FileStream)br.BaseStream).Length; } }
        public int CountRows { get { if (!DataBaseDone) { return -1; } return header.numRecords; } }
        public bool CanReadNextRow { get { if (!DataBaseDone) { return false; } return cur_num_row < header.numRecords; } }
        BinaryReader br = null;
        Stream my_stream;
        byte[] buffer;
        GCHandle handle;
        DBFHeader header;
        ArrayList fields;
        DataTable dt;
        int cur_num_row;
        List<string[]> data_list;
        public void Close()
        {
            br.Close();
            br = null;
            buffer = null;
            handle = new GCHandle();
            header = new DBFHeader();
            fields.Clear();
            fields = null;
            dt = null;
            cur_num_row = 0;
            recReader = null;
        }
        // This is the file header for a DBF. We do this special layout with everything
        // packed so we can read straight from disk into the structure to populate it
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        private struct DBFHeader
        {
            public byte version;
            public byte updateYear;
            public byte updateMonth;
            public byte updateDay;
            public Int32 numRecords;
            public Int16 headerLen;
            public Int16 recordLen;
            public Int16 reserved1;
            public byte incompleteTrans;
            public byte encryptionFlag;
            public Int32 reserved2;
            public Int64 reserved3;
            public byte MDX;
            public byte language;
            public Int16 reserved4;
        }
        Encoding CurrEnc;
        private BinaryWriter bw;
        // This is the field descriptor structure. There will be one of these for each column in the table.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        private struct FieldDescriptor
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
            public string fieldName;
            public char fieldType;
            public Int32 address;
            public byte fieldLen;
            public byte count;
            public Int16 reserved1;
            public byte workArea;
            public Int16 reserved2;
            public byte flag;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
            public byte[] reserved3;
            public byte indexFlag;
        }
        public ParseDBF(string dbfFile, Encoding CurrEnc, MainWindow OwnerWin)
        {
            if ((!File.Exists(dbfFile)))
            {
                _DataBaseDone = false;
                var v = this.DataBaseDone;
                return;
            }
            this.OwnerWin = OwnerWin;
            cur_num_row = 0;
            this.CurrEnc = CurrEnc;
            dt = new DataTable();
            br = new BinaryReader(File.OpenRead(dbfFile));
            buffer = br.ReadBytes(Marshal.SizeOf(typeof(DBFHeader)));
            handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            header = (DBFHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBFHeader));
            handle.Free();
            fields = new ArrayList();
            while ((13 != br.PeekChar()))
            {
                buffer = br.ReadBytes(Marshal.SizeOf(typeof(FieldDescriptor)));
                handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                fields.Add((FieldDescriptor)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(FieldDescriptor)));
                handle.Free();
            }
            ((FileStream)br.BaseStream).Seek(header.headerLen + 1, SeekOrigin.Begin);
            buffer = br.ReadBytes(header.recordLen);
            recReader = new BinaryReader(new MemoryStream(buffer));
            DataColumn col = null;
            foreach (FieldDescriptor field in fields)
            {
                //number = CurrEnc.GetString(recReader.ReadBytes(field.fieldLen));
                col = new DataColumn(field.fieldName, typeof(string));
                dt.Columns.Add(col);
            }
            fields_as_string_for_create = NamesFieldsSQL(true);
            fields_as_string_for_insert = NamesFieldsSQL(false);
            _DataBaseDone = true;
        }
        /// <summary>
        /// Не более 55
        /// </summary>
        /// <param name="limit_row"></param>
        /// <returns></returns>
        public DataTable GetRandomRowsAsDataTable(int limit_row, bool del_row_inc = true)
        {
            if (!this.DataBaseDone)
            {
                return new DataTable();
            }
            if (limit_row <= 0)
            {
                System.Windows.MessageBox.Show("Укажите лимит больше 0", "Не корректный лимит");
                return this.StructureDB;
            }
            if (header.numRecords <= 0)
            {
                System.Windows.MessageBox.Show(g.dict["MessEmptyDataBaseDBF"].ToString(), g.dict["MessEmptyDataBaseDBFTitle"].ToString());
                return this.StructureDB;
            }
            if (header.numRecords < limit_row)
                limit_row = header.numRecords;
            limit_row = Math.Min(55, limit_row);
            dt.Rows.Clear();
            long old_file_position = ((FileStream)br.BaseStream).Position;
            ((FileStream)br.BaseStream).Seek(header.headerLen, SeekOrigin.Begin);
            Random rnd = new Random();
            rnd.Next(0, header.numRecords - 1);
            for (int counter = 0; counter <= limit_row; counter++)
            {
                long random_position_row = (rnd.Next(0, header.numRecords - 1) * header.recordLen);
                br.BaseStream.Position = (header.headerLen + random_position_row);
                buffer = br.ReadBytes(header.recordLen);
                recReader = new BinaryReader(new MemoryStream(buffer));
                if (recReader.ReadChar() == '*')
                {
                    if (!del_row_inc)
                        continue;
                }
                fieldIndex = 0;
                row = dt.NewRow();
                foreach (FieldDescriptor field in fields)
                {
                    switch (field.fieldType)
                    {
                        case 'N':  // Number
                            number = CurrEnc.GetString(recReader.ReadBytes(field.fieldLen));
							if (IsNumber(number))
							{
                                row[fieldIndex] = number;
							}
							else
							{
                                row[fieldIndex] = "0";
							}
                            break;
                        case 'C': // String
                            row[fieldIndex] = CurrEnc.GetString(recReader.ReadBytes(field.fieldLen));//row[fieldIndex] = CurrEnc.GetString(recReader.ReadBytes(field.fieldLen));
                            break;

                        case 'D': // Date (YYYYMMDD)
                            year = CurrEnc.GetString(recReader.ReadBytes(4));
                            month = CurrEnc.GetString(recReader.ReadBytes(2));
                            day = CurrEnc.GetString(recReader.ReadBytes(2));
                            row[fieldIndex] = System.DBNull.Value;
                            try
                            {
                                if (IsNumber(year) && IsNumber(month) && IsNumber(day))
                                {
                                    if ((Int32.Parse(year) > 1900))
                                    {
                                        row[fieldIndex] = new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day)).ToString();
                                    }
                                }
                            }
                            catch
                            { }

                            break;

                        case 'T': // Timestamp, 8 bytes - two integers, first for date, second for time
                            // Date is the number of days since 01/01/4713 BC (Julian Days)
                            // Time is hours * 3600000L + minutes * 60000L + Seconds * 1000L (Milliseconds since midnight)
                            lDate = recReader.ReadInt32();
                            lTime = recReader.ReadInt32() * 10000L;
                            row[fieldIndex] = JulianToDateTime(lDate).AddTicks(lTime).ToString();
                            break;

                        case 'L': // Boolean (Y/N)
                            if ('Y' == recReader.ReadByte())
                            {
                                row[fieldIndex] = "true";
                            }
                            else
                            {
                                row[fieldIndex] = "false";
                            }

                            break;

                        case 'F':
                            number = CurrEnc.GetString(recReader.ReadBytes(field.fieldLen));
                            if (IsNumber(number))
                            {
                                row[fieldIndex] = number;
                            }
                            else
                            {
                                row[fieldIndex] = "0.0";
                            }
                            break;
                    }
                    fieldIndex++;
                }
                recReader.Close();
                dt.Rows.Add(row);
            }
            ((FileStream)br.BaseStream).Seek(old_file_position, SeekOrigin.Begin);
            return dt;
        }
        public void SaveAs(string FileOutputName, string type_file, bool inc_del, Action<int, string> UpdateStatus)
        {
            string table_name = "table_" + new System.Text.RegularExpressions.Regex(@"\W").Replace(System.IO.Path.GetFileName(FileOutputName), "_");
            type_file = type_file.ToLower();
            if (type_file != "sql" && type_file != "csv" && type_file != "xml")
            {
                System.Windows.MessageBox.Show(OwnerWin, "Не известный тип сохраняемого файла", "Ошибка");
                return;
            }
            my_stream = new FileStream(FileOutputName, FileMode.Create, FileAccess.Write);
            if (type_file == "csv")
                fsWrite = new StreamWriter(my_stream, Encoding.UTF8, 1024 * 64);
            bw = new BinaryWriter(my_stream);
            switch (type_file)
            {
                case "sql":
                    bw.Write(g.StringToByte(
                    "-- DBF - conversion  into XML, SQL, CSV, ..." + "\n" +
                    "-- " + g.preficsBildProgramm + "\n" +
                    "-- https://sourceforge.net/projects/dbf-to-mysql-csv-xml/" + "\n" +
                    "-- Datetime create dump: " + DateTime.Now.ToString("dd MM yyyy | HH:mm:ss") + "\n" +
                    "\n" +
                    "SET SQL_MODE=\"NO_AUTO_VALUE_ON_ZERO\";" + "\n" +
                    "SET time_zone=\"+00:00\";" + "\n" +
                    "\n" + "\n" +
                    "/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;" + "\n" +
                    "/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;" + "\n" +
                    "/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;" + "\n" +
                    "/*!40101 SET NAMES utf8 */;" + "\n" +
                    "" + "\n" +
                    "-- --------------------------------------------------------" + "\n" +
                    "-- " + "\n" +
                    "-- `" + table_name + "`\n" +
                    "-- " + "\n" +
                    "" + "\n" +
                    "CREATE TABLE IF NOT EXISTS `" + table_name + "` (" + "\n" +
                    fields_as_string_for_create + "\n" +
                    ") ENGINE=MyISAM DEFAULT CHARSET=utf8;" + "\n" + "\n" +
                    "-- " + "\n" +
                    "-- Dump data table `" + table_name + "` (" + header.numRecords + " Records)" + "\n" +
                    "-- " + "\n" + "\n"));
                    break;
                case "csv":
                    fsWrite.WriteLine(NamesFieldsSQL(false, ";", ""));
                    break;
                case "xml":
                    bw.Write(g.StringToByte("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n\t<" + new System.Text.RegularExpressions.Regex(@"\W").Replace(System.IO.Path.GetFileNameWithoutExtension(FileOutputName), "_") + ">\n"));
                    break;
            }
            data_list = new List<string[]>(header.numRecords);
            //my_stream.Close();
            ((FileStream)br.BaseStream).Seek(header.headerLen, SeekOrigin.Begin);
            string[] s_row;
            int data_list_Count = 0;
            int del_rows_count = 0;
            byte[] readed_data_tmp;
            for (int counter = 0; counter <= header.numRecords-1; counter++)
            {
                buffer = br.ReadBytes(header.recordLen);
                if (buffer.Length == 0)
                {
                    FlushData(type_file,table_name);
                    fsWrite.Close();
                    return;
                }
                    
                recReader = new BinaryReader(new MemoryStream(buffer));
                if (recReader.ReadChar() == '*')
                {
                    del_rows_count++;
                    if (!inc_del)
                        continue;
                }
                fieldIndex = 0;
                s_row = new string[fields.Count];
                foreach (FieldDescriptor field in fields)
                {
                    readed_data_tmp = recReader.ReadBytes(field.fieldLen);
                    switch (field.fieldType)
                    {
                        case 'N':
                            number = CurrEnc.GetString(readed_data_tmp);
                            if (IsNumber(number))
                            {
                                s_row[fieldIndex] = number;
                            }
                            else
                            {
                                s_row[fieldIndex] = "0";
                            }
                            break;
                        case 'C':
                            if (type_file == "sql")
                                s_row[fieldIndex] = "'" + g.MySQLEscape(CurrEnc.GetString(readed_data_tmp)) + "'";
                            else if (type_file == "csv")
                                s_row[fieldIndex] = g.CSVEscape(CurrEnc.GetString(readed_data_tmp));
                            else if (type_file == "xml")
                                s_row[fieldIndex] = CurrEnc.GetString(readed_data_tmp).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
                            else
                                throw new NotImplementedException();
                            break;
                        case 'D': // Date (YYYYMMDD)
                            year = CurrEnc.GetString(g.SubArray(readed_data_tmp, 0, 4));
                            month = CurrEnc.GetString(g.SubArray(readed_data_tmp, 4, 2));
                            day = CurrEnc.GetString(g.SubArray(readed_data_tmp, 6, 2));
                            s_row[fieldIndex] = "''";
                            try
                            {
                                if (IsNumber(year) && IsNumber(month) && IsNumber(day))
                                {
                                    if ((Int32.Parse(year) > 1900))
                                    {
                                        s_row[fieldIndex] = "'" + new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day)).ToString() + "'";
                                    }
                                }
                            }
                            catch
                            { }
                            break;
                        case 'T': // Timestamp, 8 bytes - two integers, first for date, second for time
                            // Date is the number of days since 01/01/4713 BC (Julian Days)
                            // Time is hours * 3600000L + minutes * 60000L + Seconds * 1000L (Milliseconds since midnight)
                            lDate = BitConverter.ToInt32(readed_data_tmp, 0);
                            lTime = BitConverter.ToInt32(readed_data_tmp, 4);
                            s_row[fieldIndex] = JulianToDateTime(lDate).AddTicks(lTime).ToString();
                            break;
                        case 'L': // Boolean (Y/N)
                            if (CurrEnc.GetString(readed_data_tmp).ToUpper() == "T")
                            {
                                s_row[fieldIndex] = "1";
                            }
                            else
                            {
                                s_row[fieldIndex] = "0";
                            }

                            break;
                        case 'F':
                            number = CurrEnc.GetString(readed_data_tmp);
                            if (IsNumber(number))
                            {
                                s_row[fieldIndex] = number;
                            }
                            else
                            {
                                s_row[fieldIndex] = "0.0";
                            }
                            break;
                    }
                    fieldIndex++;
                }
                recReader.Close();
                data_list_Count++;
                data_list.Add(s_row);
                if (data_list_Count > 5000)
                {
                    data_list_Count = 0;
                    FlushData(type_file, table_name);
                    int curr_count_data_lines = data_list.Count;
                    OwnerWin.Dispatcher.Invoke(UpdateStatus, new object[] { counter, string.Format(g.dict["MessParseCountRows"].ToString() + (del_rows_count > 0 ? g.dict["MessParseCountDeletedRows"].ToString() : "") + " " + g.dict["MessParseCounSizeOtputFile"].ToString(), counter, g.SizeFileAsString(bw.BaseStream.Length), del_rows_count) });
                }
            }
            FlushData(type_file, table_name);
            switch (type_file)
            {
                case "sql":
                    bw.Write(g.StringToByte("\n\n" + "/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;" + "\n" +
                    "/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;" + "\n" +
                    "/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;"));
                    break;
                case "xml":
                    bw.Write(g.StringToByte("\t</" + new System.Text.RegularExpressions.Regex(@"\W").Replace(System.IO.Path.GetFileNameWithoutExtension(FileOutputName), "_") + ">"));
                    break;
            }
            OwnerWin.Dispatcher.Invoke(UpdateStatus, new object[] { 0, g.dict["MessParseDone"].ToString() });
            if (fsWrite != null)
                fsWrite.Close();
            else
                my_stream.Close();
        }

        private void FlushData(string type_file, string table_name)
        {
            if (data_list.Count == 0)
                return;
            if (type_file == "sql")
                bw.Write(g.StringToByte("INSERT INTO `" + table_name + "` (" + fields_as_string_for_insert + ") VALUES\n"));
            int data_list_count = data_list.Count;
            foreach (string[] s in data_list)
            {
                data_list_count--;
                if (type_file == "sql")
                    Write(s, ", ", "(", ")" + (data_list_count == 0 ? ";" : ","));
                else if (type_file == "csv")
                    Write(s, ";", "", "");
                else
                    WriteXML(s);
            }
            data_list.Clear();
        }

        private void WriteXML(string[] s_arr)
        {
            string result_string = "<row ";
            int field_index = -1;
            int s_arr_Length = s_arr.Length - 1;
            foreach (FieldDescriptor field in fields)
            {
                field_index++;
                result_string += field.fieldName + "=\"" + s_arr[field_index] + "\"\t";
                if (field_index == s_arr_Length)
                    result_string = result_string.TrimEnd() + "/>";
            }
            bw.Write(g.StringToByte("\t\t" + result_string + "\n"));
        }
        private string NamesFieldsSQL(bool ForCreateTable, string separator = ", ", string quote = "`")
        {
            string returned_data = "";
            foreach (FieldDescriptor field in fields)
            {
                string type_data = quote + field.fieldName + quote;
                //number = CurrEnc.GetString(recReader.ReadBytes(field.fieldLen));
                if (ForCreateTable)
                {
                    switch (field.fieldType)
                    {
                        case 'N':
                            if (field.count > 0)
                            {
                                type_data += " DECIMAL(" + field.fieldLen + "," + field.count + ")";
                            }
                            else
                            {
                                type_data += " INT(" + field.fieldLen + ")";
                            }
                            break;
                        case 'C':
                            type_data += " VARCHAR(" + field.fieldLen + ")";
                            break;
                        case 'T':
                            //col = new DataColumn(field.fieldName, typeof(string));
                            type_data += " TIME";
                            break;
                        case 'D':
                            type_data += " DATE";
                            break;
                        case 'L':
                            type_data += " BOOLEAN";
                            break;
                        case 'F':
                            type_data += " DOUBLE(" + field.fieldLen + "," + field.count + ")";
                            break;
                    }
                    returned_data += type_data + " NOT NULL,\n";
                }
                else
                    returned_data += type_data + separator;

            }
            returned_data = returned_data.Trim();
            returned_data = returned_data.Substring(0, returned_data.Length - 1);
            return returned_data;
        }
        protected void Write(string[] strings, string separator = ", ", string left_blok = "(",string right_blok = "),")
        {
            string result_string = left_blok;
            foreach(string s in strings)
                result_string += s + separator;
            result_string = result_string.Trim();
                result_string = result_string.Substring(0, result_string.Length - 1);
            result_string += right_blok;
            result_string = result_string.Trim();
            if (fsWrite != null)
                fsWrite.WriteLine(result_string);
            else
                bw.Write(g.StringToByte(result_string + "\n"));
        }
        public bool IsNumber(string numberString)
        {
            char[] numbers = numberString.ToCharArray();
            int number_count = 0;
            int point_count = 0;
            int space_count = 0;

            foreach (char number in numbers)
            {
                if ((number >= 48 && number <= 57))
                {
                    number_count += 1;
                }
                else if (number == 46)
                {
                    point_count += 1;
                }
                else if (number == 32)
                {
                    space_count += 1;
                }
                else
                {
                    return false;
                }
            }

            return (number_count > 0 && point_count < 2);
        }
        public DataTable StructureDB
        {
            get
            {
                DataTable empty_dt = new DataTable();
                if (!_DataBaseDone)
                    return empty_dt;
                foreach (DataColumn col in dt.Columns)
                {
                    empty_dt.Columns.Add(new DataColumn(col.ColumnName, col.DataType));
                }
                return empty_dt;
            }
        }
        private DateTime JulianToDateTime(long lJDN)
        {
            double p = Convert.ToDouble(lJDN);
            double s1 = p + 68569;
            double n = Math.Floor(4 * s1 / 146097);
            double s2 = s1 - Math.Floor((146097 * n + 3) / 4);
            double i = Math.Floor(4000 * (s2 + 1) / 1461001);
            double s3 = s2 - Math.Floor(1461 * i / 4) + 31;
            double q = Math.Floor(80 * s3 / 2447);
            double d = s3 - Math.Floor(2447 * q / 80);
            double s4 = Math.Floor(q / 11);
            double m = q + 2 - 12 * s4;
            double j = 100 * (n - 49) + i + s4;
            return new DateTime(Convert.ToInt32(j), Convert.ToInt32(m), Convert.ToInt32(d));
        }
    }
}
