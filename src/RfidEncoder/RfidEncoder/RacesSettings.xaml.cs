using System;
using System.Collections.Generic;
using System.Configuration;
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
    /// Interaction logic for RacesSettings.xaml
    /// </summary>
    public partial class RacesSettings : Window
    {
        public RacesSettings()
        {
            InitializeComponent();
        }

        public static bool MonzaR6Visibility
        {
            get
            {
                return bool.Parse(ConfigurationManager.AppSettings["MonzaR6"]);
            }
        }
    }
}
