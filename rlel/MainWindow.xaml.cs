using System;
using System.Timers;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Management;
using System.Reflection;
using System.Security;
using System.Net;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace rlel {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        System.Windows.Forms.NotifyIcon tray;
        EventHandler contextMenuClick;
        RijndaelManaged rjm = new RijndaelManaged();

        public MainWindow()
        {
            this.SettingsUpgrade();
            InitializeComponent();
            
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.TranqPath.Length == 0)
            {
                string path = this.GetTranqPath();
                if (path != null && File.Exists(Path.Combine(path, "bin", "Exefile.exe")))
                {
                    Properties.Settings.Default.TranqPath = path;
                    Properties.Settings.Default.Save();
                }
                if (Properties.Settings.Default.SisiPath.Length == 0)
                    path = this.GetSisiPath();
                if (path != null && File.Exists(Path.Combine(path, "bin", "Exefile.exe")))
                {
                    Properties.Settings.Default.SisiPath = path;
                    Properties.Settings.Default.Save();
                }
            }
            //get password from user to decrypt account credentials
            string key;
            HashAlgorithm hashAlgorithm = SHA256.Create();
            while (true)
            {
                if (Properties.Settings.Default.IV == "" || Properties.Settings.Default.IV == null)
                {
                    key = this.SetKey();
                    this.rjm.IV = Convert.FromBase64String(this.GetIV());
                    this.rjm.Key = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(key));
                    Properties.Settings.Default.Key = this.EncryptPass("this is a string");
                    Properties.Settings.Default.Save();
                    break;
                }
                else
                {
                this.rjm.IV = Convert.FromBase64String(this.GetIV());
                key = this.GetKey(hashAlgorithm);
                if (CheckKey(key, hashAlgorithm))
                    break;
                }
            }
            byte[] hashedKey = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(key.ToCharArray()));
            this.rjm.Key = hashedKey;

            this.evePath.Text = Properties.Settings.Default.TranqPath;
            this.tray = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ResourceAssembly.Location),
                Text = this.Title,
                ContextMenu = new System.Windows.Forms.ContextMenu()
            };
            this.tray.MouseClick += new System.Windows.Forms.MouseEventHandler(this.TrayClick);
            this.contextMenuClick = new EventHandler(this.ContextMenu_Click);
            this.tray.ContextMenu.MenuItems.Add("Exit", this.contextMenuClick);
            this.tray.ContextMenu.MenuItems.Add("Singularity", this.contextMenuClick);
            this.tray.ContextMenu.MenuItems.Add("-");
            if (Properties.Settings.Default.accounts != null)
            {
                this.PopAccounts();
            }
            this.tray.ContextMenu.MenuItems.Add("-");
            this.PopContextMenu();
            this.tray.Visible = true;
            this.CheckRlelUpdate();
        }
        private void OnballoonEvent(string[] args, System.Windows.Forms.ToolTipIcon tti)
        {
            this.ShowBalloon(args[0], args[1], tti);
        }

        private void BrowseClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog
            {
                ShowNewFolderButton = false,
                SelectedPath = this.evePath.Text
            };
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.evePath.Text = fbd.SelectedPath;
            }
        }

        private void WindowStateChanged(object sender, EventArgs e)
        {
            this.ShowInTaskbar = (this.WindowState != System.Windows.WindowState.Minimized);
            if (this.WindowState == System.Windows.WindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void AddAccountClick(object sender, RoutedEventArgs e)
        {
            Account acc = new Account(this);
            this.accountsPanel.Items.Add(acc);
            this.accountsPanel.SelectedItem = acc;
            this.user.Focus();
            this.user.SelectAll();
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            if (this.accountsPanel.SelectedItem == null)
            {
                return;
            }
            ((Account)this.accountsPanel.SelectedItem).username.Text = this.user.Text;
            ((Account)this.accountsPanel.SelectedItem).password.Password = this.pass.Password;
            this.UpdateCredentials();
            this.accountsPanel.Items.Refresh();
        }

        private void RemoveClick(object sender, RoutedEventArgs e)
        {
            List<Account> acl = new List<Account>();
            foreach (Account a in this.accountsPanel.SelectedItems)
            {
                acl.Add(a);
            }
            foreach (Account acct in acl)
            {
                this.accountsPanel.Items.Remove(acct);
            }
            this.UpdateCredentials();
            if (this.accountsPanel.Items.Count > 0)
            {
                this.accountsPanel.SelectedItem = this.accountsPanel.Items[0];
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.tray.Visible = false;
        }

        private string GetTranqPath()
        {
            String path = null;
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            foreach (string dir in Directory.EnumerateDirectories(Path.Combine(appdata, "CCP", "EVE"), "*_tranquility"))
            {
                string[] split = dir.Split(new char[] { '_' }, 2);
                string drive = split[0].Substring(split[0].Length - 1);
                path = split[1].Substring(0, split[1].Length - "_tranquility".Length).Replace('_', Path.DirectorySeparatorChar);
                path = drive.ToUpper() + Path.VolumeSeparatorChar + Path.DirectorySeparatorChar + path;
                break;
            }
            return path;
        }

        private string GetSisiPath()
        {
            String path = null;
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            foreach (string dir in Directory.EnumerateDirectories(Path.Combine(appdata, "CCP", "EVE"), "*_singularity"))
            {
                string[] split = dir.Split(new char[] { '_' }, 2);
                string drive = split[0].Substring(split[0].Length - 1);
                path = split[1].Substring(0, split[1].Length - "_singularity".Length).Replace('_', Path.DirectorySeparatorChar);
                path = drive.ToUpper() + Path.VolumeSeparatorChar + Path.DirectorySeparatorChar + path;
                break;
            }
            return path;
        }

        private void SingularityClick(object sender, RoutedEventArgs e)
        {
            if (this.singularity.IsChecked == false)
            {
                this.evePath.Text = Properties.Settings.Default.TranqPath;
            }
            else
            {
                this.evePath.Text = Properties.Settings.Default.SisiPath;
            }
            this.tray.ContextMenu.MenuItems[1].Checked = (bool)this.singularity.IsChecked;
        }

        private void EvePathTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (this.singularity.IsChecked == true)
            {
                Properties.Settings.Default.SisiPath = this.evePath.Text;
                Properties.Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.TranqPath = this.evePath.Text;
                Properties.Settings.Default.Save();
            }
        }

        private void PopAccounts()
        {
            foreach (string credentials in Properties.Settings.Default.accounts)
            {
                Account account = new Account(this);
                string[] split = credentials.Split(new char[] { ':' }, 3);
                account.SettingsDir = split[2];
                account.username.Text = this.DecryptPass(split[0]);
                account.password.Password = this.DecryptPass(split[1]);
                this.accountsPanel.Items.Add(account);
                this.accountsPanel.SelectedItem = this.accountsPanel.Items[0];
            }
        }

        private string GetKey(HashAlgorithm hashAlgorithm)
        {
            UnlockDialog ud = new UnlockDialog();
            ud.Pass.Focus();
            ud.ShowDialog();
            if (ud.reset)
            {
                this.rjm.Key = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(ud.Pass.Password));
                Properties.Settings.Default.IV = this.GetIV();
                Properties.Settings.Default.Key = this.EncryptPass("this is a string");
                Properties.Settings.Default.Save();
            }
            return (ud.Pass.Password);
        }

        private string SetKey()
        {
            UnlockDialog ud = new UnlockDialog();
            ud.prompt.Text = "Enter a new password to use rlel";
            ud.Pass.Focus();
            ud.ShowDialog();
            return (ud.Pass.Password);
        }

        private Boolean CheckKey(string key, HashAlgorithm hashAlgorithm)
        {
            byte[] hashedkey = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(key));
            try
            {
                this.rjm.Key = hashedkey;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            return true;
        }
        private string GetIV()
        {
            if (Properties.Settings.Default.IV != null && Properties.Settings.Default.IV != "")
            {
                return Properties.Settings.Default.IV;
            }
            else
            {
                this.rjm.GenerateIV();
                Properties.Settings.Default.IV = Convert.ToBase64String(this.rjm.IV);
                Properties.Settings.Default.Save();
                return Properties.Settings.Default.IV;
            }
        }

        private string EncryptPass(string pass)
        {
            ICryptoTransform encryptor = this.rjm.CreateEncryptor();
            byte[] inblock = Encoding.Unicode.GetBytes(pass);
            byte[] encrypted = encryptor.TransformFinalBlock(inblock, 0, inblock.Length);
            string epass = Convert.ToBase64String(encrypted);
            return epass;
        }

        private string DecryptPass(string epass)
        {
            ICryptoTransform decryptor = this.rjm.CreateDecryptor();
            byte[] pass = Convert.FromBase64String(epass);
            byte[] outblock = decryptor.TransformFinalBlock(pass, 0, pass.Length);
            string dstring = Encoding.Unicode.GetString(outblock);
            return dstring;
        }

        private void TrayClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.Show();
                this.WindowState = System.Windows.WindowState.Normal;
            }
        }

        private void ContextMenu_Click(object sender, EventArgs e)
        {
            string username = ((System.Windows.Forms.MenuItem)sender).Text;
            if (username == "Singularity")
            {
                this.singularity.IsChecked = !this.singularity.IsChecked;
                ((System.Windows.Forms.MenuItem)sender).Checked = (bool)this.singularity.IsChecked;
            }
            if (username == "Exit")
            {
                this.Close();
            }
            else
            {
                string path = Path.Combine(this.evePath.Text, "bin", "exefile.exe");
                foreach (Account acct in this.accountsPanel.Items)
                {
                    if (acct.username.Text == username)
                        this.LaunchAccount((bool)this.singularity.IsChecked, path, acct);
                }
            }
        }

        private void PopContextMenu()
        {
            while (this.tray.ContextMenu.MenuItems.Count > 3)
            {
                this.tray.ContextMenu.MenuItems.RemoveAt(this.tray.ContextMenu.MenuItems.Count - 1);
            }
            foreach (Account account in this.accountsPanel.Items)
            {
                this.tray.ContextMenu.MenuItems.Add(account.username.Text, this.contextMenuClick);
            }

        }

        public void ShowBalloon(string title, string text, System.Windows.Forms.ToolTipIcon icon)
        {
            this.tray.ShowBalloonTip(1000, title, text, icon);
        }

        public void UpdateCredentials()
        {
            StringCollection accounts = new StringCollection();
            foreach (Account account in this.accountsPanel.Items)
            {
                string credentials = String.Format("{0}:{1}:{2}", this.EncryptPass(account.username.Text), this.EncryptPass(account.password.Password), account.SettingsDir);
                accounts.Add(credentials);
            }
            Properties.Settings.Default.accounts = accounts;
            Properties.Settings.Default.Save();
            this.PopContextMenu();
        }

        private bool CheckFilePaths(string path)
        {
            string exeFilePath;
            exeFilePath = Path.Combine(path, "bin", "ExeFile.exe");
            return File.Exists(exeFilePath);
        }

        private void AccountsPanel_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (this.accountsPanel.SelectedItem != null)
            {
                if (((Account)this.accountsPanel.SelectedItem).username.Text != null)
                    this.user.Text = ((Account)this.accountsPanel.SelectedItem).username.Text;
                if (((Account)this.accountsPanel.SelectedItem).password.Password != null)
                    this.pass.Password = ((Account)this.accountsPanel.SelectedItem).password.Password;
            }
        }

        private void SetEveSettingsProfiles(Account acct)
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string mainSettingsDir = Directory.EnumerateDirectories(Path.Combine(appdata, "CCP", "EVE"), "*_tranquility").First<string>();

            IEnumerable<string> dirs = Directory.EnumerateDirectories(mainSettingsDir, "settings_*");

            SettingsDialog sd = new SettingsDialog(acct);
            foreach (string setdir in dirs)
            {
                string[] split = setdir.Split('\\');
                string shortname = split.Last().Split('_').Last();
                sd.SettingsDirectories.Items.Add(shortname);

            }
            if (dirs.Count() > 1)
            {

                sd.Focus();
                sd.ShowDialog();
                acct.SettingsDir = (string)sd.SettingsDirectories.SelectedItem;
            }
            else
            {
                acct.SettingsDir = (string)sd.SettingsDirectories.Items[0];
            }
            this.UpdateCredentials();
        }

        private void LaunchClick(object sender, RoutedEventArgs e)
        {
            string path = Path.Combine(this.evePath.Text, "bin", "exefile.exe");
            foreach (Account acct in this.accountsPanel.SelectedItems)
            {
                this.LaunchAccount((bool)this.singularity.IsChecked, path, acct);
                Thread.Sleep(100); // there is a better way to fix this but meh for now
            }
        }

        private void LaunchAccount(bool sisi, string path, Account acct)
        {
            this.ShowBalloon("Launching...", acct.username.Text, System.Windows.Forms.ToolTipIcon.Info);
            if (acct.SettingsDir == "" || acct.SettingsDir == null)
            {
                this.SetEveSettingsProfiles(acct);
            }

            string accessToken = acct.tranqToken;
            DateTime expire = acct.tranqTokenExpiration;
            if (sisi)
            {
                accessToken = acct.sisiToken;
                expire = acct.sisiTokenExpiration;
            }
            if (!File.Exists(path))
            {
                this.ShowBalloon("eve path", "could not find" + path, System.Windows.Forms.ToolTipIcon.Error);
                return;
            }
            else if (acct.username.Text.Length == 0 || acct.password.Password.Length == 0)
            {
                this.ShowBalloon("logging in", "missing username or password", System.Windows.Forms.ToolTipIcon.Error);
                return;
            }
            this.ShowBalloon("logging in", acct.username.Text, System.Windows.Forms.ToolTipIcon.None);
            string ssoToken = null;
            try
            {
                ssoToken = this.GetSSOToken(acct, sisi);
            }
            catch (WebException e)
            {
                accessToken = null;
                this.ShowBalloon("logging in", e.Message, System.Windows.Forms.ToolTipIcon.Error);
                return;
            }
            if (ssoToken == null)
            {
                this.ShowBalloon("logging in", "invalid username/password", System.Windows.Forms.ToolTipIcon.Error);
                return;
            }
            this.ShowBalloon("logging in", "launching", System.Windows.Forms.ToolTipIcon.None);
            string args;
            if (sisi)
            {
                args = @"/noconsole /ssoToken={0} /settingsprofile={1} /server:Singularity";

            }
            else
            {
                args = @"/noconsole /ssoToken={0} /settingsprofile={1}";
            }
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(
                @".\bin\ExeFile.exe", String.Format(args, ssoToken, acct.SettingsDir)
            );
            if (sisi)
            {
                psi.WorkingDirectory = Properties.Settings.Default.SisiPath;
            }
            else
            {
                psi.WorkingDirectory = Properties.Settings.Default.TranqPath;
            }
            System.Diagnostics.Process.Start(psi);
            return;
        }

        private string GetSSOToken(Account acct, bool sisi)
        {
            string accessToken = this.GetAccessToken(acct, sisi);
            string uri = "https://login.eveonline.com/launcher/token?accesstoken=" + accessToken;
            if (accessToken == null)
                return null;
            if (sisi)
            {
                uri = "https://sisilogin.testeveonline.com/launcher/token?accesstoken=" + accessToken;
            }
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 5000;
            req.AllowAutoRedirect = false;
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            string ssoToken = this.ExtractAccessToken(resp.GetResponseHeader("Location"));
            resp.Close();
            return ssoToken;
        }

        private string ExtractAccessToken(string urlFragment)
        {
            const string search = "#access_token=";
            int start = urlFragment.IndexOf(search);
            if (start == -1)
                return null;
            start += search.Length;
            string accessToken = urlFragment.Substring(start, urlFragment.IndexOf('&') - start);
            return accessToken;
        }

        private string GetAccessToken(Account acct, bool sisi)
        {
            if (!sisi && acct.tranqToken != null && DateTime.UtcNow < acct.tranqTokenExpiration)
                return acct.tranqToken;
            if (sisi && acct.sisiToken != null && DateTime.UtcNow < acct.sisiTokenExpiration)
                return acct.sisiToken;
            string uri = "https://login.eveonline.com/Account/LogOn?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken%20user";
            if (sisi)
            {
                uri = "https://sisilogin.testeveonline.com//Account/LogOn?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Fsisilogin.testeveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken%20user";
            }

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 5000;
            req.AllowAutoRedirect = true;
            if (!sisi)
            {
                req.Headers.Add("Origin", "https://login.eveonline.com");
            }
            else
            {
                req.Headers.Add("Origin", "https://sisilogin.testeveonline.com");
            }
            req.Referer = uri;
            req.CookieContainer = new CookieContainer(8);
            CookieContainer cook = req.CookieContainer;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            byte[] body = Encoding.ASCII.GetBytes(String.Format("UserName={0}&Password={1}", Uri.EscapeDataString(acct.username.Text), Uri.EscapeDataString(acct.password.Password)));
            req.ContentLength = body.Length;
            Stream reqStream = req.GetRequestStream();
            reqStream.Write(body, 0, body.Length);
            reqStream.Close();
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

            if (resp.ResponseUri.Fragment.Length == 0)
            {
                resp.Close();
                Authenticator auth = new Authenticator(acct);
                auth.ShowDialog();
                auth.authCode.Focus();
                uri = "https://login.eveonline.com/account/authenticator?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken%20user";
                req = (HttpWebRequest)HttpWebRequest.Create(uri);
                req.Referer = "https://login.eveonline.com/Account/LogOn?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken%20user";
                req.Timeout = 5000;
                req.AllowAutoRedirect = true;
                if (!sisi)
                    req.Headers.Add("Origin", "https://login.eveonline.com");
                req.Referer = uri;
                req.CookieContainer = cook;
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                body = Encoding.ASCII.GetBytes(String.Format("Challenge={0}&RememberTwoFactor={1}&command=Continue", Uri.EscapeDataString(auth.authCode.Text), Uri.EscapeDataString(auth.DontAsk.IsChecked.ToString())));
                req.ContentLength = body.Length;
                reqStream = req.GetRequestStream();
                reqStream.Write(body, 0, body.Length);
                reqStream.Close();
                resp = (HttpWebResponse)req.GetResponse();
            }
            // https://login.eveonline.com/launcher?client_id=eveLauncherTQ#access_token=...&token_type=Bearer&expires_in=43200
            string accessToken = this.ExtractAccessToken(resp.ResponseUri.Fragment);
            resp.Close(); // WTF.NET http://stackoverflow.com/questions/11712232/ and http://stackoverflow.com/questions/1500955/
            if (!sisi)
            {
                acct.tranqToken = accessToken;
                acct.tranqTokenExpiration = DateTime.UtcNow + TimeSpan.FromHours(11);
            }
            else
            {
                acct.sisiToken = accessToken;
                acct.sisiTokenExpiration = DateTime.UtcNow + TimeSpan.FromHours(11);
            }

            return accessToken;
        }

        //upgrades settings file from older versions of rlel to new version
        private void SettingsUpgrade()
        {
            if (Properties.Settings.Default.upgraded != true)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.upgraded = true;
                Properties.Settings.Default.Save();
            }
        }

        private void SettingsProf_Click(object sender, RoutedEventArgs e)
        {
            Account acct = (Account)this.accountsPanel.SelectedItem;
            this.SetEveSettingsProfiles(acct);
        }
        //save credentials when return is pressed in either input box
        private void User_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.SaveClick(this, e);
            }
        }
        // check if local version is >= remote
        private void CheckRlelUpdate()
        {
            Boolean upToDate = true;
            FileVersionInfo fvi = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileVersionInfo;
            int[] localVersion = new int[3];
            localVersion[0] = fvi.ProductMajorPart;
            localVersion[1] = fvi.ProductMinorPart;
            localVersion[2] = fvi.ProductBuildPart;
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create("https://api.github.com/repos/frosty-nee/rlel/releases/latest");
            req.Timeout = 5000;
            req.Method = "GET";
            req.UserAgent = String.Format("Rapid Light EVE Launcher v{0}",fvi.FileVersion );
            req.ContentType = "text/html";



            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            using (Stream responseStream= resp.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                string read = reader.ReadToEnd();
                JObject jo = JObject.Parse(read);
                string tag = (string)jo["tag_name"];
                string[] split = tag.Split('.');
                for (int i = 0; i < 3; i++)
                {
                    if(int.Parse(split[i]) > localVersion[i])
                    {
                        upToDate = false;
                    }
                }
            }

            if (!upToDate)
            {
                UpdateDialog ud = new UpdateDialog();
                ud.ShowDialog();
            }
        }

    }
}



