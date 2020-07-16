using FilterBuilder.Filter;
using SimpleFilterBuilder.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace SimpleFilterBuilder.Controls.FileManager
{

    class FileManagerCtrlViewModel : INotifyPropertyChanged, IEnumerable<FilterBuilder.Filter.FileInfo>
    {
        // ------- INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }


        public delegate void OnChangeDgt(string val);
        public event OnChangeDgt OnChange;

        //----------------------------------------------------------------------
        private bool m_isEnable;
        private string[] m_fileExList;

        private string m_currentFileStr;
        private string m_filterStr;
        private string m_currentFolderStr = "C:\\";
        private FilterBuilder.Filter.FileInfo m_fileInfo;

        private List<string> m_filesInFolder = new List<string>();
        public static void FileSearch(string folder, List<string> outStr, string[] exList)
        {
            foreach (var file in Directory.GetFiles(folder))
            {
                if (exList != null)
                {
                    foreach (var ex in exList)
                        if (file.IndexOf(ex) >= 0)
                        {
                            outStr.Add(file);
                            break;
                        }
                }
                else outStr.Add(file);
            }

            try
            {
                foreach (var dict in Directory.GetDirectories(folder))
                    FileSearch(dict, outStr, exList);
            } catch { }
        }




        //--------------------------------------------------------------------
        public bool IsEnable
        {
            get { return m_isEnable; }
            set { m_isEnable = value; OnPropertyChanged("IsEnable"); }
        }
        public string FilterStr
        {
            get { return m_filterStr; }
            set {
                m_filterStr = value;                
                OnPropertyChanged("FilterStr");
                UpdateFileInfo();
            }
        }
        public string CurrentFileStr
        {
            get { return m_currentFileStr; }
            set {
                m_currentFileStr = value;
                OnPropertyChanged("CurrentFileStr");
            }
        }
        public string CurrentFolderStr
        {
            get { return m_currentFolderStr; }
            set { m_currentFolderStr = value; OnPropertyChanged("CurrentFolderStr"); }
        }
        public FilterBuilder.Filter.FileInfo CurrentFileInfo
        {
            get { return m_fileInfo; }
            set
            {
                if (value != null)
                {
                    m_fileInfo = value;
                    CurrentFileStr = m_fileInfo.FileName;

                    if (OnChange != null)
                        OnChange(CurrentFileStr);
                }
                OnPropertyChanged("CurrentFileInfo");
            }
        }




        public ObservableCollection<FilterBuilder.Filter.FileInfo> FileListInfo { get; set; } = new ObservableCollection<FilterBuilder.Filter.FileInfo>();

        private void UpdateFileInfo()
        {
            CurrentFileInfo = null;
            FileListInfo.Clear();
            foreach (var file in m_filesInFolder)
            {
                if ((FilterStr?.Length == 0) ||  (file.IndexOf(FilterStr)>=0))
                    FileListInfo.Add(new FilterBuilder.Filter.FileInfo(file));
            }
        }

        private async void UpdateFileList()
        {
            try
            {
                m_filesInFolder.Clear();

                IsEnable = false;
                await Task.Run(() => { FileSearch(CurrentFolderStr, m_filesInFolder, m_fileExList); });
                IsEnable = true;

                UpdateFileInfo();
            } catch (Exception ex)  { Console.WriteLine(ex.Message); }
        }

        public IEnumerator<FilterBuilder.Filter.FileInfo> GetEnumerator()
        {
            return FileListInfo.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return FileListInfo.GetEnumerator();
        }

        public FileManagerCtrlViewModel(string[] exFiles = null)
        {
            FilterStr = "";
            IsEnable = true;
            CurrentFileInfo = new FilterBuilder.Filter.FileInfo("");
            m_fileExList = exFiles;
        }


        public ICommand SelectFolderCmd
        {
            get
            {
                return new RelayCommand(() => {
                    using (FolderBrowserDialog openFolderDialog = new FolderBrowserDialog())
                    {
                        DialogResult result = openFolderDialog.ShowDialog();
                        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(openFolderDialog.SelectedPath))
                        {
                            CurrentFolderStr = openFolderDialog.SelectedPath;
                            UpdateFileList();
                        }
                    }
                });
            }
        }
    }
}
