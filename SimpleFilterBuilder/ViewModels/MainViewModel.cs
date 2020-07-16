using Emgu.CV;
using FilterBuilder.Common;
using FilterBuilder.Filter;
using SimpleFilterBuilder.Common;
using SimpleFilterBuilder.Controls;
using SimpleFilterBuilder.Controls.FileManager;
using SimpleFilterBuilder.Controls.FilterPropertyCtrl;
using SimpleFilterBuilder.Controls.FilterSettingsControl;
using SimpleFilterBuilder.Controls.SyncButton;
using SimpleFilterBuilder.Dialogs.ExportDlg;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SimpleFilterBuilder.ViewModels
{
    class MainViewModel : INotifyPropertyChanged
    {
        // ------- INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        private int m_imgSourceID = 0;
        public int ImgSourceID
        {
            get { return m_imgSourceID; }
            set { m_imgSourceID = value; OnPropertyChanged("ImgSourceID"); }
        }

        // ------------------------------------------

        private SettingsControlViewModel m_filterSettingsCtrl;
        public SettingsControlViewModel FilterSettingsCtrl
        {
            get { return m_filterSettingsCtrl; }
            set { m_filterSettingsCtrl = value; OnPropertyChanged("FilterSettingsCtrl"); }
        }

        // progress bar
        private int m_progressBarValue;
        private string m_progressBarInfo;


        public int ProgressBarValue
        {
            get { return m_progressBarValue; }
            set { m_progressBarValue = value; OnPropertyChanged("ProgressBarValue"); }
        }

        public string ProgressBarInfo
        {
            get { return m_progressBarInfo; }
            set { m_progressBarInfo = value; OnPropertyChanged("ProgressBarInfo"); }
        }



        //----------------------------
        private string m_imageInfo = "Image";
        private BitmapImage m_imageSource;

        public BitmapImage ImageSource
        {
            get { return m_imageSource; }
            set { m_imageSource = value; OnPropertyChanged("ImageSource"); }
        }

        public string ImageInfo
        {
            get { return m_imageInfo; }
            set { m_imageInfo = value; OnPropertyChanged("ImageInfo"); }
        }

        public void SetImageSource(Bitmap bitmap, string info)
        {
            if (bitmap == null)
                return;

            BitmapImage bitmapImage = new BitmapImage();

            using (MemoryStream memory = new MemoryStream())
            {
                try
                {
                    bitmap.Save(memory, ImageFormat.Bmp);
                    memory.Position = 0;
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                }
                catch { }
            }

            if (info.Length > 60)
                info = "..." + info.Substring(info.Length - 60);

            ImageInfo = info;
            ImageSource = bitmapImage;
        }



        //----------------------------
        private ComboBoxItem m_selectAddFilter;
        public ObservableCollection<ComboBoxItem> FilterToAdd { get; set; } = new ObservableCollection<ComboBoxItem>();
        public ComboBoxItem SelectAddFilter
        {
            get { return m_selectAddFilter; }
            set { m_selectAddFilter = value; OnPropertyChanged("SelectAddFilter"); }
        }

        //----------------------------
        private BaseFilter m_selectedFilter;
        public ObservableCollection<BaseFilter> FilterSequence { get; set; } = new ObservableCollection<BaseFilter>();        
        public BaseFilter SelectedFilter
        {
            get { return m_selectedFilter; }
            set
            {

                FilterSettingsCtrl = new SettingsControlViewModel(value);
                FilterSettingsCtrl.OnUpdate += FilterSettingsCtrl_OnUpdate;

                m_selectedFilter = value;
                OnPropertyChanged("SelectedFilter");
            }
        }


        //----------------------------
        public FileManagerCtrlViewModel FileManager { get; set; } = new FileManagerCtrlViewModel(new string[] { "jpg", "png", "bmp" });

        //----------------------------
        private BaseFilter LoadFilter;
        public GraphFilter graphFilter = new GraphFilter();

        
        public void ShowGraphFilterResult(int imgID)
        {
            long msec = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            List<DataSrc> dataSrcs = graphFilter.GetOuts();
            long msec2 = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            ProgressBarInfo = "Time:" + (msec2 - msec) + " (ms)";


            if (dataSrcs.Count==0)
            {
                Console.WriteLine("Filter error");
                return;
            }

            imgID = imgID % dataSrcs.Count;
            var bitmap = OpenCVHelper.GetRGBBitmapFromCvMat(dataSrcs[imgID].Image);

            if (bitmap == null)
            {
                Console.WriteLine("read error");
                return;
            }

            SetImageSource(bitmap, dataSrcs[imgID].Info);
        }


        // стоит процессинг бар запилить
        private bool m_exportInProgress = false;
        public void ExportGraphFilterResult(int nId, string fileName, ExportSettings exportSettings)
        {
            BaseFilter mainFileFilter = graphFilter.FindFirtsFilter(FilterType.File);
            if (mainFileFilter != null)
            {
                mainFileFilter["Path"] = new FilterBuilder.Filter.FileInfo(fileName);
                List<DataSrc> dataSrcs = graphFilter.GetOuts();
                if (dataSrcs.Count == 0)
                {
                    // todo
                    return;
                }

                for(int nSrcId = 0; nSrcId< dataSrcs.Count; nSrcId++)
                {

                    string filename = exportSettings.GetFileName(dataSrcs[nSrcId].Info);
                    if (filename == null)
                        filename = nSrcId.ToString() +"." + exportSettings.Format;

                    string ToFolder = exportSettings.GetFolder(dataSrcs[nSrcId].Info);
                    ToFolder += "\\" + nId + "\\";

                    filename = ToFolder + filename;

                    try
                    {
                        if (!Directory.Exists(ToFolder))
                            Directory.CreateDirectory(ToFolder);


                        ImageFormat useFormat = ImageFormat.Jpeg;
                        switch(exportSettings.Format)
                        {
                            case "jpg": useFormat = ImageFormat.Jpeg; break;
                            case "png": useFormat = ImageFormat.Png; break;
                            case "bmp": useFormat = ImageFormat.Bmp; break;
                        }

                        Bitmap bitmap = OpenCVHelper.GetRGBBitmapFromCvMat(dataSrcs[nSrcId].Image);
                        bitmap.Save(filename, useFormat);

                    } catch { }
                }
            }
        }


        public SyncButtonViewModel ExportButton { get; set; }


        public MainViewModel()
        {
            ProgressBarValue = 0;
            ProgressBarInfo = "";


            FileManager.OnChange += FileMng_OnChange;

            foreach (var filterName in GraphFilter.GetFilterNames())
                FilterToAdd.Add(new ComboBoxItem() { Content = filterName });
            SelectAddFilter = FilterToAdd[0];

            var bitmap = new Bitmap(@"Res\default.png");
            SetImageSource(bitmap, "default.png");

            LoadFilter = graphFilter.Add(FilterType.File, "MainFile");
            FilterSequence.Add(LoadFilter);

            bitmap.Dispose();


            ExportButton = new SyncButtonViewModel()
            {
                Text = "Export",
                Enabled = true,
                PressCmd = async (SyncButtonViewModel button) =>
                {

                    if (!m_exportInProgress)
                    {
                        button.Enabled = false;
                        ExportDlgView dlg = new ExportDlgView();
                        ExportDlgModel dlgModel = new ExportDlgModel(dlg, "export_settigs.xml");
                        dlg.DataContext = dlgModel;
                        var res = dlg.ShowDialog();

                        button.Enabled = true;

                        if (res == true)
                        {
                            m_exportInProgress = true;
                            button.Text = "Cancel";

                            ExportSettings exportSettings = dlgModel.Result;


                            await Task.Run(() =>
                            {
                                int totalCnt = FileManager.FileListInfo.Count;
                                for (int infoId = 0; m_exportInProgress && (infoId < totalCnt); infoId++)
                                {
                                    string infoStr = FileManager.FileListInfo[infoId].Info;

                                    App.Current.Dispatcher.Invoke(() =>
                                    {
                                        ProgressBarValue = (100 * infoId) / totalCnt;
                                        ProgressBarInfo = infoStr;
                                    });

                                    ExportGraphFilterResult(infoId, infoStr, exportSettings);
                                }
                            });

                            ProgressBarValue = 0;
                            m_exportInProgress = false;
                            button.Text = "Export";
                        }
                    }
                    else {
                        m_exportInProgress = false;
                        ProgressBarValue = 0;
                    }
                }
            };

        }

        private void FileMng_OnChange(string val)
        {
            Console.WriteLine(val);
            BaseFilter mainFileFilter = graphFilter.FindFirtsFilter(FilterType.File);
            if (mainFileFilter != null) {
                mainFileFilter["Path"] = new FilterBuilder.Filter.FileInfo(val);
                ShowGraphFilterResult(ImgSourceID);
            }
        }

        private void FilterSettingsCtrl_OnUpdate(BaseFilter filter, PropertyCtrlViewModel viewModel)
        {

            Console.WriteLine(filter.GetPropertyStr());
            filter.Invalidate(true);
            ShowGraphFilterResult(ImgSourceID);
        }



        public ICommand AddFilter
        {
            get
            {
                return new RelayCommand(() => {
                    BaseFilter tail = FilterSequence[FilterSequence.Count - 1];

                    string nameAddFilter = SelectAddFilter.Content.ToString();
                    var filter = graphFilter.Add(nameAddFilter);

                    if (filter==null)
                        return;

                    tail.ConnectNext(filter);
                    FilterSequence.Add(filter);
                                      
                    LoadFilter.Invalidate(false);
                    ShowGraphFilterResult(ImgSourceID);
                });
            }
        }
        public ICommand DelFilter
        {
            get
            {
                return new RelayCommand(() => {

                    if (FilterSequence.Count <=1)
                        return;

                    BaseFilter removeFilter = FilterSequence[FilterSequence.Count - 1];
                    graphFilter.RemoveFromTail(removeFilter);
                    FilterSequence.Remove(removeFilter);

                    LoadFilter.Invalidate(false);
                    ShowGraphFilterResult(ImgSourceID);
                });
            }
        }
        public ICommand IncSourceID
        {
            get
            {
                return new RelayCommand(() => {
                    ImgSourceID += 1;
                    ShowGraphFilterResult(ImgSourceID);
                });
            }
        }
        public ICommand DecSourceID
        {
            get
            {
                return new RelayCommand(() => {
                    ImgSourceID -= 1;
                    if (ImgSourceID < 0)
                        ImgSourceID = 0;

                    ShowGraphFilterResult(ImgSourceID);
                });
            }
        }
    }
}
