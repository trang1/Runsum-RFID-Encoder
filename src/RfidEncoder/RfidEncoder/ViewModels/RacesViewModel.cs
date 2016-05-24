using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            StartEncodingCommand = new DelegateCommand(StartEncoding);
            NewProjectCommand = new DelegateCommand(NewProject);

        }

        private void NewProject()
        {
            TotalRaceInfo = new TotalRaceInfo();
            TotalRaceInfo.TagsPerRaceCount = 5;
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

    public class TotalRaceInfo : ObservableCollection<RaceInfo>
    {
        public int StartNumber { get; set; }
        public int EndNumber { get; set; }

        public int TagsPerRaceCount { get; set; }
    }
}
