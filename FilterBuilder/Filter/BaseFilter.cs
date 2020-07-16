using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FilterBuilder.Common;
using FilterBuilder.Common.MovingPredict;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static FilterBuilder.Common.CvLUT;

namespace FilterBuilder.Filter
{

    public class DataSrc: IDisposable
    {
        private static Random rnd = new Random();

        public Mat Image { get; set; } = null;
        public string Info { get; set; }
        public DataSrc(Mat img, string info, bool isInfoRnd = true)
        {
            Image = img;
            Info = info;

            if (isInfoRnd)
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var str = new string(Enumerable.Repeat(chars, 10).Select(s => s[rnd.Next(s.Length)]).ToArray());
                Info += "_" + str;
            }
        }
        public void Dispose()
        {
            if (Image != null)
                Image.Dispose();
        }

        public DataSrc Clone()
        {
            return new DataSrc(this.Image.Clone(), this.Info, false);
        }
    }

    public enum FilterType
    {
        None = 0,
        Source = 1,
        BGRBlank = 2,
        File = 3,
        SourceSwipe = 4,
        Append = 5,
        Split = 6,
        Select = 7,
        Negative = 8,
        Merge = 9,
        MergeInSrc = 10,
        Bgr2Gray = 11,
        Gray2Bgr = 12,
        ResizeAbs = 13,
        Resize = 14,
        Rotation = 15,


        Blur = 16,
        Morphology = 17,
        Threshold = 18,
        Gradient = 19,

        Conv2D3K = 20,
        Conv2D5K = 21,
        Conv2D7K = 22,

        PixelSum = 30,
        Histogram = 31,

        ContourSearch = 32,
        ContourSort = 33,
        ContourAreaSelect = 34,
        ContourSingleSelect = 35,
        ContourAraund = 36,
        ContourShift = 37,

        ContourMaskMerge = 39,

        AddRect = 40,
        RectShift = 41,
        RectExport = 42,
        CropRect = 43,

        LUT = 50,
    }

    public enum FilterDrawMode
    {
        Show = 0,
        Hide = 1
    }

    public enum FilterLogicMode
    {
        On = 0,
        Off = 1
    }

    public enum SourceDrawModeEnum
    {
        Self = 0,
        None = 1,
        DrawSource = 2,
    }

    public class FileInfo
    {
        public string FileName { get; set; }
        public string Info { get; set; }

        public FileInfo(string filename)
        {
            FileName = filename;
            Info = filename; // todo
        }
        public override string ToString()
        {
            return Info;
        }
    }

    public abstract class BaseFilter
    {
        public GraphFilter Graph { get; private set; }
        public FilterType Type { get; set; } = FilterType.None;

        private static Random rnd = new Random();
        private string m_name = null;       
        public string Name
        {
            get
            {
                if (m_name==null)
                {
                    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                    m_name = new string(Enumerable.Repeat(chars, 10).Select(s => s[rnd.Next(s.Length)]).ToArray());                    
                }
                return Type + m_name;
            }
        }

        public DataType SourceType { get; set; }
        public DataType DestinationType { get; set; }

        protected List<BaseFilter> Sources { get; set; }
        protected List<BaseFilter> Children { get; set; }
        public int GetChildrenCnt { get { return Children.Count; } }

        //------------------------------------------------------------------------------------
        public Dictionary<string, NumProperty<int>> IntPropertys { get; private set; }
        public Dictionary<string, NumProperty<double>> FloatPropertys { get; private set; }
        public Dictionary<string, TableProperty<double>> TablePropertys { get; private set; }
        public Dictionary<string, EnumProperty> EnumPropertys { get; private set; }
        public Dictionary<string, FileInfo> FilePropertys { get; private set; }
        public Dictionary<string, object> DataPropertys { get; private set; } // используется для програмного проброса данных (битмап и.т.д.)
        public object this[string key]
        {
            set
            {
                if (IntPropertys.ContainsKey(key))
                {
                    IntPropertys[key].Value = (int)value;
                    Invalidate(true);
                    return;
                }

                if (FloatPropertys.ContainsKey(key))
                {
                    FloatPropertys[key].Value = (double)value;
                    Invalidate(true);
                    return;
                }

                if (TablePropertys.ContainsKey(key))
                {
                    TablePropertys[key] = (TableProperty<double>)value;
                    Invalidate(true);
                    return;
                }

                if (FilePropertys.ContainsKey(key))
                {
                    FilePropertys[key] = (FileInfo)value;
                    Invalidate(true);
                    return;
                }

                if (EnumPropertys.ContainsKey(key))
                {
                    EnumPropertys[key].Set((string)value);
                    Invalidate(true);
                    return;
                }

                if (DataPropertys.ContainsKey(key))
                {
                    DataPropertys[key] = value;
                    Invalidate(true);
                    return;
                }
            }
        }

        public void DisconnectSelfFromSource()
        {
            foreach(var source in Sources)
                source.Children.Remove(this);
        }

        public BaseFilter ConnectNext(BaseFilter filterNext)
        {
            Children.Add(filterNext);
            filterNext.Sources.Add(this);            
            return filterNext;
        }

        public BaseFilter ConnectBefore(BaseFilter filterBefore)
        {            
            Sources.Add(filterBefore);
            filterBefore.Children.Add(this);
            return this;
        }

        public BaseFilter(GraphFilter graph, string name = null )
        {
            this.m_name = name;
            this.Graph = graph;
            Sources = new List<BaseFilter>();
            Children = new List<BaseFilter>();

            IntPropertys = new Dictionary<string, NumProperty<int>>();
            FloatPropertys = new Dictionary<string, NumProperty<double>>();
            TablePropertys = new Dictionary<string, TableProperty<double>>();
            EnumPropertys = new Dictionary<string, EnumProperty>();
            FilePropertys = new Dictionary<string, FileInfo>();
            DataPropertys = new Dictionary<string, object>();

            m_out = new List<DataSrc>();
        }

        public string GetPropertyStr()
        {
            string ans = "";
            foreach (var nProp in IntPropertys)
            {
                NumProperty<int> prop = nProp.Value;                
                ans += nProp.Key + "=" + prop.Value.ToString() + "\n";
            }

            foreach (var nProp in FloatPropertys)
            {
                NumProperty<double> prop = nProp.Value;
                ans += nProp.Key + "=" + prop.Value.ToString() + "\n";
            }

            foreach (var nProp in TablePropertys)
            {
                ans += nProp.Key + ":\n" + nProp.ToString() + "\n";
            }

            foreach (var nProp in EnumPropertys)
            {
                EnumProperty prop = nProp.Value;
                ans += nProp.Key + "=" + prop.ToString() + "\n";
            }

            foreach (var nProp in FilePropertys)
                ans += nProp.Key + "= '" + nProp.Value + "'\n";

            foreach (var nProp in DataPropertys)
                ans += nProp.Key + "=" + nProp.Value.ToString() + "\n";

            return ans;
        }

        //------------------------------------------------------------------------------------
        protected List<DataSrc> m_out;

        public List<DataSrc> GetOut()
        {
            if (m_out.Count == 0)
                Do();

            return m_out;
        }

        // служебное значения, которое позволит понят о необходимости перечитывать какое-то данные внутри фильтра
        // Не все изменения своих же проперти требует их пересчёта,
        // но если ClearOut приходит от предка, то пересчёт требуется полностью
        // Созданно для возможности оптимизации вычислений
        // использовать ли этот параметр в логике каждого Do решай сам
        protected bool m_isSelfClearOut = true;

        // Передаё информация о том, является ли очистка самавоспроизводимой или пришла волной от предков
        protected void ClearOut(bool isSelf = false)
        {
            m_isSelfClearOut = isSelf;

            foreach (var img in m_out)
                img.Dispose();
            m_out.Clear();
        }
        



        // анулирует свои результаты Do() и своих потомков, т.е. при вызове GetOut будет перерасчёт
        public void Invalidate(bool isSelf)
        {
            ClearOut(isSelf);
            foreach (var filter in Children)
                filter.Invalidate(false); //потомки всегоа зависимы, потому не могут не вызывать свой Do
        }

        public abstract bool Do();
    }
    

    public class MatSourceFilter : BaseFilter
    {
        public MatSourceFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.Source;
            DataPropertys["Source"] = new Mat();
        }

        public override bool Do()
        {
            ClearOut();
            Mat src = (Mat)DataPropertys["Source"];           
            this.m_out.Add(new DataSrc(src.Clone(), "Source"));
            return true;
        }
    }

    public class NegativeFilter : BaseFilter
    {
        public NegativeFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.Negative;
        }

        public override bool Do()
        {
            ClearOut();
            try
            {
                foreach (var src in Sources)
                    foreach (var outData in src.GetOut())
                    {
                        Mat img = outData.Image;
                        Mat img255 = new Mat(img.Rows, img.Cols, img.Depth, img.NumberOfChannels);
                        img255.SetTo(new Bgr(255,255,255).MCvScalar);
                        Mat inv = img255 - img;
                        this.m_out.Add(new DataSrc(inv, outData.Info, false));
                    }
            }
            catch { return false; }
            return true;
        }
    }


    public class CreateBgrFilter : BaseFilter
    {
        public CreateBgrFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.BGRBlank;
            SourceType = new DataType() { Source = true };
            DestinationType = new DataType() { channels = 3, depth = Emgu.CV.CvEnum.DepthType.Cv8U };
            EnumPropertys["FreeMode"] = new EnumProperty(typeof(FilterLogicMode));

            IntPropertys["Width"] = new NumProperty<int>() { MaxVal = 4096, MinVal = 0, Value = 320 };
            IntPropertys["Height"] = new NumProperty<int>() { MaxVal = 4096, MinVal = 0, Value = 240 };

            IntPropertys["Red"] = new NumProperty<int>() { MaxVal = 255, MinVal = 0, Value = 0 };
            IntPropertys["Green"] = new NumProperty<int>() { MaxVal = 255, MinVal = 0, Value = 0 };
            IntPropertys["Blue"] = new NumProperty<int>() { MaxVal = 255, MinVal = 0, Value = 0 };
        }

        public override bool Do()
        {
            try
            {
                ClearOut();
                var FreeMode = (FilterLogicMode)EnumPropertys["FreeMode"].Value;


                foreach (var src in Sources)
                    foreach(var outData in src.GetOut())
                    {
                        Mat mat = null;
                        if (FreeMode == FilterLogicMode.On)
                            mat = new Mat(IntPropertys["Height"].Value,
                                               IntPropertys["Width"].Value,
                                               DepthType.Cv8U, 3);
                        else
                            mat = new Mat(outData.Image.Height, outData.Image.Width, outData.Image.Depth, outData.Image.NumberOfChannels);

                        mat.SetTo(new Bgr(IntPropertys["Blue"].Value, IntPropertys["Green"].Value, IntPropertys["Red"].Value).MCvScalar);
                        this.m_out.Add(new DataSrc(mat, "CreateBgr"));
                    }
   
            } catch { return false; }
            return true;
        }
    }

    public class LoadBgrFilter : BaseFilter
    {

        public LoadBgrFilter(GraphFilter graph = null, string name = null) : base(graph, name)
        {
            Type = FilterType.File;
            SourceType = new DataType() { Source = true };
            DestinationType = new DataType() { channels = 3, depth = Emgu.CV.CvEnum.DepthType.Cv8U };

            FilePropertys["Path"] = new FileInfo("img.jpg");

            EnumPropertys["ImgGroup"] = new EnumProperty(typeof(FilterLogicMode));
            EnumPropertys["ImgGroup"].Set(FilterLogicMode.Off.ToString());
        }

        public override bool Do()
        {
            try
            {
                FilterLogicMode groupMode = (FilterLogicMode)EnumPropertys["ImgGroup"].Value;
                bool bGroup = groupMode == FilterLogicMode.On;

                ClearOut();

                if (!bGroup)
                {
                    string filename = FilePropertys["Path"].FileName;
                    Mat readImg = CvInvoke.Imread(filename);
                    this.m_out.Add(new DataSrc(readImg, filename, false));
                } else
                {
                    string DictPath = "";
                    string filePath = FilePropertys["Path"].FileName;
                    int idxSp = filePath.LastIndexOf("\\");

                    if (idxSp >= 0)
                        DictPath = filePath.Substring(0, idxSp);

                    foreach (var file in Directory.GetFiles(DictPath))
                    {
                        Mat readImg = CvInvoke.Imread(file);
                        this.m_out.Add(new DataSrc(readImg, file, false));
                    }

                }
            } catch { return false; }

            return true;
        }
    }

    // Довольно не стабильная штука
    // Т.к. поток обнов идёт от хвостов к источникам
    // Свайп может пропускать часть логики, потому его использование опасно

    //  Я столкнулся с этим на ректах когда хотел протащить рект на другой соурс для вырезания
    public class SourceSwipeFilter : BaseFilter, IContourFilter, IRectangleFilter
    {
        List<CvContours> m_contours = null;
        List<Rectangle> m_rectangles = null;
        
        public List<CvContours> GetResultContours()
        {
            return m_contours;
        }
        public List<Rectangle> GetResultRectangles()
        {
            return m_rectangles;
        }

        public SourceSwipeFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.SourceSwipe;
            EnumPropertys["FromSource"] = new EnumProperty(graph.GetAddedNames());
        }

        public override bool Do()
        {
            try
            {
                BaseFilter FromSource = Graph.GetFilter(EnumPropertys["FromSource"].Value);

                IContourFilter contourFilter = FromSource as IContourFilter;
                if (contourFilter != null)
                    m_contours = contourFilter.GetResultContours();
                else m_contours = new List<CvContours>();


                IRectangleFilter rectFilter = FromSource as IRectangleFilter;
                if (rectFilter != null)
                    m_rectangles = rectFilter.GetResultRectangles();
                else
                    m_rectangles = new List<Rectangle>();

                foreach (var outData in FromSource.GetOut())
                    this.m_out.Add(outData.Clone());
            }
            catch { return false; }
            return true;
        }
    }

    public class SplitFilter : BaseFilter
    {
        public SplitFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.Split;
            SourceType = new DataType() { Source = true };
            DestinationType = new DataType() { Source = true };
            
            IntPropertys["Count"] = new NumProperty<int>() { MaxVal = 10, MinVal = 1, Value = 1 };
        }

        public override bool Do()
        {
            try
            {
                foreach (var src in Sources)
                    foreach (var outData in src.GetOut())
                    {
                        this.m_out.Add(outData.Clone());
                    }
            }
            catch { return false; }
            return true;
        }
    }

    public class SelectFilter : BaseFilter
    {
        public SelectFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.Select;
            SourceType = new DataType() { Source = true };
            DestinationType = new DataType() { Source = true };

            IntPropertys["SourcesID"] = new NumProperty<int>() { MaxVal = 10, MinVal = 0, Value = 0 };
            IntPropertys["OutID"] = new NumProperty<int>() { MaxVal = 10, MinVal = 0, Value = 0 };
        }

        public override bool Do()
        {
            try
            {
                var copy = Sources[IntPropertys["SourcesID"].Value].GetOut()[IntPropertys["OutID"].Value].Clone();
                this.m_out.Add(copy);
            } catch { return false; }

            return true;
        }
    }

    public class AppendSourceFilter : BaseFilter
    {
        public AppendSourceFilter(GraphFilter graph = null, string name = null) : base(graph, name)
        {
            Type = FilterType.Append;
            if (graph != null)
                EnumPropertys["AppendSource"] = new EnumProperty(graph.GetAddedNames());
        }

        public override bool Do()
        {
            try
            {
                BaseFilter SourceFilter = Graph.GetFilter(EnumPropertys["AppendSource"].Value);

                foreach(var scr in Sources)
                    foreach (var outData in scr.GetOut())
                        this.m_out.Add(outData.Clone());

                foreach (var outData in SourceFilter.GetOut())
                    this.m_out.Add(outData.Clone());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
    }

    public class MergeFilter : BaseFilter
    {
        public MergeFilter(GraphFilter graph = null, string name = null) : base(graph, name)
        {
            Type = FilterType.Merge;
            SourceType = new DataType() { Source = true, depth = Emgu.CV.CvEnum.DepthType.Cv8U };
            DestinationType = new DataType() { Source = true, depth = Emgu.CV.CvEnum.DepthType.Cv8U };

            FloatPropertys["Alpha"] = new NumProperty<double>() { MaxVal = 10, MinVal = -10, Value = 0.5 };
            FloatPropertys["Beta"] = new NumProperty<double>() { MaxVal = 10, MinVal = -10, Value = 0.5 };
            FloatPropertys["Gamma"] = new NumProperty<double>() { MaxVal = 10, MinVal = -10, Value = 0 };


            if (graph != null)
                EnumPropertys["BetaSource"] = new EnumProperty(graph.GetAddedNames());

            //IntPropertys["SourcesID"] = new NumProperty<int>() { MaxVal = 10, MinVal = 0, Value = 0 };
            //IntPropertys["OutID"] = new NumProperty<int>() { MaxVal = 10, MinVal = 0, Value = 0 };

            FilePropertys["BetaFileImg"] = new FileInfo("");
        }

        public override bool Do()
        {
            try
            {
                int comparableID = 0;
                BaseFilter betaSourceFilter = Graph.GetFilter(EnumPropertys["BetaSource"].Value);
       
                
                for (int SourcesId = 0; SourcesId < Sources.Count; SourcesId++)
                {
                    var AlphaDatas = Sources[SourcesId].GetOut();
                    var betaDatas = betaSourceFilter.GetOut();

                    for (int ImgId = 0; ImgId < AlphaDatas.Count; ImgId++)
                    {
                        Mat alphaImg = AlphaDatas[ImgId].Image;
                        Mat betaImg = null;


                        if (FilePropertys["BetaFileImg"].FileName.Length > 0)
                            betaImg = CvInvoke.Imread(FilePropertys["BetaFileImg"].FileName);
                        else
                        {
                            if (comparableID < AlphaDatas.Count)
                                betaImg = betaDatas[comparableID].Image.Clone();
                            else betaImg = new Mat(alphaImg.Size, alphaImg.Depth, alphaImg.NumberOfChannels);
                        }
                        comparableID++;


                        Mat resImg = new Mat();
                        CvInvoke.AddWeighted(alphaImg, FloatPropertys["Alpha"].Value,
                                             betaImg, FloatPropertys["Beta"].Value,
                                             FloatPropertys["Gamma"].Value, 
                                             resImg, DepthType.Cv8U);

                        this.m_out.Add(new DataSrc(resImg, AlphaDatas[ImgId].Info, false));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
    }

    public class MergeInSrcFilter : BaseFilter
    {
        public MergeInSrcFilter(GraphFilter graph = null, string name = null) : base(graph, name)
        {
            Type = FilterType.Merge;
            SourceType = new DataType() { Source = true, depth = Emgu.CV.CvEnum.DepthType.Cv8U };
            DestinationType = new DataType() { Source = true, depth = Emgu.CV.CvEnum.DepthType.Cv8U };

            FloatPropertys["Src1"] = new NumProperty<double>() { MaxVal = 10, MinVal = -10, Value = 0.5 };
            FloatPropertys["Src2"] = new NumProperty<double>() { MaxVal = 10, MinVal = -10, Value = 0.5 };
            FloatPropertys["Gamma"] = new NumProperty<double>() { MaxVal = 10, MinVal = -10, Value = 0 };

            IntPropertys["Src1ID"] = new NumProperty<int>() { MaxVal = 10, MinVal = 0, Value = 0 };
            IntPropertys["Src2ID"] = new NumProperty<int>() { MaxVal = 10, MinVal = 0, Value = 1 };
        }

        public override bool Do()
        {
            try
            {
                int id1 = IntPropertys["Src1ID"].Value;
                int id2 = IntPropertys["Src2ID"].Value;
                for (int SourcesId = 0; SourcesId < Sources.Count; SourcesId++)
                {
                    var srcDatas = Sources[SourcesId].GetOut();
                    if ((id1< srcDatas.Count) && (id2 < srcDatas.Count))
                    {
                        Mat resImg = new Mat();
                        CvInvoke.AddWeighted(srcDatas[id1].Image, FloatPropertys["Src1"].Value,
                                             srcDatas[id2].Image, FloatPropertys["Src2"].Value,
                                             FloatPropertys["Gamma"].Value,
                                             resImg, DepthType.Cv8U);
                        this.m_out.Add(new DataSrc(resImg, srcDatas[id1].Info, false));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
    }

    public class Bgr2GrayFilter : BaseFilter
    {

        public Bgr2GrayFilter(GraphFilter graph = null, string name = null) : base(graph, name)
        {
            Type = FilterType.Bgr2Gray;
            SourceType = new DataType() { channels = 3, depth = Emgu.CV.CvEnum.DepthType.Cv8U };
            DestinationType = new DataType() { channels = 1, depth = Emgu.CV.CvEnum.DepthType.Cv8U };
        }

        public override bool Do()
        {
            ClearOut();
            try
            {
                foreach (var src in Sources)
                    foreach (var outData in src.GetOut())
                    {
                        Mat imgout = new Mat();
                        CvInvoke.CvtColor(outData.Image, imgout, ColorConversion.Bgr2Gray);
                        this.m_out.Add(new DataSrc(imgout, outData.Info, false));
                    }
            } catch { return false; }
            return true;
        }
    }




    public class Gray2BgrFilter : BaseFilter
    {
        public Gray2BgrFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.Gray2Bgr;
            SourceType = new DataType() { channels = 1, depth = Emgu.CV.CvEnum.DepthType.Cv8U };
            DestinationType = new DataType() { channels = 3, depth = Emgu.CV.CvEnum.DepthType.Cv8U };
        }

        public override bool Do()
        {
            ClearOut();
            try
            {
                foreach (var src in Sources)
                    foreach (var outData in src.GetOut())
                    {
                        Mat imgout = new Mat();
                        CvInvoke.CvtColor(outData.Image, imgout, ColorConversion.Gray2Bgr);
                        this.m_out.Add(new DataSrc(imgout, outData.Info, false));
                    }
            }
            catch { return false; }
            return true;
        }
    }

    public class BgrLUTFilter : BaseFilter
    {
        private CvLUT[] m_bgrLUTS = null;


        public BgrLUTFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.LUT;
            
            
            EnumPropertys["Blue"] = new EnumProperty(typeof(LutType));
            EnumPropertys["Green"] = new EnumProperty(typeof(LutType));
            EnumPropertys["Red"] = new EnumProperty(typeof(LutType));

        }

        public override bool Do()
        {
            ClearOut();
            try
            {
                var blueTp = (LutType)EnumPropertys["Blue"].Value;
                var greenTp = (LutType)EnumPropertys["Green"].Value;
                var redTp = (LutType)EnumPropertys["Red"].Value;


                var bReqFullUpdate = (m_bgrLUTS == null) ||
                                     (m_bgrLUTS[0].Type != blueTp) ||
                                     (m_bgrLUTS[1].Type != greenTp) ||
                                     (m_bgrLUTS[2].Type != redTp);


                if (bReqFullUpdate)
                {
                    // по идее если стоит полином, то его тут тоже надо выставлять, но пока забил
                    m_bgrLUTS = new CvLUT[3];
                    m_bgrLUTS[0] = new CvLUT() { Type = blueTp };
                    m_bgrLUTS[1] = new CvLUT() { Type = greenTp };
                    m_bgrLUTS[2] = new CvLUT() { Type = redTp };
                }

                foreach (var src in Sources)
                    foreach (var outData in src.GetOut())
                    {
                        Mat imgout = CvLUT.CreateBGRLut(outData.Image, m_bgrLUTS);
                        this.m_out.Add(new DataSrc(imgout, outData.Info, false));
                    }
            }
            catch (Exception ex) {
                return false;
            }
            return true;
        }
    }

    public class ResizeAbsFilter : BaseFilter
    {
        public ResizeAbsFilter(GraphFilter graph=null, string name = null) : base(graph, name)
        {
            Type = FilterType.ResizeAbs;
            SourceType = new DataType() { channels = 3, depth = Emgu.CV.CvEnum.DepthType.Cv8U };
            DestinationType = new DataType() { channels = 3, depth = Emgu.CV.CvEnum.DepthType.Cv8U };

            IntPropertys["Width"] = new NumProperty<int>() { MaxVal = 4096, MinVal = 0, Value = 320 };
            IntPropertys["Height"] = new NumProperty<int>() { MaxVal = 4096, MinVal = 0, Value = 240 };

            FloatPropertys["fx"] = new NumProperty<double>() { MaxVal = 4096, MinVal = 0, Value = 0 };
            FloatPropertys["fy"] = new NumProperty<double>() { MaxVal = 4096, MinVal = 0, Value = 0 };

            EnumPropertys["Inter"] = new EnumProperty(typeof(Inter));
            EnumPropertys["Inter"].Set("Area");
        }

        public override bool Do()
        {            
            ClearOut();
            try
            {
                Inter InterEnum = (Inter)EnumPropertys["Inter"].Value;
                foreach (var src in Sources)
                    foreach (var outData in src.GetOut())
                    {
                        Mat imgout = new Mat();
                        CvInvoke.Resize(outData.Image, imgout, new System.Drawing.Size(IntPropertys["Width"].Value, IntPropertys["Height"].Value),
                                                                             FloatPropertys["fx"].Value, FloatPropertys["fy"].Value, InterEnum);
                        this.m_out.Add(new DataSrc(imgout, outData.Info, false));
                    }
            } catch { return false; }
            return true;
        }
    }


    public class ResizeFilter : BaseFilter
    {
        public ResizeFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.Resize;
            SourceType = new DataType() { channels = 3, depth = Emgu.CV.CvEnum.DepthType.Cv8U };
            DestinationType = new DataType() { channels = 3, depth = Emgu.CV.CvEnum.DepthType.Cv8U };

            FloatPropertys["Scale"] = new NumProperty<double>() { MaxVal = 1000, MinVal = 0, Value = 1 };

            EnumPropertys["Inter"] = new EnumProperty(typeof(Inter));
            EnumPropertys["Inter"].Set("Area");
        }

        public override bool Do()
        {
            ClearOut();
            try
            {                
                Inter InterEnum = (Inter)EnumPropertys["Inter"].Value;
                foreach (var src in Sources)
                    foreach (var outData in src.GetOut())
                    {
                        int w = (int)(outData.Image.Width * FloatPropertys["Scale"].Value);
                        int h = (int)(outData.Image.Height * FloatPropertys["Scale"].Value);

                        Mat imgout = new Mat();
                        CvInvoke.Resize(outData.Image, imgout, new System.Drawing.Size(w, h),0,0, InterEnum);
                        this.m_out.Add(new DataSrc(imgout, outData.Info, false));
                    }
            }
            catch { return false; }
            return true;
        }
    }

    public class RotationFilter : BaseFilter
    {
        public RotationFilter(GraphFilter graph = null, string name = null) : base(graph, name)
        {
            Type = FilterType.Rotation;
            FloatPropertys["Deg"] = new NumProperty<double>() { MaxVal = 360, MinVal = 0, Value = 0 };
            FloatPropertys["Scale"] = new NumProperty<double>() { MaxVal = 10, MinVal = 0, Value = 1 };

            FloatPropertys["xc"] = new NumProperty<double>() { MaxVal = 1, MinVal = 0, Value = 0.5 };
            FloatPropertys["yc"] = new NumProperty<double>() { MaxVal = 1, MinVal = 0, Value = 0.5 };


            IntPropertys["NewSize"] = new NumProperty<int>() { MaxVal = 1, MinVal = 0, Value = 0 };
            IntPropertys["Width"] = new NumProperty<int>() { MaxVal = 4096, MinVal = 0, Value = 320 };
            IntPropertys["Height"] = new NumProperty<int>() { MaxVal = 4096, MinVal = 0, Value = 240 };

            EnumPropertys["Inter"] = new EnumProperty(typeof(Inter));
            EnumPropertys["Inter"].Set("Area");
        }

        public override bool Do()
        {
            ClearOut();
            try
            {
                Inter InterEnum = (Inter)EnumPropertys["Inter"].Value;
                foreach (var src in Sources)
                    foreach (var outData in src.GetOut())
                    {
                        int xc = (int)(outData.Image.Width * FloatPropertys["xc"].Value);
                        int yc = (int)(outData.Image.Height * FloatPropertys["yc"].Value);

                        Mat rotationMatrix = new Mat();
                        CvInvoke.GetRotationMatrix2D(new System.Drawing.Point(xc, yc), FloatPropertys["Deg"].Value, FloatPropertys["Scale"].Value, rotationMatrix);

                        var outSz = new System.Drawing.Size(outData.Image.Width, outData.Image.Height);
                        if (IntPropertys["NewSize"].Value>0)
                            outSz = new System.Drawing.Size(IntPropertys["Width"].Value, IntPropertys["Height"].Value);

                        Mat dst = new Mat();
                        CvInvoke.WarpAffine(outData.Image, dst, rotationMatrix, outSz, InterEnum);

                        this.m_out.Add(new DataSrc(dst, outData.Info, false));
                    }
            }
            catch { return false; }

            return true;
        }
    }

    public class Conv2DFilter : BaseFilter
    {
        public Conv2DFilter(GraphFilter graph, string name = null, int kernSize = 3) : base(graph, name)
        {
            TablePropertys["Kernel"] = new TableProperty<double>(kernSize, kernSize);
        }

        public override bool Do()
        {
            Mat kernelMat = OpenCVHelper.GetMat(TablePropertys["Kernel"].m_table, TablePropertys["Kernel"].Rows, TablePropertys["Kernel"].Columns);
            try
            {
                var kernel = TablePropertys["Kernel"];
                foreach (var src in Sources)
                    foreach (var outData in src.GetOut())
                    {
                        Mat dst = outData.Image.Clone();
                        CvInvoke.Filter2D(outData.Image, dst, kernelMat, new System.Drawing.Point(-1, -1));
                        this.m_out.Add(new DataSrc(dst, outData.Info, false));
                    }
            }
            catch { return false; }
            return true;
        }
    }


    public class HistogramFilter : BaseFilter
    {
        private List<double[,]> histogramsResults = new List<double[,]>(); // чтобы не пересчитывать гистограммы на каждый чих, буду делать это
                                                                           // только на bReqFullUpdate

        public HistogramFilter(GraphFilter graph, string name) : base(graph, name)
        {
            Type = FilterType.Histogram;

            IntPropertys["x_show"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 0 };
            IntPropertys["y_show"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 100 };
            FloatPropertys["scaleX"] = new NumProperty<double>() { MinVal = 0.1, MaxVal = 5, Value = 1 };
            FloatPropertys["scaleY"] = new NumProperty<double>() { MinVal = 0.001, MaxVal = 1, Value = 0.1 };

            EnumPropertys["MovingType"] = new EnumProperty(typeof(MovingPredictType));
            IntPropertys["MovingRange"] = new NumProperty<int>() { MinVal = 1, MaxVal = 10, Value = 1 };            
        }

        public override bool Do()
        {
            try
            {
                var bReqFullUpdate = !m_isSelfClearOut || (histogramsResults.Count==0);
                if (bReqFullUpdate)
                    histogramsResults.Clear();

                IMovingPredict movingPredict = null;
                MovingPredictType movingType = (MovingPredictType)EnumPropertys["MovingType"].Value;
                switch (movingType)
                {
                    case MovingPredictType.MovingAverage:
                        movingPredict = new MovingAverage(IntPropertys["MovingRange"].Value);
                        break;
                    case MovingPredictType.MovingMediana:
                        movingPredict = new MovingMediana(IntPropertys["MovingRange"].Value);
                        break;
                    default:
                        movingPredict = new MovingAverage(IntPropertys["MovingRange"].Value);
                        break;
                }

                int listID = 0;
                for (int srcID = 0; srcID < Sources.Count; srcID++) {
                    var outDatas = Sources[srcID].GetOut();
                    for (int dataID = 0; dataID < outDatas.Count; dataID++) {
                        
                        if (bReqFullUpdate)
                            histogramsResults.Add(RGBHistogram.GetHistograms(outDatas[dataID].Image));

                        Mat copy = outDatas[dataID].Image.Clone();

                        
                        RGBHistogram.DrawChannel(copy, IntPropertys["x_show"].Value, IntPropertys["y_show"].Value,
                                                 FloatPropertys["scaleX"].Value, FloatPropertys["scaleY"].Value, movingPredict,
                                                 histogramsResults[listID], 0, new Bgr(0, 0, 255).MCvScalar, new Bgr(50, 0, 255).MCvScalar, false);


                        RGBHistogram.DrawChannel(copy, IntPropertys["x_show"].Value, IntPropertys["y_show"].Value,
                                                 FloatPropertys["scaleX"].Value, FloatPropertys["scaleY"].Value, movingPredict,
                                                 histogramsResults[listID], 1, new Bgr(255, 0, 0).MCvScalar, new Bgr(255, 0, 50).MCvScalar, false);

                        RGBHistogram.DrawChannel(copy, IntPropertys["x_show"].Value, IntPropertys["y_show"].Value,
                                                 FloatPropertys["scaleX"].Value, FloatPropertys["scaleY"].Value, movingPredict,
                                                 histogramsResults[listID], 2, new Bgr(0, 255, 0).MCvScalar, new Bgr(50, 255, 50).MCvScalar, false);

                        listID++;
                        this.m_out.Add(new DataSrc(copy, outDatas[dataID].Info, false));
                    }
                }
            }
            catch (Exception ex) {
                return false;
            }
            return true;
        }
    }

    public class PixelSumFilter : BaseFilter
    {
        private List<double> pixelSumResults = new List<double>(); // пересчитывается только на bReqFullUpdate = !m_isSelfClearOut || (pixelSumResults.Count == 0)

        public PixelSumFilter(GraphFilter graph, string name) : base(graph, name)
        {
            Type = FilterType.PixelSum;
            EnumPropertys["Draw"] = new EnumProperty(typeof(FilterDrawMode));   
            IntPropertys["y"] = new NumProperty<int>() { MinVal = 0, MaxVal = 255, Value = 30 };

            IntPropertys["r"] = new NumProperty<int>() { MinVal = 0, MaxVal = 255, Value = 255 };
            IntPropertys["g"] = new NumProperty<int>() { MinVal = 0, MaxVal = 255, Value = 0 };
            IntPropertys["b"] = new NumProperty<int>() { MinVal = 0, MaxVal = 255, Value = 0 };
        }



        public override bool Do()
        {
            try
            {
                var bReqFullUpdate = !m_isSelfClearOut || (pixelSumResults.Count == 0);
                if (bReqFullUpdate)
                    pixelSumResults.Clear();

                FilterDrawMode drawMode = (FilterDrawMode)EnumPropertys["Draw"].Value;

                int listID = 0;
                for (int srcID = 0; srcID < Sources.Count; srcID++)
                {
                    var outDatas = Sources[srcID].GetOut();
                    for (int dataID = 0; dataID < outDatas.Count; dataID++)
                    {
                        if (bReqFullUpdate)
                            pixelSumResults.Add(OpenCVHelper.GetPixelSum(outDatas[dataID].Image));

                        if (drawMode == FilterDrawMode.Show)
                        {
                            double val = pixelSumResults[listID];
                            Mat copy = outDatas[dataID].Image.Clone();
                            CvInvoke.PutText(
                               copy,
                               "sum:" + val.ToString(),
                               new System.Drawing.Point(10, IntPropertys["y"].Value),
                               FontFace.HersheyComplex, 1.0, new Bgr(IntPropertys["r"].Value, IntPropertys["g"].Value, IntPropertys["b"].Value).MCvScalar);
                            this.m_out.Add(new DataSrc(copy, outDatas[dataID].Info, false));
                        } else
                        {
                            this.m_out.Add(outDatas[dataID].Clone());
                        }

                        listID++;
                    }
                }
            } catch { return false; }
            return true;
        }
    }

}
