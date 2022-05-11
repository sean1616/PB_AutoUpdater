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

            fnc = new Functions(ini_path);

            #region Read ini
            bool result = false;
            if (bool.TryParse(fnc.Ini_Read("Connection", "Auto_Update"), out result))
                _isAutoUpdate = result;

            auto_update_path = fnc.Ini_Read("Connection", "Auto_Update_Path");

            if (string.IsNullOrEmpty(auto_update_path))
            {
                auto_update_path = @"\\192.168.2.3\shared\SeanWu\PB\";
                fnc.Ini_Write("Connection", "Auto_Update_Path", auto_update_path);

            }
            #endregion

            txt_server_path.Text = auto_update_path;
            //txt_path.Text = @"\\192.168.2.3\shared\SeanWu\PB\";
            path_exe = txt_local_path.Text;

            path_now = Directory.GetCurrentDirectory();
            txt_local_path.Text = path_now;
            //path_now = @"C:\Users\sean_wu\source\repos\AutoUpdater\AutoUpdater\bin\Debug";


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


            #region Check server latest version

            combox_latest_version.Items.Clear();
            all_version_for_show.Clear();
            dic_server_version.Clear();

            if (!Directory.Exists(auto_update_path))
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
            for (int i = 0; i < ss.Length; i++)
            {
                string versionName = Path.GetFileName(ss[i].Replace("V", ""));
                dic_server_version.Add(versionName, ss[i]);
                combox_latest_version.Items.Add(versionName);
            }

            txt_new_version.Text = fnc.Get_Lastest_Version(dic_server_version.Keys.ToList());
            if (combox_latest_version.Items.Contains(txt_new_version.Text)) combox_latest_version.SelectedItem = txt_new_version.Text;


            //allPaths = fnc.Get_all_PD_files(auto_update_path);

            //if (allPaths.Length == 0) label_msg.Content = "Path error";

            //List<List<string>> result_server = new List<List<string>>();

            //result_server = fnc.Analyze_all_PD_files(allPaths);

            //if (result_server.Count == 2)
            //{
            //    list_server_all_path = new List<string>(result_server[0]);
            //    list_server_all_version = new List<string>(result_server[1]);
            //}

            //dic_server_version = fnc.Get_All_Version_for_Show(list_server_all_version, list_server_all_path);


            //txt_new_version.Text = fnc.Get_Lastest_Version(list_all_version);


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

            //txt_local_all_version.Text = "";
            //foreach(string s in list_local_all_path)
            //{
            //    txt_local_all_version.Text += (s + "\r");
            //}

            //txt_server_all_version.Text = "";
            //foreach(string s in list_server_all_path)
            //{
            //    txt_server_all_version.Text += (s + "\r");
            //}
        }

        string targetExe = "";
        private void btn_update_Click(object sender, RoutedEventArgs e)
        {
            #region Copy_Server_Version

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

                MessageBox.Show("Updated");
            }

            #endregion
        }

        private void btn_Open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(dic_local_version[combox_now_version.SelectedItem.ToString()]);
                this.Close();
            }
            catch { }
        }

        //private void Get_all_PD_files(string pathDirectory)
        //{            
        //    list_all_version.Clear();
        //    list_all_path.Clear();

        //    string[] filePaths = Directory.GetFiles(pathDirectory);

        //    Analyze_all_PD_files(filePaths);           
        //}

        //private void Analyze_all_PD_files(string[] all_filePaths)
        //{
        //    string allfiles = "";

        //    foreach (string s in all_filePaths)
        //    {
        //        string exeName = Path.GetFileName(s);
        //        string exeName_without_extension = Path.GetFileNameWithoutExtension(s);

        //        if (!string.IsNullOrEmpty(exeName))
        //        {
        //            string[] exeName_detail = exeName.Split('v');
        //            string[] exeName_detail_without_extension = exeName_without_extension.Split('v');

        //            if (exeName_detail.Length > 1)
        //            {
        //                if (exeName_detail[0] == "PD-")
        //                {
        //                    allfiles = allfiles + "\r" + s;

        //                    list_all_path.Add(s);

        //                    if (!list_all_version.Contains(exeName_detail[1]))   //含有版別資訊的檔案
        //                        list_all_version.Add(exeName_detail[1]);
        //                }

        //            }
        //        }

        //    }
        //    txt_server_all_version.Text = allfiles;
        //}

        //private void Check_Version()
        //{
        //    path_now = Directory.GetCurrentDirectory();

        //    string fllename_now = Process.GetCurrentProcess().MainModule.FileName;

        //    fllename_now = Path.GetFileNameWithoutExtension(fllename_now);
        //    txt_now_version.Text = fllename_now;

        //    if (list_all_version.Count == 0)
        //    {
        //        Get_all_PD_files(path_exe);
        //    }

        //    #region Judge latest version
        //    int Level_1 = 0;
        //    int Level_2 = 0;
        //    int Level_3 = 0;

        //    all_version_level.Clear();
        //    foreach (string ss in list_all_version)
        //    {
        //        int[] version_level = new int[3];
        //        string[] version_detail = ss.Split('.');

        //        int result;
        //        if (int.TryParse(version_detail[0], out result))
        //            version_level[0] = result;

        //        if (int.TryParse(version_detail[1], out result))
        //            version_level[1] = result;

        //        if (int.TryParse(version_detail[2], out result))
        //            version_level[2] = result;

        //        //Level 1:
        //        if (version_level[0] > Level_1)
        //        {
        //            //Level 1:
        //            Level_1 = version_level[0];

        //            //Level 2:
        //            Level_2 = version_level[1];

        //            //Level 3:
        //            Level_3 = version_level[2];
        //        }
        //        else if (version_level[1] > Level_2)
        //        {
        //            if (version_level[0] >= Level_1)
        //            {
        //                //Level 2:
        //                Level_2 = version_level[1];

        //                //Level 3:
        //                Level_3 = version_level[2];
        //            }
        //        }
        //        else if (version_level[2] > Level_3)
        //        {
        //            if (version_level[1] >= Level_2)
        //            {
        //                if (version_level[0] >= Level_1)
        //                {
        //                    //Level 3:
        //                    Level_3 = version_level[2];
        //                }
        //            }
        //        }

        //        string new_version = string.Concat(Level_1, ".", Level_2, ".", Level_3);
        //        txt_new_version.Text = new_version;

        //        all_version_level.Add(version_level);
        //    }
        //    #endregion
        //}

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

            //txt_server_all_version.Text = updateFile;

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
                System.Diagnostics.Process.Start(txt_server_path.Text);
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
            #region Check server latest version

            combox_latest_version.Items.Clear();
            all_version_for_show.Clear();
            dic_server_version.Clear();
            string[] ss = Directory.GetDirectories(auto_update_path, "v*");
            for (int i = 0; i < ss.Length; i++)
            {
                string versionName = Path.GetFileName(ss[i].Replace("V", ""));
                dic_server_version.Add(versionName, ss[i]);
                combox_latest_version.Items.Add(versionName);
            }

            txt_new_version.Text = fnc.Get_Lastest_Version(dic_server_version.Keys.ToList());
            if (combox_latest_version.Items.Contains(txt_new_version.Text)) combox_latest_version.SelectedItem = txt_new_version.Text;
            
            #endregion

        }

        private void combox_now_version_DropDownOpened(object sender, EventArgs e)
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
        }


    }
}
