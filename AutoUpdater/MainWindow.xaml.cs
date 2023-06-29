using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Diagnostics;
using Microsoft.WindowsAPICodePack.Dialogs;
using IniParser;
using IniParser.Model;

namespace AutoUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> list_all_version = new List<string>();
        List<string> list_all_path = new List<string>();
        List<int[]> all_version_level = new List<int[]>();

        List<string> list_server_all_version = new List<string>();
        List<string> list_server_all_path = new List<string>();

        List<string> list_local_all_version = new List<string>();
        List<string> list_local_all_path = new List<string>();

        string[] allPaths;

        string path_exe = "";
        string path_now = "";
        static string CurrentDirectory = Directory.GetCurrentDirectory();
        string ini_path = @"D:\PD\Instrument.ini";
        string auto_update_path = "";

        Functions fnc;



        List<string> all_version_for_show = new List<string>();
        Dictionary<string, string> dic_local_version = new Dictionary<string, string>();
        Dictionary<string, string> dic_server_version = new Dictionary<string, string>();

        bool _isAutoUpdate = false;

        public MainWindow()
        {
            InitializeComponent();

            ini_path = File.Exists(@"D:\PD\Instrument.ini") ? @"D:\PD\Instrument.ini" : Path.Combine(CurrentDirectory, "Instrument.ini");
            //ini_path = Path.Combine(CurrentDirectory, "Instrument.ini");

            fnc = new Functions(ini_path);

            try
            {
                fnc.data = fnc.parser.ReadFile(ini_path, Encoding.Unicode);
            }
            catch
            {
                MessageBox.Show($"Ini file parse failed");
            }

            #region Read ini
            bool result = false;
            if (bool.TryParse(fnc.Ini_Read("Connection", "Auto_Update"), out result))
                _isAutoUpdate = result;

            auto_update_path = fnc.Ini_Read("Connection", "Auto_Update_Path");
            //auto_update_path = data["Connection"]["Auto_Update_Path"];

            if (string.IsNullOrEmpty(auto_update_path))
            {
                auto_update_path = @"\\192.168.2.4\OptiComm\tff\Data\SW\PB";
                fnc.Ini_Write("Connection", "Auto_Update_Path", auto_update_path);
            }
            #endregion

            txt_server_path.Text = auto_update_path;
            path_exe = txt_local_path.Text;

            path_now = Directory.GetCurrentDirectory();
            txt_local_path.Text = path_now;

            #region Check local version

            Check_Local_Version();

            #endregion

            #region Check server latest version

            Check_Latest_Version_from_Server();

            #endregion

            #region 版別比較
            string target_version = "";

            string[] NV_split = txt_now_version.Text.Split('.');
            string[] LV_split = txt_new_version.Text.Split('.');

            int[] NV_detail;
            int[] LV_detail;
            if (NV_split.Length == 3 && LV_split.Length == 3)
            {
                NV_detail = new int[] { int.Parse(NV_split[0]), int.Parse(NV_split[1]), int.Parse(NV_split[2]) };
                LV_detail = new int[] { int.Parse(LV_split[0]), int.Parse(LV_split[1]), int.Parse(LV_split[2]) };

                if (LV_detail[0] > NV_detail[0])
                {
                    //Server
                    target_version = txt_new_version.Text;
                    list_all_path = list_server_all_path;
                }
                else if (LV_detail[0] == NV_detail[0])
                {
                    if (LV_detail[1] > NV_detail[1])
                    {
                        //Server
                        target_version = txt_new_version.Text;
                        list_all_path = list_server_all_path;
                    }
                    else if (LV_detail[0] == NV_detail[0])
                    {
                        if (LV_detail[2] > NV_detail[2])
                        {
                            //Server
                            target_version = txt_new_version.Text;
                            list_all_path = list_server_all_path;
                        }
                        else
                        {
                            target_version = txt_now_version.Text;
                            list_all_path = list_local_all_path;
                        }
                    }
                    else
                    {
                        target_version = txt_now_version.Text;
                        list_all_path = list_local_all_path;
                    }
                }
                else
                {
                    target_version = txt_now_version.Text;
                    list_all_path = list_local_all_path;
                }
            }
            #endregion

            ToggleBtn_Auto_Update.IsChecked = _isAutoUpdate;

            //自動更新
            if (_isAutoUpdate)
            {
                #region Get_Latest_Version
                string server_path = dic_server_version[combox_latest_version.SelectedItem.ToString()];
                fnc.Get_Files_from_Server(server_path, txt_local_path.Text);
                targetExe = fnc.Get_Latest_Version_File(list_all_path, target_version, path_now, server_path);
                #endregion

                #region Open Latest Version
                fnc.Open_Latest_Version(path_now, targetExe, target_version);
                #endregion
            }
        }

        List<int[]> dic_server_version_arrayKey = new List<int[]>();

        private void Check_Local_Version()
        {
            #region Check local version

            combox_now_version.Items.Clear();
            all_version_for_show.Clear();

            allPaths = fnc.Get_all_PD_files(path_now);

            List<List<string>> result_local = new List<List<string>>();

            result_local = fnc.Analyze_all_PD_files(allPaths);

            if (result_local.Count == 2)
            {
                list_local_all_path = new List<string>(result_local[0]);
                list_local_all_version = new List<string>(result_local[1]);
            }

            dic_local_version = fnc.Get_All_Version_for_Show(list_local_all_version, list_local_all_path);

            //foreach (string s in dic_local_version.Keys)
            //    combox_now_version.Items.Add(s);

            string[] sort_ver = dic_local_version.Keys.ToArray();
            sort_ver = Bubble_Sort(sort_ver);

            foreach (string s in sort_ver)
                combox_now_version.Items.Add(s);

            txt_now_version.Text = fnc.Get_Lastest_Version(dic_local_version.Keys.ToList());

            if (combox_now_version.Items.Contains(txt_now_version.Text)) combox_now_version.SelectedItem = txt_now_version.Text;
            #endregion

        }

        public bool CheckDirectoryExist(string dir_path)
        {
            if (Directory.Exists(dir_path))
                return true;
            else
            {
                MessageBox.Show($"Timeout\n{dir_path} is not exist");
                return false;
            }
        }

        private void Check_Latest_Version_from_Server()
        {
            #region Check server latest version

            auto_update_path = txt_server_path.Text;

            combox_latest_version.Items.Clear();
            all_version_for_show.Clear();
            dic_server_version.Clear();
            dic_server_version_arrayKey.Clear();

            var task = Task.Run(() => CheckDirectoryExist(auto_update_path));
            var result = (task.Wait(1500)) ? task.Result : false;

            if (!result)
            {
                string msg = string.Concat
                    ("Auto-Update folder is not exist",
                     "\r\n",
                     auto_update_path,
                     "\r\n\r\n",
                     "Please check ini setting",
                     "\r\n",
                      fnc.ini_path
                    );
                MessageBox.Show(msg);

                return;
            }

            string[] ss = Directory.GetDirectories(auto_update_path, "v*");
            string[] sort_ver = new string[ss.Length];

            for (int i = 0; i < ss.Length; i++)
            {
                string versionName = Path.GetFileName(ss[i].Replace("V", ""));
                dic_server_version.Add(versionName, ss[i]);

                sort_ver[i] = versionName;
            }

            //Bubble sort
            sort_ver = Bubble_Sort(sort_ver);


            //add items into combobox (in order)
            foreach (string s in sort_ver)
            {
                combox_latest_version.Items.Add(s); //Add version string to combobox
            }

            txt_new_version.Text = fnc.Get_Lastest_Version(dic_server_version.Keys.ToList());
            if (combox_latest_version.Items.Contains(txt_new_version.Text)) combox_latest_version.SelectedItem = txt_new_version.Text;

            #endregion
        }

        private string[] Bubble_Sort(string[] sort_ver)
        {
            string[] sort_result = new string[sort_ver.Length];
            sort_ver.CopyTo(sort_result, 0);

            //Bubble sort
            for (int i = 0; i < sort_result.Length; i++)
            {
                for (int j = 1; j < sort_result.Length - i; j++)
                {
                    var ver1 = new Version(sort_result[j - 1]);
                    var ver2 = new Version(sort_result[j]);
                    Console.WriteLine(sort_result[j - 1]);
                    Console.WriteLine(sort_result[j]);

                    //比較版本新/舊
                    var result = ver2.CompareTo(ver1);

                    if (result > 0)
                        Console.WriteLine("version2 is greater");
                    else if (result < 0)
                    {
                        Console.WriteLine("version1 is greater");

                        //exchange items
                        string temp_string = sort_result[j];
                        sort_result[j] = sort_result[j - 1];
                        sort_result[j - 1] = temp_string;
                    }
                    else
                        Console.WriteLine("versions are equal");
                }
            }

            return sort_result;
        }

        private void border_title_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            mRestoreForDragMove = false;
        }

        private void border_title_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (mRestoreForDragMove && this.WindowState == WindowState.Maximized)
            {

            }
            else if (mRestoreForDragMove && this.WindowState == WindowState.Normal)
            {
                //mRestoreForDragMove = false;
                mRestoreForDragMove = false;
                this.DragMove();
            }
        }

        private bool mRestoreForDragMove;
        private void border_title_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //判斷滑鼠點擊次數
            if (e.ClickCount == 2)
            {
                if ((this.ResizeMode != ResizeMode.CanResize) && (this.ResizeMode != ResizeMode.CanResizeWithGrip))
                    return;
                this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; //雙擊最大化
            }
            else
            {
                mRestoreForDragMove = this.WindowState == WindowState.Normal;
            }
        }

        private void btn_checkVersion_Click(object sender, RoutedEventArgs e)
        {
            #region Check local version
            allPaths = fnc.Get_all_PD_files(path_now);

            List<List<string>> result_local = new List<List<string>>();

            result_local = fnc.Analyze_all_PD_files(allPaths);

            if (result_local.Count == 2)
            {
                list_local_all_path = new List<string>(result_local[0]);
                list_local_all_version = new List<string>(result_local[1]);
            }

            txt_now_version.Text = fnc.Get_Lastest_Version(list_all_version);
            #endregion

            #region Check server latest version
            allPaths = fnc.Get_all_PD_files(auto_update_path);

            List<List<string>> result_server = new List<List<string>>();

            result_server = fnc.Analyze_all_PD_files(allPaths);

            if (result_server.Count == 2)
            {
                list_server_all_path = new List<string>(result_server[0]);
                list_server_all_version = new List<string>(result_server[1]);
            }

            txt_new_version.Text = fnc.Get_Lastest_Version(list_all_version);
            #endregion
        }

        private void btn_checkAllVersion_Click(object sender, RoutedEventArgs e)
        {
            #region Check local version
            allPaths = fnc.Get_all_PD_files(path_now);

            List<List<string>> result_local = new List<List<string>>();

            result_local = fnc.Analyze_all_PD_files(allPaths);

            if (result_local.Count == 2)
            {
                list_local_all_path = new List<string>(result_local[0]);
                list_local_all_version = new List<string>(result_local[1]);
            }

            txt_now_version.Text = fnc.Get_Lastest_Version(list_all_version);
            #endregion

            #region Check server latest version
            allPaths = fnc.Get_all_PD_files(auto_update_path);

            List<List<string>> result_server = new List<List<string>>();

            result_server = fnc.Analyze_all_PD_files(allPaths);

            if (result_server.Count == 2)
            {
                list_server_all_path = new List<string>(result_server[0]);
                list_server_all_version = new List<string>(result_server[1]);
            }

            txt_new_version.Text = fnc.Get_Lastest_Version(list_server_all_version);
            #endregion
        }

        string targetExe = "";
        private void btn_update_Click(object sender, RoutedEventArgs e)
        {
            #region Copy_Check_version_from_Server

            Check_Latest_Version_from_Server();

            if (dic_server_version.Count == 0)
            {
                MessageBox.Show("Not found in Server");
                return;
            }

            string server_path = dic_server_version[combox_latest_version.SelectedItem.ToString()];
            if (fnc.Get_Files_from_Server(server_path, txt_local_path.Text))
            {
                #region Check local version

                combox_now_version.Items.Clear();
                all_version_for_show.Clear();

                allPaths = fnc.Get_all_PD_files(path_now);

                List<List<string>> result_local = new List<List<string>>();

                result_local = fnc.Analyze_all_PD_files(allPaths);

                if (result_local.Count == 2)
                {
                    list_local_all_path = new List<string>(result_local[0]);
                    list_local_all_version = new List<string>(result_local[1]);
                }

                dic_local_version = fnc.Get_All_Version_for_Show(list_local_all_version, list_local_all_path);
                foreach (string s in dic_local_version.Keys)
                    combox_now_version.Items.Add(s);

                txt_now_version.Text = fnc.Get_Lastest_Version(dic_local_version.Keys.ToList());

                if (combox_now_version.Items.Contains(txt_now_version.Text)) combox_now_version.SelectedItem = txt_now_version.Text;
                #endregion

                auto_update_path = txt_server_path.Text;
                fnc.Ini_Write("Connection", "Auto_Update_Path", auto_update_path);

                MessageBox.Show("Updated");
            }

            #endregion
        }

        private void btn_Open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //System.Diagnostics.Process.Start(dic_local_version[combox_now_version.SelectedItem.ToString()]);

                string str = fnc.Ini_Read("Connection", "SkipSelectStation");
                bool SkipSelectStation = bool.TryParse(str, out bool rst) ? rst : false;

                if (SkipSelectStation)  //跳過選擇站台視窗
                {
                    if (File.Exists(dic_local_version[combox_now_version.SelectedItem.ToString()]))
                    {
                        //開啟北極熊
                        Process.Start(dic_local_version[combox_now_version.SelectedItem.ToString()]);
                        this.Close();
                    }
                }
                else
                {
                    if (File.Exists(Path.Combine(CurrentDirectory, "Window_Select_Station.exe")))
                    {
                        string para = dic_local_version[combox_now_version.SelectedItem.ToString()];
                        Process.Start(Path.Combine(CurrentDirectory, "Window_Select_Station.exe"), para);
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show($"路徑不存在:{Path.Combine(CurrentDirectory, "Window_Select_Station.exe")}");

                        if (File.Exists(dic_local_version[combox_now_version.SelectedItem.ToString()]))
                        {
                            //開啟北極熊
                            Process.Start(dic_local_version[combox_now_version.SelectedItem.ToString()]);
                            this.Close();
                        }
                    }
                }
            }
            catch { }
        }

        private void Open_Latest_Version()
        {
            string new_version = txt_new_version.Text;
            string updateFile = "";

            //Check which path contain the latest version
            foreach (string sss in list_all_path)
            {
                if (sss.Contains(new_version))
                {
                    updateFile = updateFile + sss + "\r";

                    string targetFileName = Path.GetFileName(sss);

                    string s = Path.GetExtension(sss);
                    if (s == ".exe")
                        targetExe = targetFileName;

                    try
                    {
                        File.Copy(sss, Path.Combine(path_now, targetFileName), true);
                    }
                    catch (Exception e)
                    {
                        int errorCode = e.HResult;
                        MessageBox.Show("Error Code : " + errorCode);
                    }
                }
            }

            string targetPath = Path.Combine(path_now, targetExe);
            if (File.Exists(targetPath))
            {
                try
                {
                    //開啟新版程式
                    Process.Start(targetPath);

                    fnc.Ini_Write("Connection", "Latest_Version", new_version);

                    //關閉目前程式
                    this.Close();
                    App.Current.Shutdown();
                    Process main = Process.GetCurrentProcess();
                    main.Kill();
                }
                catch
                {
                    MessageBox.Show("檔案已被開啟");
                }
            }
        }

        private void btn_min_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            System.Environment.Exit(System.Environment.ExitCode);
        }

        private void ToggleBtn_Auto_Update_Click(object sender, RoutedEventArgs e)
        {
            fnc.Ini_Write("Connection", "Auto_Update", ToggleBtn_Auto_Update.IsChecked.ToString());
        }

        private void btn_open_server_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new CommonOpenFileDialog();
                dlg.Title = "Select Server Path";
                dlg.IsFolderPicker = true;
                string str_server_path = txt_server_path.Text;
                var task = Task.Run(() => CheckDirectoryExist(str_server_path));
                var result = (task.Wait(1500)) ? task.Result : false;

                string server_path = result ? txt_server_path.Text : Directory.GetCurrentDirectory();
                dlg.InitialDirectory = server_path;

                dlg.DefaultDirectory = server_path;
                dlg.AddToMostRecentlyUsedList = false;
                dlg.AllowNonFileSystemItems = false;
                dlg.EnsureFileExists = true;
                dlg.EnsurePathExists = true;
                dlg.EnsureReadOnly = false;
                dlg.EnsureValidNames = true;
                dlg.Multiselect = false;
                dlg.ShowPlacesList = true;

                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    var folder = dlg.FileName;

                    if (!string.IsNullOrEmpty(folder))
                    {
                        auto_update_path = folder;
                        fnc.Ini_Write("Connection", "Auto_Update_Path", auto_update_path);
                        txt_server_path.Text = auto_update_path;
                    }
                }
            }
            catch { }
        }

        private void btn_open_local_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(txt_local_path.Text);
            }
            catch { }
        }

        private void combox_latest_version_DropDownOpened(object sender, EventArgs e)
        {
            Check_Latest_Version_from_Server();

            //combox_latest_version.Items.Clear();
            //all_version_for_show.Clear();
            //dic_server_version.Clear();
            //string[] ss = Directory.GetDirectories(auto_update_path, "v*");
            //for (int i = 0; i < ss.Length; i++)
            //{
            //    string versionName = Path.GetFileName(ss[i].Replace("V", ""));
            //    dic_server_version.Add(versionName, ss[i]);
            //    combox_latest_version.Items.Add(versionName);
            //}


            //txt_new_version.Text = fnc.Get_Lastest_Version(dic_server_version.Keys.ToList());
            //if (combox_latest_version.Items.Contains(txt_new_version.Text)) combox_latest_version.SelectedItem = txt_new_version.Text;
        }

        private void combox_now_version_DropDownOpened(object sender, EventArgs e)
        {
            Check_Local_Version();
            //#region Check local version

            //combox_now_version.Items.Clear();
            //all_version_for_show.Clear();

            //allPaths = fnc.Get_all_PD_files(path_now);

            //List<List<string>> result_local = new List<List<string>>();

            //result_local = fnc.Analyze_all_PD_files(allPaths);

            //if (result_local.Count == 2)
            //{
            //    list_local_all_path = new List<string>(result_local[0]);
            //    list_local_all_version = new List<string>(result_local[1]);
            //}

            //dic_local_version = fnc.Get_All_Version_for_Show(list_local_all_version, list_local_all_path);
            //foreach (string s in dic_local_version.Keys)
            //    combox_now_version.Items.Add(s);

            //txt_now_version.Text = fnc.Get_Lastest_Version(dic_local_version.Keys.ToList());

            //if (combox_now_version.Items.Contains(txt_now_version.Text)) combox_now_version.SelectedItem = txt_now_version.Text;
            //#endregion
        }
    }
}
