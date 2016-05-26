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
        private TotalRaceInfo _totalRaceInfo;
        private RaceInfo _selectedRace;

        public TotalRaceInfo TotalRaceInfo
        {
            get { return _totalRaceInfo; }
            set
            {
                _totalRaceInfo = value;
                OnPropertyChanged("TotalRaceInfo");
            }
        }

        public RaceInfo SelectedRace
        {
            get { return _selectedRace; }
            set
            {
                _selectedRace = value;
                OnPropertyChanged("SelectedRace");
            }
        }

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
                var info = model.TotalRaceInfo;

                for (var i = info.StartNumber; i <= info.EndNumber; i++)
                {
                    info.Add(new RaceInfo { RaceNumber = i });
                }
                TotalRaceInfo = info;

                NextRaceNumber = TotalRaceInfo.StartNumber;
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
        private int _startNumber;
        private int _endNumber;
        private int _tagsPerRaceCount;
        private bool _isDigitInserting;
        private string _fileName;

        public int StartNumber
        {
            get { return _startNumber; }
            set
            {
                _startNumber = value;
                OnPropertyChanged("StartNumber");
            }
        }

        public int EndNumber
        {
            get { return _endNumber; }
            set
            {
                _endNumber = value;
                OnPropertyChanged("EndNumber");
            }
        }

        public int TagsPerRaceCount
        {
            get { return _tagsPerRaceCount; }
            set
            {
                _tagsPerRaceCount = value;
                OnPropertyChanged("TagsPerRaceCount");
            }
        }

        public bool IsDigitInserting
        {
            get { return _isDigitInserting; }
            set
            {
                _isDigitInserting = value;
                OnPropertyChanged("IsDigitInserting");
            }
        }

        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                OnPropertyChanged("FileName");
            }
        }

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
