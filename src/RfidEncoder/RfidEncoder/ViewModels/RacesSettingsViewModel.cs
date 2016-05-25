using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace RfidEncoder.ViewModels
{
    public class RacesSettingsViewModel : ViewModelBase
    {
        public TotalRaceInfo TotalRaceInfo { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand ChooseFileCommand { get; set; }

        public RacesSettingsViewModel(TotalRaceInfo totalRaceInfo)
        {
            TotalRaceInfo = new TotalRaceInfo()
            {
                IsDigitInserting = totalRaceInfo.IsDigitInserting,
                StartNumber = totalRaceInfo.StartNumber,
                EndNumber = totalRaceInfo.EndNumber,
                TagsPerRaceCount = totalRaceInfo.TagsPerRaceCount
            };

            SaveCommand = new DelegateCommand(Save);
            ChooseFileCommand = new DelegateCommand(ChooseFile);
        }

        private void ChooseFile()
        {
            var dialog = new OpenFileDialog();
            dialog.ShowDialog();
        }

        private void Save()
        {
            if (FrameworkElement is Window)
                ((Window)FrameworkElement).DialogResult = true;
        }
    }
}
