using SimpleFilterBuilder.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SimpleFilterBuilder.Controls
{
    public class SliderPlusViewModel<T>: INotifyPropertyChanged
        where T : IComparable<T>
    {
        // ------- INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        private string m_headText;
        private T m_sliderVal;
        private T m_minVal;
        private T m_maxVal;
        private T m_step;

        public delegate void OnChangeDgt(T val);
        public event OnChangeDgt OnChange;

        public string HeadText
        {
            get { return m_headText; }
            set
            {
                m_headText = value;
                OnPropertyChanged("HeadText");
            }
        }  
        public T SliderVal
        {
            get { return m_sliderVal; }
            set
            {
                m_sliderVal = value;
                if (m_sliderVal.CompareTo(value) < 0)
                    m_sliderVal = MaxVal;

                if (MinVal.CompareTo(value) > 0)
                    m_sliderVal = MinVal;

                if (OnChange != null)
                    OnChange(m_sliderVal);
                OnPropertyChanged("SliderVal");
            }
        }

        public T MinVal
        {
            get { return m_minVal; }
            set
            {
                m_minVal = value;
                OnPropertyChanged("MinVal");
            }
        }
        public T MaxVal
        {
            get { return m_maxVal; }
            set
            {
                m_maxVal = value;
                OnPropertyChanged("MaxVal");
            }
        }

        public SliderPlusViewModel(string nameStr, T min, T max, T step )
        {
            HeadText = nameStr;
            m_step = step;
            MinVal = min;
            MaxVal = max;            
        }

        public ICommand Inc
        {
            get
            {
                return new RelayCommand(() => {

                    dynamic a = SliderVal;
                    dynamic b = m_step;
                    SliderVal = a+b;                    
                });
            }
        }
        public ICommand Dec
        {
            get
            {
                return new RelayCommand(() => {
                    dynamic a = SliderVal;
                    dynamic b = m_step;
                    SliderVal = a - b;
                });
            }
        }
    }
}
