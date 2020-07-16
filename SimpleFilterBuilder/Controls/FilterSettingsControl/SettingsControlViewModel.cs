using FilterBuilder.Filter;
using SimpleFilterBuilder.Controls.FilterPropertyCtrl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SimpleFilterBuilder.Controls.FilterSettingsControl
{
    class SettingsControlViewModel : INotifyPropertyChanged
    {
        public BaseFilter Filter { get; private set; }

        // ------- INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        // Members ==================================================================
        private string m_filterType;


        // Property =================================================================
        public string FilterType
        {
            get { return m_filterType; }
            set { m_filterType = value; OnPropertyChanged("FilterType");  }
        }

        public ObservableCollection<PropertyCtrlViewModel> PropertyList { get; set; } = new ObservableCollection<PropertyCtrlViewModel>();

        public delegate void OnUpdateImgDlgt(BaseFilter filter, PropertyCtrlViewModel viewModel);
        public event OnUpdateImgDlgt OnUpdate;


        public SettingsControlViewModel(BaseFilter filter)
        {
            if (filter == null)
                return;

            Filter = filter;
            FilterType = filter.Type.ToString();

            foreach (var prop in filter.IntPropertys)
            {
                var propCtrl = new PropertyCtrlViewModel(prop.Key, prop.Value);
                propCtrl.OnUpdateImg += PropCtrl_OnUpdateImg;
                PropertyList.Add(propCtrl);
            }

            foreach (var prop in filter.FloatPropertys)
            {
                var propCtrl = new PropertyCtrlViewModel(prop.Key, prop.Value);
                propCtrl.OnUpdateImg += PropCtrl_OnUpdateImg;
                PropertyList.Add(propCtrl);
            }

            foreach (var prop in filter.EnumPropertys)
            {
                var propCtrl = new PropertyCtrlViewModel(prop.Key, prop.Value);
                propCtrl.OnUpdateImg += PropCtrl_OnUpdateImg;
                PropertyList.Add(propCtrl);
            }

            foreach (var prop in filter.FilePropertys)
            {
                var propCtrl = new PropertyCtrlViewModel(prop.Key, prop.Value);
                propCtrl.OnUpdateImg += PropCtrl_OnUpdateImg;
                PropertyList.Add(propCtrl);
            }


            foreach (var prop in filter.TablePropertys)
            {
                var propCtrl = new PropertyCtrlViewModel(prop.Key, prop.Value);
                propCtrl.OnUpdateImg += PropCtrl_OnUpdateImg;
                PropertyList.Add(propCtrl);
            }
        }


        private void PropCtrl_OnUpdateImg(PropertyCtrlViewModel viewModel)
        {
            if (OnUpdate != null)
                OnUpdate(Filter, viewModel);
        }
    }
}
