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

namespace rlel {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        System.Windows.Forms.NotifyIcon tray;
        int tranqVersion;
        int sisiVersion;
        EventHandler contextMenuClick;
        DateTime updateCheckExpire = new DateTime();
        System.Timers.Timer checkUpdate;
        RijndaelManaged rjm = new RijndaelManaged();
        Thread tqpatching;
        Thread sisipatching;

        public MainWindow() {
            this.settingsUpgrade();
            InitializeComponent();
            string key = this.getKey();
            string iv = this.getIV();
            this.rjm.Key = Convert.FromBase64String(key);
            this.rjm.IV = Convert.FromBase64String(iv);
        }

        private void onballoonEvent(string[] args, System.Windows.Forms.ToolTipIcon tti) {
            this.showBalloon(args[0], args[1], tti);
        }

        private void browseClick(object sender, RoutedEventArgs e) {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.ShowNewFolderButton = false;
            fbd.SelectedPath = this.evePath.Text;
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                this.evePath.Text = fbd.SelectedPath;
            }
        }

        private void windowStateChanged(object sender, EventArgs e) {
            this.ShowInTaskbar = (this.WindowState != System.Windows.WindowState.Minimized);
            if (this.WindowState == System.Windows.WindowState.Minimized) {
                this.Hide();
            }
        }

        private void addAccountClick(object sender, RoutedEventArgs e) {
            Account acc = new Account(this);
            acc.showBalloon += new Account.balloonEvent(onballoonEvent);
            this.accountsPanel.Items.Add(acc);
            this.accountsPanel.SelectedItem = acc;
            this.user.Focus();
            this.user.SelectAll();
        }

        private void saveClick(object sender, RoutedEventArgs e) {
            if (this.accountsPanel.SelectedItem == null) {
                return;
            }
			((Account)this.accountsPanel.SelectedItem).accountName.Text = this.accountName.Text;
            ((Account)this.accountsPanel.SelectedItem).username.Text = this.user.Text;
            ((Account)this.accountsPanel.SelectedItem).password.Password = this.pass.Password;
            this.updateCredentials();
            this.accountsPanel.Items.Refresh();
        }

        private void removeClick(object sender, RoutedEventArgs e) {
            List<Account> acl = new List<Account>();
            foreach (Account a in this.accountsPanel.SelectedItems) {
                acl.Add(a);
            }
            foreach (Account acct in acl) {
                this.accountsPanel.Items.Remove(acct);
                acct.showBalloon -= new Account.balloonEvent(onballoonEvent);
            }
            this.updateCredentials();
            if (this.accountsPanel.Items.Count > 0) {
                this.accountsPanel.SelectedItem = this.accountsPanel.Items[0];
            }
        }


        private void windowLoaded(object sender, RoutedEventArgs e) {
            if (Properties.Settings.Default.TranqPath.Length == 0) {
                string path = this.getTranqPath();
                if (path != null && File.Exists(Path.Combine(path, "bin", "Exefile.exe"))) {
                    Properties.Settings.Default.TranqPath = path;
                    Properties.Settings.Default.Save();
                }
                if (Properties.Settings.Default.SisiPath.Length == 0)
                    path = this.getSisiPath();
                if (path != null && File.Exists(Path.Combine(path, "bin", "Exefile.exe"))) {
                    Properties.Settings.Default.SisiPath = path;
                    Properties.Settings.Default.Save();
                }
            }

            Thread updateCheck = new Thread(() => this.updater());
            updateCheck.SetApartmentState(ApartmentState.STA);
            updateCheck.Start();
            this.evePath.Text = Properties.Settings.Default.TranqPath;
            this.tray = new System.Windows.Forms.NotifyIcon();
            this.tray.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ResourceAssembly.Location);
            this.tray.Text = this.Title;
            this.tray.ContextMenu = new System.Windows.Forms.ContextMenu();
            this.tray.MouseClick += new System.Windows.Forms.MouseEventHandler(this.trayClick);
            this.contextMenuClick = new EventHandler(this.contextMenu_Click);
            this.tray.ContextMenu.MenuItems.Add("Exit", this.contextMenuClick);
            this.tray.ContextMenu.MenuItems.Add("Singularity", this.contextMenuClick);
            this.tray.ContextMenu.MenuItems.Add("-");
            if (Properties.Settings.Default.accounts != null) {
                this.popAccounts();
            }
            this.tray.ContextMenu.MenuItems.Add("-");
            this.popContextMenu();
            this.tray.Visible = true;
            this.checkUpdate = new System.Timers.Timer(3600000); //this is 1 hour, I think
            this.checkUpdate.Elapsed += new ElapsedEventHandler(checkUpdateElapsed);
            this.autoUpdate.IsChecked = Properties.Settings.Default.autoPatch;
            if (Properties.Settings.Default.autoPatch) {
                Thread client = new Thread(() => this.checkClientVersion());
                client.Start();

                this.checkUpdate.Enabled = true;
            }
        }

        void checkUpdateElapsed(object sender, ElapsedEventArgs e) {
            Thread client = new Thread(() => this.checkClientVersion());
            client.Start();
        }

        private void windowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            this.tray.Visible = false;
        }

        private string getTranqPath() {
            String path = null;
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            foreach (string dir in Directory.EnumerateDirectories(Path.Combine(appdata, "CCP", "EVE"), "*_tranquility")) {
                string[] split = dir.Split(new char[] { '_' }, 2);
                string drive = split[0].Substring(split[0].Length - 1);
                path = split[1].Substring(0, split[1].Length - "_tranquility".Length).Replace('_', Path.DirectorySeparatorChar);
                path = drive.ToUpper() + Path.VolumeSeparatorChar + Path.DirectorySeparatorChar + path;
                break;
            }
            return path;
        }

        private string getSisiPath() {
            String path = null;
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            foreach (string dir in Directory.EnumerateDirectories(Path.Combine(appdata, "CCP", "EVE"), "*_singularity")) {
                string[] split = dir.Split(new char[] { '_' }, 2);
                string drive = split[0].Substring(split[0].Length - 1);
                path = split[1].Substring(0, split[1].Length - "_singularity".Length).Replace('_', Path.DirectorySeparatorChar);
                path = drive.ToUpper() + Path.VolumeSeparatorChar + Path.DirectorySeparatorChar + path;
                break;
            }
            return path;
        }

        private void singularityClick(object sender, RoutedEventArgs e) {
            if (this.singularity.IsChecked == false) {
                this.evePath.Text = Properties.Settings.Default.TranqPath;
            }
            else {
                this.evePath.Text = Properties.Settings.Default.SisiPath;
            }
            this.tray.ContextMenu.MenuItems[1].Checked = (bool)this.singularity.IsChecked;
        }

        private void evePathTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            if (this.singularity.IsChecked == true) {
                Properties.Settings.Default.SisiPath = this.evePath.Text;
                Properties.Settings.Default.Save();
            }
            else {
                Properties.Settings.Default.TranqPath = this.evePath.Text;
                Properties.Settings.Default.Save();
            }
        }

        private void popAccounts() {
            foreach (string credentials in Properties.Settings.Default.accounts) {
                Account account = new Account(this);
                string[] split = credentials.Split(new char[] { ':' }, 4);
				if (split.Length < 3) {
					string user = split[0];
					string pass = split[1];
					split = new string[] { "", split[0], split[1] };
				}
                account.accountName.Text = split[0];
                account.username.Text = split[1];
                account.password.Password = this.decryptPass(rjm, split[2]);
                this.accountsPanel.Items.Add(account);
                this.accountsPanel.SelectedItem = this.accountsPanel.Items[0];
            }
        }

        private string getKey() {
            if (Properties.Settings.Default.Key != null && Properties.Settings.Default.Key != "") {
                return Properties.Settings.Default.Key;
            }
            else {
                this.rjm.GenerateKey();
                Properties.Settings.Default.Key = Convert.ToBase64String(this.rjm.Key);
                Properties.Settings.Default.Save();
                return Properties.Settings.Default.Key;
            }
        }

        private string getIV() {
            if (Properties.Settings.Default.IV != null && Properties.Settings.Default.IV != "") {
                return Properties.Settings.Default.IV;
            }
            else {
                this.rjm.GenerateIV();
                Properties.Settings.Default.IV = Convert.ToBase64String(this.rjm.IV);
                Properties.Settings.Default.Save();
                return Properties.Settings.Default.IV;
            }
        }

        private string encryptPass(RijndaelManaged rin, string pass) {
            ICryptoTransform encryptor = rin.CreateEncryptor();
            byte[] inblock = Encoding.Unicode.GetBytes(pass);
            byte[] encrypted = encryptor.TransformFinalBlock(inblock, 0, inblock.Length);
            string epass = Convert.ToBase64String(encrypted);
            return epass;
        }

        private string decryptPass(RijndaelManaged rin, string epass) {
            ICryptoTransform decryptor = rin.CreateDecryptor();
            byte[] pass = Convert.FromBase64String(epass);
            byte[] outblock = decryptor.TransformFinalBlock(pass, 0, pass.Length);
            string dstring = Encoding.Unicode.GetString(outblock);
            return dstring;
        }

        private void trayClick(object sender, System.Windows.Forms.MouseEventArgs e) {
            if (e.Button == System.Windows.Forms.MouseButtons.Left) {
                this.Show();
                this.WindowState = System.Windows.WindowState.Normal;
            }
        }

        private void contextMenu_Click(object sender, EventArgs e) {
            string username = ((System.Windows.Forms.MenuItem)sender).Text;
            if (username == "Singularity") {
                this.singularity.IsChecked = !this.singularity.IsChecked;
                ((System.Windows.Forms.MenuItem)sender).Checked = (bool)this.singularity.IsChecked;
            }
            if (username == "Exit") {
                this.Close();
            }
            else {
                foreach (Account account in this.accountsPanel.Items) {
                    if (account.username.Text == username) {
						new Thread(() => account.launchAccount(
							(bool)this.singularity.IsChecked,
							Path.Combine(this.evePath.Text, "bin", "exefile.exe"),
							account.accountName.Text,
                            account.username.Text,
                            account.password.SecurePassword)).Start();
                        break;
                    }
                }
            }
        }

        private void popContextMenu() {
            while (this.tray.ContextMenu.MenuItems.Count > 3) {
                this.tray.ContextMenu.MenuItems.RemoveAt(this.tray.ContextMenu.MenuItems.Count - 1);
            }
            foreach (Account account in this.accountsPanel.Items) {
                this.tray.ContextMenu.MenuItems.Add(account.username.Text, this.contextMenuClick);
            }

        }

        public void showBalloon(string title, string text, System.Windows.Forms.ToolTipIcon icon) {
            this.tray.ShowBalloonTip(1000, title, text, icon);
        }

        public void updateCredentials() {
            StringCollection accounts = new StringCollection();
            foreach (Account account in this.accountsPanel.Items) {
                string credentials = String.Format("{0}:{1}:{2}", account.accountName.Text, account.username.Text, this.encryptPass(this.rjm, account.password.Password));
                accounts.Add(credentials);
            }
            Properties.Settings.Default.accounts = accounts;
            Properties.Settings.Default.Save();
            this.popContextMenu();
        }

        private void updateEveVersion() {
            if (DateTime.UtcNow > this.updateCheckExpire) {
                System.Net.WebClient wc = new System.Net.WebClient();
                string ds;
                try {
                    ds = wc.DownloadString(new Uri("http://client.eveonline.com/patches/premium_patchinfoTQ_inc.txt"));
                }
                catch {
                    return;
                }
                this.tranqVersion = Convert.ToInt32(ds.Substring(6, 6));

                try {
                    ds = wc.DownloadString(new Uri("http://client.eveonline.com/patches/premium_patchinfoSISI_inc.txt"));
                }
                catch {
                    return;
                }
                this.sisiVersion = Convert.ToInt32(ds.Substring(6, 6));
                this.updateCheckExpire = (DateTime.UtcNow + TimeSpan.FromHours(1));
                wc.Dispose();
            }
        }

        private void checkClientVersion() {
            this.updateEveVersion();
            StreamReader sr;
            int clientVers;
            if (this.checkFilePaths(Properties.Settings.Default.TranqPath)) {
                sr = new StreamReader(String.Format("{0}\\{1}", Properties.Settings.Default.TranqPath, "start.ini"));
                sr.ReadLine(); sr.ReadLine();

                clientVers = Convert.ToInt32(sr.ReadLine().Substring(8));
                if (this.tranqVersion != clientVers) {
                    this.patch(Properties.Settings.Default.TranqPath, false);
                }
                sr.Close();
            }
            if (this.checkFilePaths(Properties.Settings.Default.SisiPath)) {
                sr = new StreamReader(String.Format("{0}\\{1}", Properties.Settings.Default.SisiPath, "start.ini"));
                sr.ReadLine(); sr.ReadLine();
                clientVers = Convert.ToInt32(sr.ReadLine().Substring(8));
                if (this.sisiVersion != clientVers) {
                    this.patch(Properties.Settings.Default.SisiPath, true);
                }
                sr.Close();
            }
        }

        private bool checkFilePaths(string path) {
            string exeFilePath;
            exeFilePath = Path.Combine(path, "bin", "ExeFile.exe");
            return File.Exists(exeFilePath);
        }

        private void patch(string path, bool sisi) {
            if (!sisi && tqpatching != null && tqpatching.IsAlive)
                return;
            if (sisi && sisipatching != null && sisipatching.IsAlive)
                return;

            Process p = isLauncherRunning(path);
            System.Diagnostics.ProcessStartInfo repair = new System.Diagnostics.ProcessStartInfo(@".\eve.exe", "");
            if (sisi)
                repair = new System.Diagnostics.ProcessStartInfo(@".\eve.exe", "/server:singularity");
            repair.WorkingDirectory = path;
            repair.WindowStyle = ProcessWindowStyle.Minimized;
            Process proc;
            if (p == null) {
                proc = System.Diagnostics.Process.Start(repair);
            }
            else {
                proc = p;
            }
            string log = Path.Combine(repair.WorkingDirectory, "launcher", "cache", String.Format("launcher.{0}.log", DateTime.UtcNow.ToString("yyyy-MM-dd")));
            Thread akill = new Thread(() => this.kill(log, proc));
            akill.Start();

            if (!sisi)
                tqpatching = akill;
            else
                sisipatching = akill;

        }

        private void kill(string path, Process PID) {
            while (!File.Exists(path))
                Thread.Sleep(1000);
            Thread.Sleep(5000);
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                using (StreamReader sr = new StreamReader(fs)) {
                    string s = sr.ReadToEnd();
                    while (true) {
                        s = sr.ReadToEnd();
                        if (s.Contains("Client update: successful")) {
                            foreach (Process p in getChildren(PID.Id)) {
                                p.CloseMainWindow();
                            }
                            return;
                        }
                        if (allChildrenDead(PID.Id))
                            return;
                        Thread.Sleep(1000);
                    }
                }
            }
        }

        private static bool allChildrenDead(int id) {
            foreach (Process p in getChildren(id)) {
                if (!p.HasExited)
                    return false;
            }
            return true;
        }

        private Process isLauncherRunning(string path) {
            Process[] plist = Process.GetProcessesByName("launcher");
            foreach (Process p in plist) {
                if (((List<Process>)getChildren(p.Id)).Count == 0) {
                    string split = p.Modules[0].FileName.Split(new string[] { "launcher" }, StringSplitOptions.None)[0];
                    if (path.ToLower() == split.ToLower().Substring(0, split.Length - 1))
                        return p;
                }
            }
            return null;
        }

        private static IEnumerable<Process> getChildren(int id) {
            List<Process> children = new List<Process>();
            List<Process> grandchildren = new List<Process>();
            ManagementObjectSearcher search = new ManagementObjectSearcher(String.Format("SELECT * FROM Win32_Process WHERE ParentProcessID={0}", id));
            foreach (ManagementObject mo in search.Get()) {
                try {
                    children.Add(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])));
                }
                catch {
                }
            }
            foreach (Process child in children) {
                grandchildren.AddRange(getChildren(child.Id));
            }
            children.AddRange(grandchildren);
            return children;

        }

        private void autoUpdateClick(object sender, RoutedEventArgs e) {
            if (autoUpdate.IsChecked == true) {
                Thread client = new Thread(() => this.checkClientVersion());
                client.Start();
                checkUpdate.Enabled = true;
            }
            if (autoUpdate.IsChecked == false) {
                this.checkUpdate.Enabled = false;
            }
            Properties.Settings.Default.autoPatch = (bool)autoUpdate.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void accountsPanelSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            if (this.accountsPanel.SelectedItem != null) {
                if (((Account)this.accountsPanel.SelectedItem).username.Text != null)
                    this.user.Text = ((Account)this.accountsPanel.SelectedItem).username.Text;
                if (((Account)this.accountsPanel.SelectedItem).password.Password != null)
                    this.pass.Password = ((Account)this.accountsPanel.SelectedItem).password.Password;
            }
        }

        private void launchClick(object sender, RoutedEventArgs e){
            bool sisi = (bool)this.singularity.IsChecked;
            string path = Path.Combine(this.evePath.Text, "bin", "exefile.exe");
            foreach (Account acct in this.accountsPanel.SelectedItems)
            {
				string accountName = acct.accountName.Text;
                string username = acct.username.Text;
                SecureString password = acct.password.SecurePassword;
                new Thread(() => acct.launchAccount(sisi, path, accountName, username, password)).Start();
                Thread.Sleep(100); // there is a better way to fix this but meh for now
            }
        }

        private void settingsUpgrade() {
            if (Properties.Settings.Default.upgraded != true) {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.upgraded = true;
                Properties.Settings.Default.Save();
            }
        }

        private Boolean accountStringValid() {
			foreach(String acct in Properties.Settings.Default.accounts) {
				if (acct.Split(':').Length < 3)
					return false;
			}
            return true;
        }


        private void updater() {
            System.Net.WebClient wc = new System.Net.WebClient();
            string str = "";
            try {
                str = wc.DownloadString(new Uri("http://rlel.frosty-nee.net/VERSION"));
            }
            catch {
                this.showBalloon("Error", "rlel version checking timed out", System.Windows.Forms.ToolTipIcon.Error);
                return;
            }
            String[] splat = str.Split(new String[] { "\r\n", " " }, StringSplitOptions.RemoveEmptyEntries);
            Version localversion = Version.Parse(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Version remoteversion = Version.Parse(splat[1].ToString());
            if (localversion < remoteversion) {
                update u = new update();
                u.ShowDialog();
            }
        }
    }
}



