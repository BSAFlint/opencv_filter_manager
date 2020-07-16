using SimpleFilterBuilder.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Serialization;

namespace SimpleFilterBuilder.Dialogs.ExportDlg
{

    public class ExportMapItem
    {
        public int Lvl { get; set; }
        public string FileMask { get; set; }
        public string ToSubFolder { get; set; }
    }


    [Serializable]
    public class ExportSettings
    {
        public string Format { get; set; }
        public enum InfoToFileNameMode
        {
            NUMBER = 0,
            OLD_NAME = 1,
            OLD_PATH_TO_NAME = 2
        }

        public InfoToFileNameMode InfoMode { get; set; }
        

        public string ResultFolder { get; set; }
        public List<ExportMapItem> ExportMap { get; set; } = new List<ExportMapItem>();

        public void ToDefault()
        {
            InfoMode = InfoToFileNameMode.NUMBER;
            Format = "jpg";
            ResultFolder = "Export";
            ExportMap.Add(new ExportMapItem() { Lvl = 0, FileMask = "Mask", ToSubFolder = "to folder" });
        }

        public int GetMaxLeveOfExportMap()
        {
            int maxLvl = 0;
            foreach (var el in ExportMap)
                if (el.Lvl > maxLvl)
                    maxLvl = el.Lvl;
            return maxLvl;
        }


        // пока не проверял
        public string GetFolder(string fileName)
        {
            string outFolder = ResultFolder;

            for (int nLvl=0; nLvl <= GetMaxLeveOfExportMap(); nLvl++ )
                foreach(var el in ExportMap)
                    if (el.Lvl == nLvl)
                    {
                        int nIdx = fileName.IndexOf(el.FileMask);
                        if (nIdx>=0)
                        {
                            outFolder += "\\" + el.ToSubFolder;
                            fileName = fileName.Substring(nIdx + el.FileMask.Length);
                            break; // lvl ++
                        }
                    }
            return outFolder;
        }

        public string GetFileName(string fileName)
        {
            switch (InfoMode)
            {
                case InfoToFileNameMode.NUMBER:
                    return null;

                case InfoToFileNameMode.OLD_NAME:
                    int lastSlash = fileName.LastIndexOf('\\');
                    if (lastSlash >= 0)
                        return fileName.Substring(lastSlash + 1);
                    return fileName;

                case InfoToFileNameMode.OLD_PATH_TO_NAME:
                    return fileName.Replace('\\', '_');
            }

            return null;
        }
    }


    public class ExportDlgModel : INotifyPropertyChanged
    {
        private string m_settingsFilename;
        private Window m_selfPtr;
        public ExportSettings Result;

        //-------------------------
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public string ResultFolder
        {
            get { return Result.ResultFolder; }
            set { Result.ResultFolder = value; OnPropertyChanged("ResultFolder"); }
        }
        public ObservableCollection<ExportMapItem> ExportMap { get; set; } = new ObservableCollection<ExportMapItem>();


        private ComboBoxItem m_selectedMode;
        private ComboBoxItem m_selecteFormat;


        public ComboBoxItem SelectedMode
        {
            get { return m_selectedMode; }
            set { m_selectedMode = value; OnPropertyChanged("SelectedMode"); }
        }
        public ComboBoxItem SelecteFormat
        {
            get { return m_selecteFormat; }
            set { m_selecteFormat = value; OnPropertyChanged("SelecteFormat"); }
        }


        public ObservableCollection<ComboBoxItem> InfoModeList { get; set; } = new ObservableCollection<ComboBoxItem>();
        public ObservableCollection<ComboBoxItem> FormatList { get; set; } = new ObservableCollection<ComboBoxItem>();

        public void Load(string filename)
        {
            XmlSerializer formatter = new XmlSerializer(typeof(ExportSettings));
            try
            {
                using (FileStream fs = new FileStream(filename, FileMode.Open))
                {
                    Result = (ExportSettings)formatter.Deserialize(fs);
                }
            }
            catch (Exception ex)
            {
                Result = new ExportSettings();
                Result.ToDefault();
            }
        }

        public void Save(string filename)
        {
            XmlSerializer formatter = new XmlSerializer(typeof(ExportSettings));
            try
            {
                if (File.Exists(filename)) // без удаления файл может ломаться, т.к. хвост не затирается
                    File.Delete(filename);

                using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate))
                    formatter.Serialize(fs, Result);
            }
            catch { }
        }

        public ExportDlgModel(Window self, string settingsFilename)
        {
            m_selfPtr = self;
            m_settingsFilename = settingsFilename;
            Load(m_settingsFilename);

            foreach (var item in Result.ExportMap)
                ExportMap.Add(item);

            var modes = Enum.GetValues(typeof(ExportSettings.InfoToFileNameMode));
            foreach (var mode in modes)
            {
                var ob = new ComboBoxItem() { Content = mode };
                InfoModeList.Add(ob);                
                if ((ExportSettings.InfoToFileNameMode)ob.Content == Result.InfoMode)
                    SelectedMode = ob;
            }


            string[] formats = new string[] { "jpg", "png", "bmp" };
            foreach(var form in formats)
            {
                var ob = new ComboBoxItem() { Content = form };
                FormatList.Add(ob);
                if (form == Result.Format)
                    SelecteFormat = ob;
            }
        }


        public ICommand FolderResultCmd
        {
            get
            {
                return new RelayCommand(() => {
                    using (FolderBrowserDialog openFolderDialog = new FolderBrowserDialog())
                    {
                        DialogResult result = openFolderDialog.ShowDialog();
                        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(openFolderDialog.SelectedPath))
                        {
                            ResultFolder = openFolderDialog.SelectedPath;
                        }
                    }
                });
            }
        }
        public ICommand AddCmd
        {
            get
            {
                return new RelayCommand(() => {
                    ExportMap.Add(new ExportMapItem() { Lvl = 0, FileMask = "Mask", ToSubFolder = "to folder" });
                });
            }
        }
        public ICommand RemoveCmd
        {
            get
            {
                return new RelayCommand(() => {
                    if (ExportMap.Count > 1)
                        ExportMap.RemoveAt(ExportMap.Count - 1);
                });
            }
        }
        public ICommand ClearCmd
        {
            get
            {
                return new RelayCommand(() => {
                    ExportMap.Clear();
                });
            }
        }
        public ICommand CancelCmd
        {
            get
            {
                return new RelayCommand(() => {
                    m_selfPtr.DialogResult = false;
                    m_selfPtr.Close();
                });
            }
        }
        public ICommand StartCmd
        {
            get
            {
                return new RelayCommand(() => {

                    Result.ExportMap.Clear();
                    foreach (var item in ExportMap)
                        Result.ExportMap.Add(item);

                    Result.InfoMode = (ExportSettings.InfoToFileNameMode)SelectedMode.Content;
                    Result.Format = (string)SelecteFormat.Content;

                    m_selfPtr.DialogResult = true;
                    Save(m_settingsFilename);
                    m_selfPtr.Close();
                });
            }
        }
    }
}
