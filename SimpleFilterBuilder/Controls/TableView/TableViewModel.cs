using FilterBuilder.Filter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFilterBuilder.Controls.TableView
{

    public class TableValueModel : INotifyPropertyChanged
    {
        // ------- INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public delegate void OnChangeDgt(int row, int column, double value);
        public event OnChangeDgt OnChange;

        private double m_value;
        public double Value
        {
            get { return m_value; }
            set
            {
                m_value = value;
                if (OnChange != null)
                    OnChange(Row, Column, value);
                OnPropertyChanged("Value");
            }
        }



        public int Row { get; set; }
        public int Column { get; set; }    
    }

    public class TableViewModel : INotifyPropertyChanged
    {
        // ------- INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }


        public delegate void OnChangeDgt(int row, int column, double value, bool bInvalidateAll);
        public event OnChangeDgt OnChange;

        // members --------------
        private SliderPlusViewModel<double> m_floatSlider;
        public SliderPlusViewModel<double> FloatSlider
        {
            get { return m_floatSlider; }
            set { m_floatSlider = value; OnPropertyChanged("FloatSlider"); }
        }


        private string m_title;
        private int m_tableRows;
        private int m_tableColumns;

        public string Title
        {
            get { return m_title; }
            set { m_title = value; OnPropertyChanged("Title"); }
        }
        public int TableRows
        {
            get { return m_tableRows; }
            set { m_tableRows = value; OnPropertyChanged("TableRows"); }
        }
        public int TableColumns
        {
            get { return m_tableColumns; }
            set { m_tableColumns = value; OnPropertyChanged("TableColumns"); }
        }

        public ObservableCollection<TableValueModel> TextBoxTable { get; set; } = new ObservableCollection<TableValueModel>();

        private bool bInvalidateAll = true;

        public TableViewModel(string title, TableProperty<double> table)
        {
            Title = title;
            TableRows = table.Rows;
            TableColumns = table.Columns;

            for (int r = 0; r < table.Rows; r++)
                for (int c = 0; c < table.Columns; c++)
                {
                    var valueModel = new TableValueModel() { Row = r, Column = c, Value = table[r, c] };
                    valueModel.OnChange += (int row, int column, double value) => {
                        if (OnChange != null)
                            OnChange(row, column, value, bInvalidateAll);
                    };
                    TextBoxTable.Add(valueModel);
                }

            FloatSlider = new SliderPlusViewModel<double>("SetAll", -10, 10, 0.1);
            FloatSlider.OnChange += FloatSlider_OnChange;
        }

        private void FloatSlider_OnChange(double val)
        {
            bInvalidateAll = false;
            foreach (var textBox in TextBoxTable)
                textBox.Value = val;
            bInvalidateAll = true;
            TextBoxTable[0].Value = val;
        }
    }
}
