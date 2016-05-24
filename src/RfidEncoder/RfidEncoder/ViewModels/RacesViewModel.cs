using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RfidEncoder.ViewModels
{
    public class RacesViewModel : ViewModelBase
    {
        private int _nextRaceNumber;
        private int _nextTagNumber;
        public TotalRaceInfo TotalRaceInfo { get; set; }
        public ICommand StartEncodingCommand { get; set; }
        public ICommand NewProjectCommand { get; set; }
        public bool IsEncoding { get; set; }
        public int NextRaceNumber
        {
            get { return _nextRaceNumber; }
            set
            {
                _nextRaceNumber = value;
                OnPropertyChanged("NextRaceNumber");
            }
        }

        public int NextTagNumber
        {
            get { return _nextTagNumber; }
            set
            {
                _nextTagNumber = value;
                OnPropertyChanged("NextTagNumber");
            }
        }

        public string FileName { get; set; }

        public RacesViewModel()
        {
            StartEncodingCommand = new DelegateCommand(StartEncoding, ()=> MainWindowViewModel.Instance.IsConnected);
            NewProjectCommand = new DelegateCommand(NewProject);

        }

        private void NewProject()
        {
            TotalRaceInfo = new TotalRaceInfo();

            var wnd = new RacesSettings();
            var model = new RacesSettingsViewModel(TotalRaceInfo) { FrameworkElement = wnd };
            wnd.DataContext = model;

            if (wnd.ShowDialog().GetValueOrDefault(false))
            {
                TotalRaceInfo = model.TotalRaceInfo;

                for (var i = TotalRaceInfo.StartNumber; i <= TotalRaceInfo.EndNumber; i++)
                {
                    TotalRaceInfo.Add(new RaceInfo { RaceNumber = i });
                }
            }
        }

        private void StartEncoding()
        {
            
        }
    }

    public class RaceInfo
    {
        public int RaceNumber { get; set; }

        public ObservableCollection<int> TagList { get; set; }
    }

    public class TotalRaceInfo : ObservableCollection<RaceInfo>, INotifyPropertyChanged
    {
        public int StartNumber { get; set; }
        public int EndNumber { get; set; }

        public int TagsPerRaceCount { get; set; }
        public bool IsDigitInserting { get; set; }

        #region INotifyPropertyChanged

        public new event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }
}
