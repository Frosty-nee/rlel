using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace rlel {
    /// <summary>
    /// Interaction logic for Account.xaml
    /// </summary>
    public partial class Account : UserControl {
        private MainWindow main;
        string accessToken;
        DateTime accessTokenExpiration;

        public Account(MainWindow main) {
            InitializeComponent();
            this.main = main;
        }

        private void remove_Click(object sender, RoutedEventArgs e) {
            this.main.accountsPanel.Children.Remove(this);
            this.main.updateCredentials();
        }

        private void launch_Click(object sender, RoutedEventArgs e) {
            this.launchAccount();
        }

        public bool launchAccount() {
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
                this.accessToken = null;
                this.main.showBalloon("logging in", e.Message, System.Windows.Forms.ToolTipIcon.Error);
                return false;
            }
            if (ssoToken == null) {
                this.main.showBalloon("logging in", "invalid username/password", System.Windows.Forms.ToolTipIcon.Error);
                return false;
            }
            this.main.showBalloon("logging in", "launching", System.Windows.Forms.ToolTipIcon.None);
            string args;
            if (this.main.singularity.IsChecked == true) {
                args = @"/server:Singularity /noconsole /ssoToken={0}";
            }
            else {
                args = @"/noconsole /ssoToken={0}";
            }
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(
                @".\bin\ExeFile.exe", String.Format(args, ssoToken)
            );
            psi.WorkingDirectory = this.main.evePath.Text;
            System.Diagnostics.Process.Start(psi);
            return true;
        }

        private string getAccessToken(string username, string password) {
            if (this.accessToken != null && DateTime.UtcNow < this.accessTokenExpiration)
                return this.accessToken;
            const string uri = "https://login.eveonline.com/Account/LogOn?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 5000;
            req.AllowAutoRedirect = true;
            req.Headers.Add("Origin", "https://login.eveonline.com");
            req.Referer = uri;
            req.CookieContainer = new CookieContainer(8);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            byte[] body = Encoding.ASCII.GetBytes(String.Format("UserName={0}&Password={1}", username, Uri.EscapeDataString(password)));
            req.ContentLength = body.Length;
            Stream reqStream = req.GetRequestStream();
            reqStream.Write(body, 0, body.Length);
            reqStream.Close();
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            // https://login.eveonline.com/launcher?client_id=eveLauncherTQ#access_token=...&token_type=Bearer&expires_in=43200
            string accessToken = this.extractAccessToken(resp.ResponseUri.Fragment);
            resp.Close(); // WTF.NET http://stackoverflow.com/questions/11712232/ and http://stackoverflow.com/questions/1500955/
            this.accessToken = accessToken;
            this.accessTokenExpiration = DateTime.UtcNow + TimeSpan.FromHours(11); // expiry is 12 hours; we use 11 to be safe
            return accessToken;
        }

        private string getSSOToken(string username, string password) {
            string accessToken = this.getAccessToken(username, password);
            if (accessToken == null)
                return null;
            string uri = "https://login.eveonline.com/launcher/token?accesstoken=" + accessToken;
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

        private void credentialsChanged(object sender, EventArgs e) {
            this.main.updateCredentials();
        }
    }
}
