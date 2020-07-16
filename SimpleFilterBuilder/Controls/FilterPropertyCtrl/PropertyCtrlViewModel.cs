using FilterBuilder.Filter;
using SimpleFilterBuilder.Controls.FileManager;
using SimpleFilterBuilder.Controls.TableView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SimpleFilterBuilder.Controls.FilterPropertyCtrl
{

    /*
     * Как меняет занчение в самом property переданному по ссылке из фильтра
     * Так и шлёт уведомление наверх об его изменении
     * Это событие нужно передать грау, чтобы тот обновил картинку для тех фильтров, на которых эти property были изменены
    */


    class PropertyCtrlViewModel : INotifyPropertyChanged
    {
        // ------- INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        // Members ===================================================================
        // Sliders
        private Visibility m_intSliderShow;
        private Visibility m_floatSliderShow;
        private SliderPlusViewModel<int> m_intSlider;
        private SliderPlusViewModel<double> m_floatSlider;

        // ComboBox
        private Visibility m_comboBoxShow;
        private string m_enumNameStr;
        private ComboBoxItem m_selectEnuml;

        private delegate void OnUpdateEnumDlgt(string content);
        private event OnUpdateEnumDlgt OnUpdateEnum;


        // FileManager
        private Visibility m_fileManagerShow;
        private FileManagerCtrlViewModel m_fileManagerCtrl;

        // TableView
        private Visibility m_tableViewShow;
        private TableViewModel m_tableViewCtrl;

        // Property =====================================================================
        public string Name { get; private set; }
        public Visibility IntSliderShow
        {
            get { return m_intSliderShow; }
            set { m_intSliderShow = value; OnPropertyChanged("IntSliderShow"); }
        }
        public Visibility FloatSliderShow
        {
            get { return m_floatSliderShow; }
            set { m_floatSliderShow = value; OnPropertyChanged("FloatSliderShow"); }
        }
        public SliderPlusViewModel<int> IntSlider
        {
            get { return m_intSlider; }
            set { m_intSlider = value; OnPropertyChanged("IntSlider"); }
        }
        public SliderPlusViewModel<double> FloatSlider
        {
            get { return m_floatSlider; }
            set { m_floatSlider = value; OnPropertyChanged("FloatSlider"); }
        }
        public Visibility ComboBoxShow
        {
            get { return m_comboBoxShow; }
            set { m_comboBoxShow = value; OnPropertyChanged("ComboBoxShow"); }
        }
        public string EnumNameStr
        {
            get { return m_enumNameStr; }
            set { m_enumNameStr = value; OnPropertyChanged("EnumNameStr"); }
        }
        public ComboBoxItem SelectEnum
        {
            get { return m_selectEnuml; }
            set
            {
                m_selectEnuml = value;
                if (OnUpdateEnum != null)
                    OnUpdateEnum(m_selectEnuml.Content.ToString());
                OnPropertyChanged("SelectEnum");
            }
        }
        public ObservableCollection<ComboBoxItem> Enums { get; set; } = new ObservableCollection<ComboBoxItem>();

        public Visibility FileManagerShow
        {
            get { return m_fileManagerShow; }
            set { m_fileManagerShow = value; OnPropertyChanged("FileManagerShow"); }
        }
        public FileManagerCtrlViewModel FileManagerCtrl
        {
            get { return m_fileManagerCtrl; }
            set { m_fileManagerCtrl = value; OnPropertyChanged("FileManagerCtrl"); }
        }

        public Visibility TableViewShow
        {
            get { return m_tableViewShow; }
            set { m_tableViewShow = value; OnPropertyChanged("TableViewShow"); }
        }
        public TableViewModel TableViewCtrl
        {
            get { return m_tableViewCtrl; }
            set { m_tableViewCtrl = value; OnPropertyChanged("TableViewCtrl"); }
        }

        // ============================================================================

        public delegate void OnUpdateImgDlgt(PropertyCtrlViewModel viewModel);
        public event OnUpdateImgDlgt OnUpdateImg;
        public PropertyCtrlViewModel(string name, NumProperty<int> property)
        {
            Name = name;
            IntSliderShow = Visibility.Visible;
            FloatSliderShow = Visibility.Collapsed;
            ComboBoxShow = Visibility.Collapsed;
            FileManagerShow = Visibility.Collapsed;
            TableViewShow = Visibility.Collapsed;

            IntSlider = new SliderPlusViewModel<int>(Name, property.MinVal, property.MaxVal, 1);
            IntSlider.SliderVal = property.Value;

            IntSlider.OnChange += (int value) =>
            {
                property.Value = value;

                if (OnUpdateImg != null)
                    OnUpdateImg(this);
            };
        }
        public PropertyCtrlViewModel(string name, NumProperty<double> property)
        {
            Name = name;
            IntSliderShow = Visibility.Collapsed;
            FloatSliderShow = Visibility.Visible;
            ComboBoxShow = Visibility.Collapsed;
            FileManagerShow = Visibility.Collapsed;
            TableViewShow = Visibility.Collapsed;

            FloatSlider = new SliderPlusViewModel<double>(Name, property.MinVal, property.MaxVal, 1);
            FloatSlider.SliderVal = property.Value;

            FloatSlider.OnChange += (double value) =>
            {
                property.Value = value;
                if (OnUpdateImg != null)
                    OnUpdateImg(this);
            };
        }
        public PropertyCtrlViewModel(string name, EnumProperty property)
        {
            Name = name;
            IntSliderShow = Visibility.Collapsed;
            FloatSliderShow = Visibility.Collapsed;
            ComboBoxShow = Visibility.Visible;
            FileManagerShow = Visibility.Collapsed;
            TableViewShow = Visibility.Collapsed;

            EnumNameStr = Name;
            foreach (var el in property.Values)
                Enums.Add(new ComboBoxItem() { Content = el.Key });

            if (Enums.Count > 0)
            {
                SelectEnum = Enums[0];
                foreach (var en in Enums)
                    if (en.Content.ToString() == property.ToString())
                    {
                        SelectEnum = en;
                        break;
                    }
            }


            OnUpdateEnum += (string content) =>
            {
                property.Set(content);
                if (OnUpdateImg != null)
                    OnUpdateImg(this);
            };
        }

        public PropertyCtrlViewModel(string name, FileInfo property)
        {
            Name = name;
            IntSliderShow = Visibility.Collapsed;
            FloatSliderShow = Visibility.Collapsed;
            ComboBoxShow = Visibility.Collapsed;
            FileManagerShow = Visibility.Visible;
            TableViewShow = Visibility.Collapsed;

            FileManagerCtrl = new FileManagerCtrlViewModel(new string[] { "jpg", "png", "bmp" });
            FileManagerCtrl.CurrentFileInfo.FileName = property.FileName;
            FileManagerCtrl.CurrentFileInfo.Info = property.FileName;

            FileManagerCtrl.OnChange += (string val) =>
            {
                property.FileName = val;
                property.Info = val;
                if (OnUpdateImg != null)
                    OnUpdateImg(this);
            };
        }

        public PropertyCtrlViewModel(string name, TableProperty<double> table)
        {
            Name = name;
            IntSliderShow = Visibility.Collapsed;
            FloatSliderShow = Visibility.Collapsed;
            ComboBoxShow = Visibility.Collapsed;
            FileManagerShow = Visibility.Collapsed;
            TableViewShow = Visibility.Visible;

            TableViewCtrl = new TableViewModel(Name, table);

            TableViewCtrl.OnChange += (int row, int column, double value, bool bInvalidateAll) =>
            {
                table[row, column] = value;
                if (bInvalidateAll && (OnUpdateImg != null))
                    OnUpdateImg(this);
            };
        }
    }
}
