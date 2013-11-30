using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Foxmail_Password_Recovery
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        // Object to store all the data to be written in the CSV file
        List<SharedFunctions.CsvRow> csvContent = new List<SharedFunctions.CsvRow>();

        // Required for drag and drop file
        private void frmMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                e.Effect = DragDropEffects.All;
        }

        // Handler after the drag and drop
        private void frmMain_DragDrop(object sender, DragEventArgs e)
        {
            //Clear previous print out
            txtData.Clear();

            //Get the files that have been dragged and drop
            string[] fileList = e.Data.GetData(DataFormats.FileDrop) as string[];

            foreach( string word in fileList ){
                // Read the file into <bits>
                var fs = new FileStream(@word, FileMode.Open);
                var len = (int)fs.Length;
                var bits = new byte[len];

                bool accfound = false;
                string buffer = "";
                int ver = 0;
                SharedFunctions.UserInfo userInfo = new SharedFunctions.UserInfo();
                List<SharedFunctions.UserInfo> userInfoList = new List<SharedFunctions.UserInfo>();

                String finaloutput = "------------------------------------------------------------------\r\n";
                finaloutput += "From file: " + word + " \r\n";
                finaloutput += "------------------------------------------------------------------\r\n";

                fs.Read(bits, 0, len);

                // Check if the file version
                if( bits[0]==0xD0 ){
                    // Version 6.X
                    ver = 0;
                }else{
                    // Version 7.0 and 7.1
                    ver = 1;
                }
                // Loop to filter out non alphanumeric characters. Form word from individual character
                // to see if it is the interested data
                for( int jx = 0; jx < len; ++jx ){
                    // Filter out not alphanumeric character
                    if( bits[jx] > 0x20 && bits[jx] < 0x7f && bits[jx] != 0x3d ){
                        // Concat to from word
                        buffer += (char)bits[jx];

                        // Check if the next word is going to the user account
                        if( buffer.Equals("Account") || buffer.Equals("POP3Account") ){
                            // Offset
                            int index = jx + 9;

                            // Additional offset required for version 6.5
                            if( ver==0 ){
                                index = jx + 2;
                            }
                            // Loop till the entire data is extracted 
                            // (Data is in alphanumeric character, non alphanumeric mean end of data)
                            while (bits[index] > 0x20 && bits[index] < 0x7f){
                                userInfo.acc += (char)bits[index];
                                index++;
                            }
                            // Flag to indicate account found
                            accfound = true;

                            // Shift the current "pointer" to the end index of the data
                            jx = index;
                        }
                        // If there is an user account, check for its password
                        else if( accfound && (buffer.Equals("Password") || buffer.Equals("POP3Password")) ){
                            int index = jx + 9;
                            if( ver==0 ){
                                index = jx + 2;
                            }
                            string pw = "";

                            while( bits[index] > 0x20 && bits[index] < 0x7f ){
                                pw += (char)bits[index];
                                index++;
                            }
                            if( pw!="" ){
                                userInfo.password = SharedFunctions.decodePW(ver, pw);
                            }else{
                                userInfo.password = "empty";
                            }
                            bool duplicate = false;

                            //Check for duplicate data before inserting into userInfoList
                            foreach (SharedFunctions.UserInfo user in userInfoList)
                            {
                                if (user.acc.Equals(userInfo.acc) && user.password.Equals(userInfo.password))
                                {
                                    duplicate = true;
                                    break;
                                }

                            }

                            if (!duplicate)
                            {
                                userInfoList.Add(userInfo);
                                duplicate = false;
                            }

                            userInfo = null;
                            userInfo = new SharedFunctions.UserInfo();

                            accfound = false;
                            jx = index;
                        }
                    }else{
                        buffer = "";
                    }
                }
                bool empty = true;

                // Loop to output data
                foreach (SharedFunctions.UserInfo user in userInfoList){
                    SharedFunctions.CsvRow row = new SharedFunctions.CsvRow();
                    finaloutput += "Account : " + user.acc + "\r\nPassword : " + user.password + "\r\n\r\n";
                    row.Add(user.acc);
                    row.Add(user.password);
                    row.Add(word);
                    csvContent.Add(row);
                    btnExport.Enabled = true;
                    empty = false;
                }
                if (empty){
                    finaloutput += "Empty\r\n\r\n";
                }
                txtData.AppendText(finaloutput);
                fs.Close();
            }
        }

        // Write data to csv file
        private void btnExport_Click(object sender, EventArgs e)
        {
            if (sfdSave.ShowDialog() == DialogResult.OK)
            {
                using (SharedFunctions.CsvFileWriter writer = new SharedFunctions.CsvFileWriter(sfdSave.FileName))
                {
                    foreach (SharedFunctions.CsvRow r in csvContent)
                    {
                        writer.WriteRow(r);
                    }
                }
            }
        }
    }
}