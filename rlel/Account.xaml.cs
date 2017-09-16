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
        public delegate void balloonEvent(string[] args, System.Windows.Forms.ToolTipIcon tti);
		
		MainWindow main;
        public string tranqToken;
        public string sisiToken;
        public DateTime tranqTokenExpiration;
        public DateTime sisiTokenExpiration;
        public string SettingsDir;


        public Account(MainWindow main) {
            InitializeComponent();
            this.main = main;

        }
    }
}
