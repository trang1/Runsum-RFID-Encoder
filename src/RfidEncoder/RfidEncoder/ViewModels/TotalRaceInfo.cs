using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RfidEncoder.ViewModels
{
    /// <summary>
    ///     Stores and manages all information about the current race
    /// </summary>
    public class TotalRaceInfo : ObservableCollection<RaceInfo>, INotifyPropertyChanged
    {
        private string _accessPassword;
        private bool _addPrefix;
        private int _codeLength;
        private int _endNumber;
        private string _fileName;
        private bool _isDigitInserting;
        private string _killPassword;
        private string _newAccessPassword;
        private bool _permalock;
        private string _prefix;
        private bool _setNewAccessPassword;
        private int _startNumber;
        private int _tagsPerRaceCount;

        public TotalRaceInfo(TotalRaceInfo totalRaceInfo)
        {
            if (totalRaceInfo == null) return;

            _startNumber = totalRaceInfo.StartNumber;
            _endNumber = totalRaceInfo.EndNumber;
            _tagsPerRaceCount = totalRaceInfo.TagsPerRaceCount;
            _isDigitInserting = totalRaceInfo.IsDigitInserting;
            _fileName = totalRaceInfo.FileName;
            _addPrefix = totalRaceInfo.AddPrefix;
            _prefix = totalRaceInfo.Prefix;
            _codeLength = totalRaceInfo.CodeLength;
            _accessPassword = totalRaceInfo.AccessPassword;
            _killPassword = totalRaceInfo.KillPassword;
            _permalock = totalRaceInfo.Permalock;
            _setNewAccessPassword = totalRaceInfo.SetNewAccessPassword;
            _newAccessPassword = totalRaceInfo.NewAccessPassword;
        }

        #region Public properties

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

        public bool AddPrefix
        {
            get { return _addPrefix; }
            set
            {
                _addPrefix = value;
                OnPropertyChanged("AddPrefix");
            }
        }

        public string Prefix
        {
            get { return _prefix; }
            set
            {
                _prefix = value;
                OnPropertyChanged("Prefix");
            }
        }

        public int CodeLength
        {
            get { return _codeLength; }
            set
            {
                _codeLength = value;
                OnPropertyChanged("CodeLength");
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

        public string AccessPassword
        {
            get { return _accessPassword; }
            set
            {
                _accessPassword = value;
                OnPropertyChanged("AccessPassword");
            }
        }

        public string KillPassword
        {
            get { return _killPassword; }
            set
            {
                _killPassword = value;
                OnPropertyChanged("KillPassword");
            }
        }

        public bool Permalock
        {
            get { return _permalock; }
            set
            {
                _permalock = value;
                OnPropertyChanged("Permalock");
            }
        }

        public bool SetNewAccessPassword
        {
            get { return _setNewAccessPassword; }
            set
            {
                _setNewAccessPassword = value;
                OnPropertyChanged("SetNewAccessPassword");
            }
        }

        public string NewAccessPassword
        {
            get { return _newAccessPassword; }
            set
            {
                _newAccessPassword = value;
                OnPropertyChanged("NewAccessPassword");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public new event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Actions

        public event TagEventHandler NextTag = (sender, args) => { };
        public event TagEventHandler SelectCell = (sender, args) => { };

        public delegate void TagEventHandler(object sender, TagEventArgs tea);

        public void FireNextTag(uint lastTag)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                Application.Current.Dispatcher.Invoke(() => NextTag(this, new TagEventArgs(lastTag)));
            });
        }

        public void FireSelectCell(int? nextRaceNumber, uint? nextTagNumber)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                Application.Current.Dispatcher.Invoke(
                    () => SelectCell(this, new TagEventArgs(nextTagNumber, nextRaceNumber)));
            });
        }

        #endregion
    }

    public class TagEventArgs : EventArgs
    {
        public TagEventArgs(uint encodedTag)
        {
            EncodedTag = encodedTag;
        }

        public TagEventArgs(uint? nextTag, int? nextRace)
        {
            NextTag = nextTag;
            NextRace = nextRace;
        }

        public uint EncodedTag { get; set; }
        public uint? NextTag { get; set; }
        public int? NextRace { get; set; }
    }

    public class RaceInfo
    {
        public int RaceNumber { get; set; }

        public ObservableCollection<uint?> TagList { get; set; }
    }
}