using CsvHelper;
using CsvHelper.Configuration;
using sqlite_Example;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace import_CSV
{
    class CSV_Import
    {
        static private SQLiteDatabase mydatabase;
        static private List<string> table_cols;
        static private int row_length;
        static private CsvReader csv;
        static private StreamReader reader;
        static private CsvConfiguration config;
        static private string table_name;
        static private bool End_Of_File;

        static public void init(string filename, string tablename, string databasename, bool skip_header, List<string> column_headers, string delim, bool emptyfail)
        {
            End_Of_File = false;
            if (tablename.Contains(' ') || tablename.Contains('\t') || tablename.Contains('\n') || tablename.Contains('\r'))
            {
                Console.WriteLine(string.Format("I refuse to deal with sql tables with white space in their names, even \"{0}\"\nSo fix that garbage", tablename));
                Environment.Exit(4);
                    
            }
            else table_name = tablename;

            mydatabase = new SQLiteDatabase(databasename);
            // Otherwise lets get it
            config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
                Delimiter = delim
            };
            try
            {
                reader = new StreamReader(filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(3);
            }
            csv = new CsvReader(reader, config);

            // Do we need to read a header?
            if (skip_header) table_cols = column_headers;
            else table_cols = read_row();

            // Regardless use the column titles expressed
            if (column_headers.Count > 0) table_cols = column_headers;

            if((null == table_cols) || (0 >= table_cols.Count))
            {
                Console.WriteLine(string.Format("I can't use an empty file named {0}", filename));
                if(emptyfail) Environment.Exit(5);
                else Environment.Exit(0);
            }

            if(table_cols.Distinct().Count() != table_cols.Count())
            {
                Console.WriteLine("I don't support duplicate column names\n");
                Environment.Exit(8);
            }

            mydatabase.ExecuteNonQuery(string.Format("drop table if exists '{0}';", tablename));
            string sql = string.Format("CREATE TABLE '{0}' ( ", tablename);
            foreach(string s in table_cols)
            {
                if ('`' == sql[sql.Length -1]) sql = sql + ", ";
                sql = sql + "`" + s + "`";
            }
            sql = sql + ");";

            mydatabase.ExecuteNonQuery(sql);

        }

        static public bool read_record()
        {
            List<string> entries = read_row();
            if (!End_Of_File) return End_Of_File;

            Dictionary<string, string> tmp = new Dictionary<string, string>();
            int i = 0;
            while (i < row_length)
            {
                tmp.Add(table_cols[i], entries[i]);
                i = i + 1;
            }

            mydatabase.Insert(table_name, tmp);
            return End_Of_File;
        }

        static public void cleanup()
        {
            mydatabase.SQLiteDatabase_Close();
        }

        static private List<string> read_row()
        {
            End_Of_File = csv.Read();
            if (!End_Of_File) return null;
            row_length = csv.Parser.Count;

            List<string> s = new List<string>();
            int i = 0;
            while (i < row_length)
            {
                string t = csv.GetField(i);
                s.Add(t);
                i = i + 1;
            }
            return s;
            
        }
    }
}
