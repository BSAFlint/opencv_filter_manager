using SimpleFilterBuilder.Common;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SimpleFilterBuilder.Controls.SyncButton
{
    /// <summary>
    /// Можно управлять Enabled-свойством кнопки в отдельном обработчике
    /// Можно управлять тектсом 
    /// </summary>
    public class SyncButtonViewModel : INotifyPropertyChanged
    {
        public delegate void PressDelegate(SyncButtonViewModel button);
        public PressDelegate PressCmd { get; set; } = (SyncButtonViewModel button) => { };


        // ------- INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        private bool m_enabled;
        public bool Enabled
        {
            get { return m_enabled; }
            set
            {
                m_enabled = value;
                OnPropertyChanged("Enabled");
            }
        }

        private string m_text;
        public string Text
        {
            get { return m_text; }
            set
            {
                m_text = value;
                OnPropertyChanged("Text");
            }
        }

        public SyncButtonViewModel()
        {
            Enabled = true;
        }
        public ICommand ButtonPressCmd
        {
            get
            {
                return new RelayCommand(() => {
                    PressCmd(this);
                });
            }
        }
    }

}
