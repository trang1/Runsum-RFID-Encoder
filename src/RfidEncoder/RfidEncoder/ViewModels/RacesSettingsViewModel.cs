using System;
using System.Collections.Generic;
using System.IO;
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
                TagsPerRaceCount = totalRaceInfo.TagsPerRaceCount == 0 ? 1 : totalRaceInfo.TagsPerRaceCount
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
            if (TotalRaceInfo.StartNumber >= TotalRaceInfo.EndNumber)
            {
                MessageBox.Show("End number must be greater than start number");
                return;
            }

            if (!IsPathValid(TotalRaceInfo.FileName))
            {
                if (MessageBox.Show("File path is invalid. Continue anyway?", "Question", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    TotalRaceInfo.FileName = null;
                }
                else
                {
                    return;
                }
            }

            if (FrameworkElement is Window)
                ((Window)FrameworkElement).DialogResult = true;
        }

        public bool IsPathValid(String pathString)
        {
            Uri pathUri;
            Boolean isValidUri = Uri.TryCreate(pathString, UriKind.Absolute, out pathUri);

            return isValidUri && pathUri != null && pathUri.IsLoopback;
        }
    }
}
