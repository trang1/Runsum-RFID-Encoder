using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using ThingMagic;
using System.Diagnostics;

namespace RfidEncoder.ViewModels
{
    /// <summary>
    ///     Represents the viewModel for MainWindow
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        #region private members

        private static readonly MainWindowViewModel _mainWindowViewModel = new MainWindowViewModel();
        
        #endregion

        #region Constructor
        public MainWindowViewModel()
        {
            ExitCommand = new DelegateCommand(() => Application.Current.Shutdown());

            RacesViewModel = new RacesViewModel();
            TagOperationsViewModel = new TagOperationsViewModel();
        }

        #endregion


        #region public members

        public static MainWindowViewModel Instance
        {
            get { return _mainWindowViewModel; }
        }

        public RacesViewModel RacesViewModel { get; set; }
        public TagOperationsViewModel TagOperationsViewModel { get; set; }
        
        public ICommand ExitCommand { get; set; }
        #endregion
    }
}
