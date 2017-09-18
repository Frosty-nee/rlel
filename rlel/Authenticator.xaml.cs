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

namespace rlel
{
    /// <summary>
    /// Interaction logic for Authenticator.xaml
    /// </summary>
    public partial class Authenticator : Window
    {
        public Authenticator(Account acct)
        {
            InitializeComponent();
            this.notification.Text += acct.username.Text;
        }

        private void submit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void authCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.Close();
        }
    }
}
