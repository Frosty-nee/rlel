using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Security;

namespace rlel {
    /// <summary>
    /// Interaction logic for Account.xaml
    /// </summary>
    public partial class Account : UserControl {
        public delegate void balloon_event(string[] args, System.Windows.Forms.ToolTipIcon tti);
        public event balloon_event show_balloon;
        private MainWindow main;
        string tranqToken;
        string sisiToken;
        DateTime tranqTokenExpiration;
        DateTime sisiTokenExpiration;


        public Account(MainWindow main) {
            InitializeComponent();
            this.main = main;

        }

        private void remove_Click(object sender, RoutedEventArgs e) {
            this.main.accountsPanel.Items.Remove(this);
            this.main.updateCredentials();
        }

        private void launch_Click(object sender, RoutedEventArgs e) {
            new Thread(()=>this.launchAccount(
                (bool)this.main.singularity.IsChecked,
                Path.Combine(this.main.evePath.Text, "bin", "exefile.exe"),
                this.username.Text,
                this.password.SecurePassword)).Start();
        }

        public void launchAccount(bool sisi, string path, string username, SecureString password ) {
            string accessToken = this.tranqToken;
            DateTime expire = this.tranqTokenExpiration;
            if (sisi) {
                accessToken = this.sisiToken;
                expire = this.sisiTokenExpiration;
            }
            if (!File.Exists(path)) {
                this.show_balloon(new string[] {"eve path", "could not find " + path}, System.Windows.Forms.ToolTipIcon.Error);
                return;
            }
            else if (username.Length == 0 || password.Length == 0) {
                this.show_balloon(new string[] {"logging in", "missing username or password"}, System.Windows.Forms.ToolTipIcon.Error);
                return;
            }
            this.show_balloon(new string[] {"logging in", username}, System.Windows.Forms.ToolTipIcon.None);
            string ssoToken = null;
            try {
                ssoToken = this.getSSOToken(username, this.password.Password, sisi);
            }
            catch (WebException e) {
                accessToken = null;
                this.show_balloon(new string[] {"logging in", e.Message}, System.Windows.Forms.ToolTipIcon.Error);
                return;
            }
            if (ssoToken == null) {
                this.show_balloon(new string[] {"logging in", "invalid username/password"}, System.Windows.Forms.ToolTipIcon.Error);
                return;
            }
            this.show_balloon(new string[] {"logging in", "launching"}, System.Windows.Forms.ToolTipIcon.None);
            string args;
            string dx9 = "dx11";
            if (sisi) {
                args = @"/noconsole /ssoToken={0} /server:Singularity";

            }
            else {
                args = @"/noconsole /ssoToken={0}";
            }
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(
                @".\bin\ExeFile.exe", String.Format(args, ssoToken)
            );
            if (sisi) {
                psi.WorkingDirectory = Properties.Settings.Default.SisiPath;
            }
            else {
                psi.WorkingDirectory = Properties.Settings.Default.TranqPath;
            }
            System.Diagnostics.Process.Start(psi);
            return;
        }

        private string getAccessToken(string username, string password, bool sisi) {
            if (!sisi && tranqToken != null && DateTime.UtcNow < this.tranqTokenExpiration)
                return this.tranqToken;
            if (sisi && sisiToken != null && DateTime.UtcNow < this.sisiTokenExpiration)
                return this.sisiToken;
            string uri = "https://login.eveonline.com/Account/LogOn?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken%20user";
            if (sisi) {
                uri = "https://sisilogin.testeveonline.com//Account/LogOn?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Fsisilogin.testeveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken%20user";
            }

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 5000;
            req.AllowAutoRedirect = true;
            if (!sisi) {
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
            if (!sisi) {
                this.tranqToken = accessToken;
                this.tranqTokenExpiration = DateTime.UtcNow + TimeSpan.FromHours(11);
            }
            else {
                this.sisiToken = accessToken;
                this.sisiTokenExpiration = DateTime.UtcNow + TimeSpan.FromHours(11);
            }

            return accessToken;
        }

        private string getSSOToken(string username, string password, bool sisi) {
            string accessToken = this.getAccessToken(username, password, sisi);
            string uri = "https://login.eveonline.com/launcher/token?accesstoken=" + accessToken;
            if (accessToken == null)
                return null;
            if (sisi) {
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
            this.launch_Click(this, new RoutedEventArgs());
        }
    }
}
