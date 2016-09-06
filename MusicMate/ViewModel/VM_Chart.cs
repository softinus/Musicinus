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
        public static ObservableCollection<TestClass> Anal1 { get; private set; }

        public VM_Chart()
        {
            Anal1 = new ObservableCollection<TestClass>();
            Anal1.Add(new TestClass() { Category = "Globalization", Number = 75 });
            Anal1.Add(new TestClass() { Category = "Features", Number = 2 });
            Anal1.Add(new TestClass() { Category = "ContentTypes", Number = 12 });
            Anal1.Add(new TestClass() { Category = "Correctness", Number = 83 });
            Anal1.Add(new TestClass() { Category = "Best Practices", Number = 29 });
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
    public class TestClass
    {
        public string Category { get; set; }

        public int Number { get; set; }
    }
}
