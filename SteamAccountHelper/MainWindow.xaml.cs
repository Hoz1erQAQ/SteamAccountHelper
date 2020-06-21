using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Shapes;

namespace SteamAccountHelper
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private RegistryKey steamKey;

        public MainWindow()
        {
            InitializeComponent();
        }

        public class SteamAccountItem
        {
            public string AccountName { get; set; }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                RegistryKey userKey = Registry.CurrentUser;
                steamKey = GetRegistryKey(userKey, new string[] { "SOFTWARE", "Valve", "Steam" }, true);
                if (steamKey == null)
                {
                    MessageBox.Show("读取Steam注册表信息失败");
                    this.Close();
                }
                string steamPath = Convert.ToString(steamKey.GetValue("SteamPath"));
                if (string.IsNullOrWhiteSpace(steamPath))
                {
                    MessageBox.Show("读取Steam路径信息失败");
                    this.Close();
                }
                string accountConfigPath = System.IO.Path.Combine(steamPath, "config", "loginusers.vdf");
                LstAccount.Items.Clear();
                SteamAccountItem accountItem = new SteamAccountItem();
                using (FileStream fs = new FileStream(accountConfigPath, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            string curLine = sr.ReadLine().Trim();
                            if (curLine.StartsWith("\"AccountName\""))
                            {
                                accountItem = new SteamAccountItem();
                                accountItem.AccountName = curLine.Substring(13).Trim().Trim('"');
                            }
                            if (curLine.StartsWith("\"RememberPassword\""))
                            {
                                if ("1".Equals(GetValue(curLine, "\"RememberPassword\"")))
                                {
                                    LstAccount.Items.Add(accountItem);
                                }
                            }
                        }

                    }
                }

                if (LstAccount.Items.Count < 1)
                {
                    MessageBox.Show("未检测到可自动登录的账号");
                    this.Close();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }
        }

        private string GetValue(string curLine, string startStr)
        {
            string str= curLine.Substring(startStr.Length).Trim();
            return str.Substring(1, str.Length - 2);//去除包裹账号的左右双引号
        }

        private RegistryKey GetRegistryKey(RegistryKey parentKey, string[] subKeys, bool writable = false)
        {
            if (parentKey != null && subKeys.Length > 0 && parentKey.GetSubKeyNames().Contains(subKeys[0]))
            {
                RegistryKey childKey = parentKey.OpenSubKey(subKeys[0], writable);
                if (childKey == null || subKeys.Length < 2)
                {
                    return childKey;
                }
                return GetRegistryKey(childKey, subKeys.Skip(1).ToArray(), writable);
            }
            return null;
        }

        private void LstAccount_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (Process.GetProcessesByName("steam").Length > 0)
                {
                    MessageBox.Show("steam正在运行中");
                }
                else
                {
                    try
                    {
                        string steamExe = Convert.ToString(steamKey.GetValue("SteamExe"));
                        if (sender is ListView lstView && lstView.SelectedValue is SteamAccountItem accountItem)
                        {
                            steamKey.SetValue("AutoLoginUser", accountItem.AccountName);
                        }
                        Process.Start(steamExe);
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }
    }
}
