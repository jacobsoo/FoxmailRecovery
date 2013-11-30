using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace Foxmail_Password_Recovery
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args){
            string[] szInputFile = new string[1];
            szInputFile[0] = "";
            string szOutputFile = "";
            Boolean bExitFlag = false;

            if( args.Length==0 ){
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new frmMain());
            }else{
                if( args.Length != 4 || (args[0] == "-f" && args[2] != "-o") || (args[0] == "-o" && args[2] != "-f") ){
                    const string message = "Error in format, Enter as in format below:\n" + "Eg. FoxmailRecovery.exe -f [filename1];[filename2]...[filenameN] -o [outputfilepath]\n" + "Eg. FoxmailRecovery.exe -o [outputfilepath] -f [filename1];[filename2]...[filenameN]";
                    MessageBox.Show(message, "Error in format", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }else{
                    Boolean bLoop = true;
                    string[] szTemp = new string[1];
                    szTemp[0] = null;

                    for( int i = 0; i < 4; i++ ){
                        if( args[i].Equals("-f") && !args[i + 1].Equals("-o") ){
                            szInputFile = args[i + 1].Split(';');

                            foreach( string input in szInputFile ){
                                if( File.Exists(input) ){
                                    if( bLoop ){
                                        szTemp[0] = input;
                                        bLoop = false;
                                    }else{
                                        string[] szBuffer = new string[szTemp.Length + 1];
                                        Array.Copy(szTemp, 0, szBuffer, 0, szTemp.Length);
                                        szBuffer[szTemp.Length] = input;
                                        szTemp = null;
                                        szTemp = szBuffer;
                                        szBuffer = null;
                                    }
                                }
                            }
                            szInputFile = szTemp;
                            i++;
                        }else if( args[i].Equals("-o") && !args[i + 1].Equals("-f") ){
                            //string directory;
                            if( File.Exists(args[i + 1]) ){
                                var result = MessageBox.Show("File exist!, Do you want to overwrite?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                                if (result == DialogResult.Yes){
                                    szOutputFile = args[i + 1];
                                }else{
                                    szOutputFile = "1";
                                }
                            }else{
                                if( args[i + 1].EndsWith(".csv") ){
                                    szOutputFile = String.Format(@"{0}\{1}", Application.StartupPath, args[i + 1]);
                                }else{
                                    szOutputFile = "2";
                                }
                            }
                        }
                    }
                    if( szInputFile[0] == "" ){
                        MessageBox.Show("Invalid input(s)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        bExitFlag = true;
                    }else{
                        if( szOutputFile == "" ){
                            MessageBox.Show("Invalid output", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            bExitFlag = true;
                        }else{
                            if( szOutputFile == "1" ){
                                MessageBox.Show("Please enter a different output file name", "Renter", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                bExitFlag = true;
                            }else if( szOutputFile == "2" ){
                                MessageBox.Show("Invalid output file format", "File format", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                bExitFlag = true;
                            }
                        }
                    }
                    if( !bExitFlag ){
                        List<SharedFunctions.CsvRow> csvContent = new List<SharedFunctions.CsvRow>();
                        foreach( string word in szInputFile ){
                            // Read the file into <bits>
                            var fs = new FileStream(@word, FileMode.Open);
                            var len = (int)fs.Length;
                            var bits = new byte[len];
                            bool accfound = false;
                            string tempstring = "";
                            int ver = 0;

                            SharedFunctions.UserInfo test = new SharedFunctions.UserInfo();
                            List<SharedFunctions.UserInfo> userInfo = new List<SharedFunctions.UserInfo>();

                            fs.Read(bits, 0, len);

                            if( bits[0]==0xD0 ){
                                ver = 0;
                            }else{
                                ver = 1;
                            }

                            // Extract readable character from file
                            for (int jx = 0; jx < len; ++jx){
                                // If it's within range of ascii alphanumerics
                                if (bits[jx] > 0x20 && bits[jx] < 0x7f && bits[jx] != 0x3d){
                                    // Form word from each character for checking 
                                    tempstring += (char)bits[jx];

                                    //Loop to extract data if the formed word is "Account" or "POP3Account"
                                    if( tempstring.Equals("Account") || tempstring.Equals("POP3Account") ){
                                        int index = jx + 9;
                                        if( ver == 0 ){
                                            index = jx + 2;
                                        }
                                        while (bits[index] > 0x20 && bits[index] < 0x7f){
                                            test.acc += (char)bits[index];
                                            index++;
                                        }
                                        accfound = true;
                                        jx = index;
                                    }
                                    //Loop to extract data if the formed word is "Password" or "POP3Password"
                                    else if( accfound && (tempstring.Equals("Password") || tempstring.Equals("POP3Password")) ){
                                        int index = jx + 9;
                                        if (ver == 0){
                                            index = jx + 2;
                                        }
                                        string pw = "";
                                        while (bits[index] > 0x20 && bits[index] < 0x7f){
                                            pw += (char)bits[index];
                                            index++;
                                        }
                                        if( pw != "" ){
                                            test.password = SharedFunctions.decodePW(ver, pw);
                                        }else{
                                            test.password = "-";
                                        }

                                        bool duplicate = false;

                                        // Check for duplicate entry
                                        foreach (SharedFunctions.UserInfo user in userInfo){
                                            if (user.acc.Equals(test.acc) && user.password.Equals(test.password)){
                                                duplicate = true;
                                                break;
                                            }
                                        }
                                        if (!duplicate){
                                            userInfo.Add(test);
                                            duplicate = false;
                                        }
                                        test = null;
                                        test = new SharedFunctions.UserInfo();
                                        accfound = false;
                                        jx = index;
                                    }
                                }else{
                                    tempstring = "";
                                }
                            }

                            // Add into csv object from output file
                            foreach (SharedFunctions.UserInfo user in userInfo){
                                SharedFunctions.CsvRow row = new SharedFunctions.CsvRow();
                                row.Add(user.acc);
                                row.Add(user.password);
                                row.Add(word);
                                csvContent.Add(row);
                            }
                            fs.Close();
                        }
                        // Write file into csv file
                        using( SharedFunctions.CsvFileWriter writer = new SharedFunctions.CsvFileWriter(szOutputFile) ){
                            foreach( SharedFunctions.CsvRow r in csvContent ){
                                writer.WriteRow(r);
                            }
                        }
                    }
                }
            }
        }
    }
}