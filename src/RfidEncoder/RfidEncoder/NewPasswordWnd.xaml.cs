using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace RfidEncoder
{
    /// <summary>
    /// Interaction logic for NewPasswordWnd.xaml
    /// </summary>
    public partial class NewPasswordWnd : Window
    {
        public NewPasswordWnd(string password)
        {
            InitializeComponent();

            Password = password;
        }

        public string Password
        {
            get { return PwdTextBox.Text; }
            set { PwdTextBox.Text = value; }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
