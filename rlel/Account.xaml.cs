using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace rlel {
    /// <summary>
    /// Interaction logic for Account.xaml
    /// </summary>
    public partial class Account : UserControl {
        private MainWindow main;
        string tranqToken;
        string sisiToken;
        DateTime tranqTokenExpiration;
        DateTime sisiTokenExpiration;
        private Process tranqProcess;
        private Process sisiProcess;

        [DllImport("user32.dll")]
        public static extern IntPtr SetActiveWindow (IntPtr hwnd);
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow (IntPtr hwnd);

        public Account(MainWindow main) {
            InitializeComponent();
            this.main = main;

        }

        private void remove_Click(object sender, RoutedEventArgs e) {
            this.main.accountsPanel.Items.Remove(this);
            this.main.updateCredentials();
        }

        private void launch_Click(object sender, RoutedEventArgs e) {
            this.launchAccount();
        }

        public bool activateAccount () {
            if (!ClientIsRunning( ))
                return launchAccount( );

            Process eveProcess = tranqProcess;
            if (this.main.singularity.IsChecked == true)
                eveProcess = sisiProcess;

            if (eveProcess == null || eveProcess.HasExited)
                return false;

            IntPtr wndHnd = eveProcess.MainWindowHandle;
            if (wndHnd == null)
                return false;

            if (!SetForegroundWindow(wndHnd))
                return false;

            if (SetActiveWindow(wndHnd) == null)
                return false;
            return true;
        }


        public bool launchAccount() {
            string accessToken = this.tranqToken;
            DateTime expire = this.tranqTokenExpiration;
            if (this.main.singularity.IsChecked == true) {
                accessToken = this.sisiToken;
                expire = this.sisiTokenExpiration;
            }
            string exefilePath = Path.Combine(this.main.evePath.Text, "bin", "ExeFile.exe");
            if (!File.Exists(exefilePath)) {
                this.main.showBalloon("eve path", "could not find " + exefilePath, System.Windows.Forms.ToolTipIcon.Error);
                return false;
            }
            else if (this.username.Text.Length == 0 || this.password.Password.Length == 0) {
                this.main.showBalloon("logging in", "missing username or password", System.Windows.Forms.ToolTipIcon.Error);
                return false;
            }
            this.main.showBalloon("logging in", this.username.Text, System.Windows.Forms.ToolTipIcon.None);
            string ssoToken = null;
            try {
                ssoToken = this.getSSOToken(this.username.Text, this.password.Password);
            }
            catch (WebException e) {
                accessToken = null;
                this.main.showBalloon("logging in", e.Message, System.Windows.Forms.ToolTipIcon.Error);
                return false;
            }
            if (ssoToken == null) {
                this.main.showBalloon("logging in", "invalid username/password", System.Windows.Forms.ToolTipIcon.Error);
                return false;
            }
            this.main.showBalloon("logging in", "launching", System.Windows.Forms.ToolTipIcon.None);
            string args;
            string dx9 = "dx11";
            if (this.main.dx9.IsChecked == true)
                dx9 = "dx9";
            if (this.main.singularity.IsChecked == true) {
                args = @"/noconsole /ssoToken={0} /triPlatform={1} /server:Singularity";

            }
            else {
                args = @"/noconsole /ssoToken={0} /triPlatform={1}";
            }
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(
                @".\bin\ExeFile.exe", String.Format(args, ssoToken, dx9)
            );
            if (this.main.singularity.IsChecked == true) {
                psi.WorkingDirectory = Properties.Settings.Default.SisiPath;
                this.sisiProcess = System.Diagnostics.Process.Start(psi);
                this.sisiProcess.EnableRaisingEvents = true;
                this.sisiProcess.Exited += sisiProcess_Exited;
                runningSiSi.Visibility = Visibility.Visible;
            }
            else {
                psi.WorkingDirectory = Properties.Settings.Default.TranqPath;
                this.tranqProcess = System.Diagnostics.Process.Start(psi);
                this.tranqProcess.EnableRaisingEvents = true;
                this.tranqProcess.Exited += tranqProcess_Exited;
                runningTQ.Visibility = Visibility.Visible;
            }
            return true;
        }
        void sisiProcess_Exited (object sender, EventArgs e) {
            this.sisiProcess = null;
            runningSiSi.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                runningSiSi.Visibility = Visibility.Hidden;
            });

        }
        void tranqProcess_Exited (object sender, EventArgs e) {
            this.tranqProcess = null;
            runningTQ.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                runningTQ.Visibility = Visibility.Hidden;
            });
        }

        private string getAccessToken(string username, string password) {
            if (this.main.singularity.IsChecked == false && tranqToken != null && DateTime.UtcNow < this.tranqTokenExpiration)
                return this.tranqToken;
            if (this.main.singularity.IsChecked == true && sisiToken != null && DateTime.UtcNow < this.sisiTokenExpiration)
                return this.sisiToken;
            string uri = "https://login.eveonline.com/Account/LogOn?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
            if (this.main.singularity.IsChecked == true) {
                uri = "https://sisilogin.testeveonline.com//Account/LogOn?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Fsisilogin.testeveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
            }

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 5000;
            req.AllowAutoRedirect = true;
            if (this.main.singularity.IsChecked == false) {
                req.Headers.Add("Origin", "https://login.eveonline.com");
            }
            else {
                req.Headers.Add("Origin", "https://sisilogin.testeveonline.com");
            }
            req.Referer = uri;
            req.CookieContainer = new CookieContainer(8);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            byte[] body = Encoding.ASCII.GetBytes(String.Format("UserName={0}&Password={1}", Uri.EscapeDataString(username), Uri.EscapeDataString(password)));
            req.ContentLength = body.Length;
            Stream reqStream = req.GetRequestStream();
            reqStream.Write(body, 0, body.Length);
            reqStream.Close();
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            // https://login.eveonline.com/launcher?client_id=eveLauncherTQ#access_token=...&token_type=Bearer&expires_in=43200
            string accessToken = this.extractAccessToken(resp.ResponseUri.Fragment);
            resp.Close(); // WTF.NET http://stackoverflow.com/questions/11712232/ and http://stackoverflow.com/questions/1500955/
            if (this.main.singularity.IsChecked == false) {
                this.tranqToken = accessToken;
                this.tranqTokenExpiration = DateTime.UtcNow + TimeSpan.FromHours(11);
            }
            else {
                this.sisiToken = accessToken;
                this.sisiTokenExpiration = DateTime.UtcNow + TimeSpan.FromHours(11);
            }

            return accessToken;
        }

        private string getSSOToken(string username, string password) {
            string accessToken = this.getAccessToken(username, password);
            string uri = "https://login.eveonline.com/launcher/token?accesstoken=" + accessToken;
            if (accessToken == null)
                return null;
            if (this.main.singularity.IsChecked == true) {
                uri = "https://sisilogin.testeveonline.com/launcher/token?accesstoken=" + accessToken;
            }
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 5000;
            req.AllowAutoRedirect = false;
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            string ssoToken = this.extractAccessToken(resp.GetResponseHeader("Location"));
            resp.Close();
            return ssoToken;
        }

        private string extractAccessToken(string urlFragment) {
            const string search = "#access_token=";
            int start = urlFragment.IndexOf(search);
            if (start == -1)
                return null;
            start += search.Length;
            string accessToken = urlFragment.Substring(start, urlFragment.IndexOf('&') - start);
            return accessToken;
        }

        private void UserControl_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            this.launchAccount();
        }

        public bool ClientIsRunning() {
            if (this.main.singularity.IsChecked == false && this.tranqProcess != null)
                return !this.tranqProcess.HasExited;
            else if (this.main.singularity.IsChecked == true && this.sisiProcess != null)
                return !this.sisiProcess.HasExited;
            else
                return false;
        }
    }
}
