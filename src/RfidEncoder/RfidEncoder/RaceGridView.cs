using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace RfidEncoder
{
    public class RaceGridView: DataGrid
    {
        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            if (newValue is TotalRaceInfo)
            {
                this.Columns.Clear();

                FillColumns(newValue as TotalRaceInfo);
            }
        }

        private void FillColumns(TotalRaceInfo totalRaceInfo)
        {
            var raceColumn = new DataGridTextColumn();
            raceColumn.Header = "Race #";
            raceColumn.Binding = new Binding("RaceNumber");
            Columns.Add(raceColumn);

            for (int index = 0; index < totalRaceInfo.TagsPerRaceCount; index++)
            {
                var column = new DataGridTextColumn();
                column.Header = "Tag " + (index + 1) + " code (dec)";
                column.Binding = new Binding("TagList["+index+"]");

                Columns.Add(column);
            }
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
