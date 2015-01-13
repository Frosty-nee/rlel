using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace rlel {
    /// <summary>
    /// Interaction logic for update.xaml
    /// </summary>
    public partial class update : Window {
        public update() {
            InitializeComponent();
        }

        private void ok_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void msi_Click(object sender, RoutedEventArgs e) {
            System.Diagnostics.Process.Start("http://rlel.frosty-nee.net/rlel.msi");
        }

        private void github_Click(object sender, RoutedEventArgs e) {
            System.Diagnostics.Process.Start("http://github.com/frostbite/rlel");
        }
    }
}
