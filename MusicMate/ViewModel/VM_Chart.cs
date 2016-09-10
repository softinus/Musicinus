using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicMate.ViewModel
{
    // bind this view model to your page or window (DataContext)
    public class VM_Chart
    {
        public static ObservableCollection<Artists> AnalData1 { get; private set; }

        public VM_Chart()
        {
            AnalData1 = new ObservableCollection<Artists>();
        }

        private object selectedItem = null;
        public object SelectedItem
        {
            get
            {
                return selectedItem;
            }
            set
            {
                // selected item has changed
                selectedItem = value;
            }
        }
    }

    // class which represent a data point in the chart
    public class Artists
    {
        public string Category { get; set; }

        public int Number { get; set; }
    }
}
