using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foxmail_Password_Recovery
{
    class SharedFunctions
    {
        //Method required for writing csv files
        public class CsvRow : List<string>
        {
            public string LineText { get; set; }
        }

        //Write CSV file
        public class CsvFileWriter : System.IO.StreamWriter
        {
            public CsvFileWriter(Stream stream)
                : base(stream)
            {
            }

            public CsvFileWriter(string filename)
                : base(filename)
            {
            }

            /// <summary>
            /// Writes a single row to a CSV file.
            /// </summary>
            /// <param name="row">The row to be written</param>
            public void WriteRow(CsvRow row)
            {
                StringBuilder builder = new StringBuilder();
                bool firstColumn = true;
                foreach (string value in row)
                {
                    // Add separator if this isn't the first value
                    if (!firstColumn)
                        builder.Append(',');
                    // Implement special handling for values that contain comma or quote
                    // Enclose in quotes and double up any double quotes
                    if (value.IndexOfAny(new char[] { '"', ',' }) != -1)
                        builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                    else
                        builder.Append(value);
                    firstColumn = false;
                }
                row.LineText = builder.ToString();
                WriteLine(row.LineText);
            }
        }

        //Object to contain user info
        public class UserInfo
        {
            public string acc;
            public string password;

            public UserInfo()
            {
                acc = "";
                password = "";
            }

        }

        // Foxmail password decoder
        public static String decodePW(int v, String pHash){
            String decodedPW = "";

            int[] a = { '~', 'd', 'r', 'a', 'G', 'o', 'n', '~' };
            int[] v7a = { '~', 'F', '@', '7', '%', 'm', '$', '~' };
            int fc0 = Convert.ToInt32("5A", 16);


            if (v == 1)
            {
                a = null;
                a = v7a;
                v7a = null;
                fc0 = Convert.ToInt32("71", 16);
            }


            int size = pHash.Length / 2;
            int index = 0;
            int[] b = new int[size];
            for (int i = 0; i < size; i++)
            {
                b[i] = Convert.ToInt32(pHash.Substring(index, 2), 16);
                index = index + 2;
            }

            int[] c = new int[b.Length];

            c[0] = b[0] ^ fc0;
            Array.Copy(b, 1, c, 1, b.Length - 1);

            while (b.Length > a.Length)
            {
                int[] newA = new int[a.Length * 2];
                Array.Copy(a, 0, newA, 0, a.Length);
                Array.Copy(a, 0, newA, a.Length, a.Length);
                a = null;
                a = newA;
                newA = null;

            }

            int[] d = new int[b.Length];

            for (int i = 1; i < b.Length; i++)
            {
                d[i - 1] = b[i] ^ a[i - 1];

            }

            int[] e = new int[d.Length];

            for (int i = 0; i < d.Length - 1; i++)
            {
                if (d[i] - c[i] < 0)
                {
                    e[i] = d[i] + 255 - c[i];

                }

                else
                {
                    e[i] = d[i] - c[i];
                }

                decodedPW += (char)e[i];
            }


            return decodedPW;
        }
    }
}
