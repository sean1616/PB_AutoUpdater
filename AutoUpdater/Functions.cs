using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using IniParser;
using IniParser.Model;

namespace AutoUpdater
{
    public class Functions
    {
        public static SetupIniIP ini = new SetupIniIP();

        public string ini_path = @"D:\PD\Instrument.ini";
        public string allfiles = "";

        List<string> list_all_version = new List<string>();
        List<string> list_all_path = new List<string>();
        List<int[]> all_version_level = new List<int[]>();

        public FileIniDataParser parser = new FileIniDataParser();
        public IniData data = new IniData();

        public Functions(string ini_path)
        {
            this.ini_path = ini_path;

            //try
            //{
            //    data = parser.ReadFile(ini_path);
            //}
            //catch { }
        }

        public string Ini_Read(string Section, string key)
        {
            string _ini_read;
            if (File.Exists(ini_path))
            {
                //_ini_read = ini.IniReadValue(Section, key, ini_path);
                _ini_read = data[Section][key];
            }
            else
                _ini_read = "";

            return _ini_read;
        }

        public void Ini_Write(string Section, string key, string value)
        {
            if (!File.Exists(ini_path))
                Directory.CreateDirectory(System.IO.Directory.GetParent(ini_path).ToString());  //建立資料夾

            data[Section][key] = value;
            parser.WriteFile(ini_path, data);

            //ini.IniWriteValue(Section, key, value, ini_path);  //創建ini file並寫入基本設定
        }

        public Dictionary<string, string> Get_All_Version_for_Show(List<string> allVersion, List<string> allVersion_path)
        {
            Dictionary<string, string> resultList = new Dictionary<string, string>();

            for (int i = 0; i < allVersion.Count; i++)
            {
                string s = allVersion[i];
                if (s.Contains(".exe") && !s.Contains(".config"))
                {
                    resultList.Add(s.Replace(".exe", ""), allVersion_path[i]);
                }
            }            

            return resultList;
        }

        public string Get_Lastest_Version(List<string> allVersion)
        {
            if (allVersion.Count == 0) return "No version data";

            string new_version = "";

            #region Judge new version
            int Level_1 = 0;
            int Level_2 = 0;
            int Level_3 = 0;

            all_version_level.Clear();
            foreach (string ss in allVersion)
            {                
                int[] version_level = new int[3];
                string[] version_detail = ss.Split('.');

                int result;
                if (int.TryParse(version_detail[0], out result))
                    version_level[0] = result;

                if (int.TryParse(version_detail[1], out result))
                    version_level[1] = result;

                if (int.TryParse(version_detail[2], out result))
                    version_level[2] = result;

                //Level 1:
                if (version_level[0] > Level_1)
                {
                    //Level 1:
                    Level_1 = version_level[0];

                    //Level 2:
                    Level_2 = version_level[1];

                    //Level 3:
                    Level_3 = version_level[2];
                }
                else if (version_level[1] > Level_2)
                {
                    if (version_level[0] >= Level_1)
                    {
                        //Level 2:
                        Level_2 = version_level[1];

                        //Level 3:
                        Level_3 = version_level[2];
                    }
                }
                else if (version_level[2] > Level_3)
                {
                    if (version_level[1] >= Level_2)
                    {
                        if (version_level[0] >= Level_1)
                        {
                            //Level 3:
                            Level_3 = version_level[2];
                        }
                    }
                }

                new_version = string.Concat(Level_1, ".", Level_2, ".", Level_3);
                //txt_new_version.Text = new_version;

                all_version_level.Add(version_level);
            }
            #endregion

            return new_version;
        }

        public string[] Get_all_PD_files(string pathDirectory)
        {
            string[] filePaths = new string[] { };

            if (string.IsNullOrEmpty(pathDirectory)) return filePaths;

            filePaths = Directory.GetFiles(pathDirectory);

            List<string> listFiles = filePaths.ToList();

            listFiles = listFiles.Where(x => x.Contains(".exe") && !x.Contains(".config")).ToList();

            filePaths = listFiles.ToArray();

            return filePaths;
        }

        public List<List<string>> Analyze_all_PD_files(string[] all_filePaths)
        {
            List<List<string>> result = new List<List<string>>();

            list_all_version.Clear();
            list_all_path.Clear();

            foreach (string s in all_filePaths)
            {
                string exeName = Path.GetFileName(s);
                string exeName_without_extension = Path.GetFileNameWithoutExtension(s);

                if (!string.IsNullOrEmpty(exeName))
                {
                    string[] exeName_detail = exeName.Split('v');
                    string[] exeName_detail_without_extension = exeName_without_extension.Split('v');

                    if (exeName_detail.Length > 1)
                    {
                        if (exeName_detail[0] == "PD-")
                        {
                            allfiles = allfiles + "\r" + s;

                            list_all_path.Add(s);

                            if (!list_all_version.Contains(exeName_detail[1]))   //含有版別資訊的檔案
                                list_all_version.Add(exeName_detail[1]);
                        }
                    }
                }


            }

            result.Add(list_all_path);
            result.Add(list_all_version);

            return result;
        }

        public bool Get_Files_from_Server(string server_path, string local_path)
        {
            try
            {
                string[] files_on_server = Directory.GetFiles(server_path);
                foreach (string path in files_on_server)
                {
                    string des_Filename = local_path +@"\"+ Path.GetFileName(path);

                    try
                    {
                        File.Copy(path, des_Filename, true);
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show($"File [{path}] copy failed.");
                        continue;
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public string Get_Latest_Version_File(List<string> list_all_path, string latest_version, string path_now, string path_latest_ver_file)
        {
            string targetExe = "";
            //string updateFile = "";

            //Check which path contain the latest version
            foreach (string sss in list_all_path)
            {
                if (sss.Contains(latest_version))
                {
                    //updateFile = updateFile + sss + "\r";

                    string targetFileName = Path.GetFileName(sss);

                    string s = Path.GetExtension(sss);
                    if (s == ".exe")
                        targetExe = targetFileName;

                    //Copy the latest file
                    try
                    {
                        string pn = Path.Combine(path_now, targetFileName);
                        File.Copy(Path.Combine(path_latest_ver_file, targetFileName), path_now, true);
                    }
                    catch (Exception e)
                    {
                        //updateFile = "";
                        int errorCode = e.HResult;
                        //updateFile = ("Error Code : " + errorCode);
                    }
                }
            }

            return targetExe;
        }

        public string Open_Latest_Version(string path_now, string targetExe, string latest_version)
        {
            string errormsg = "";

            string targetPath = Path.Combine(path_now, targetExe);
            if (File.Exists(targetPath))
            {


                try
                {
                    //開啟新版程式
                    Process.Start(targetPath);

                    Ini_Write("Connection", "Latest_Version", latest_version);

                    //關閉目前程式
                    App.Current.Shutdown();
                    Process main = Process.GetCurrentProcess();
                    main.Kill();
                }
                catch
                {
                    errormsg = ("檔案已被開啟");
                }


            }

            return errormsg;
        }
    }

    public class SetupIniIP
    {
        public string path;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section,
        string key, string val, string filePath);
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section,
        string key, string def, StringBuilder retVal,
        int size, string filePath);

        //ini write
        public void IniWriteValue(string Section, string Key, string Value, string inipath)
        {
            WritePrivateProfileString(Section, Key, Value, inipath);
        }

        //ini read
        public string IniReadValue(string Section, string Key, string inipath)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp, 255, inipath);
            return temp.ToString();
        }
    }
}
