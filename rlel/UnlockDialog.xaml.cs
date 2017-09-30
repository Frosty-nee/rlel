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
    /// Interaction logic for UnlockDialog.xaml
    /// </summary>
    public partial class UnlockDialog : Window
    {
        public Boolean reset = false;
        public UnlockDialog()
        {
            InitializeComponent();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Pass_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.Close();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.accounts = null;
            Properties.Settings.Default.IV = null;
            Properties.Settings.Default.Key = null;
            Properties.Settings.Default.Save();
            this.reset = true;
            this.prompt.Text = "Account information reset, enter a new password";
        }
    }
}
