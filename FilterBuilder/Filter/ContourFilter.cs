using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FilterBuilder.Common;
using FilterBuilder.Common.MovingPredict;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Filter
{

    public enum FillDrawModeEnum
    {
        None = 0,
        RndFill = 1,
        SourceMeanColor = 2
    }



    // фильтры которы польузтся результатами поиска контуров должны поддерживать этот интерфейс
    public interface IContourFilter
    {
        List<CvContours> GetResultContours();
    }


    /// <summary>
    /// Поиск всех контуров
    /// Есть несколько режимов отображения
    /// Можно переключиться на рендерв от другого источника (чтобы искать по одим картинкам и использовать уже на других)
    /// </summary>
    public class ContourSearchFilter : BaseFilter, IContourFilter
    {
        List<CvContours> m_contours = new List<CvContours>();

        public List<CvContours> GetResultContours()
        {
            return m_contours;
        }

        public ContourSearchFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.ContourSearch;
            EnumPropertys["DrawOn"] = new EnumProperty(typeof(FilterDrawMode));
            IntPropertys["Thickness"] = new NumProperty<int>() { MaxVal = 10, MinVal = 0, Value = 3 };
            EnumPropertys["BoundingRect"] = new EnumProperty(typeof(FilterDrawMode));
            EnumPropertys["CenterDrawMode"] = new EnumProperty(typeof(CenterDrawMode));
            EnumPropertys["FillMode"] = new EnumProperty(typeof(FillDrawModeEnum));

            EnumPropertys["Histogram"] = new EnumProperty(typeof(FilterDrawMode));
            EnumPropertys["Histogram"].Set("Hide");


            EnumPropertys["SourceMode"] = new EnumProperty(typeof(SourceDrawModeEnum));
            if (graph != null)
                EnumPropertys["DrawSource"] = new EnumProperty(graph.GetAddedNames());
        }

        public override bool Do()
        {
            try
            {
                m_contours.Clear();
                FilterDrawMode DrawOn = (FilterDrawMode)EnumPropertys["DrawOn"].Value;
                FilterDrawMode BoundingRect = (FilterDrawMode)EnumPropertys["BoundingRect"].Value;
                bool bShowBoundingRect = BoundingRect == FilterDrawMode.Show;
                CenterDrawMode centerDrawMode = (CenterDrawMode)EnumPropertys["CenterDrawMode"].Value;


                FillDrawModeEnum FillMode = (FillDrawModeEnum)EnumPropertys["FillMode"].Value;
                bool bFill = FillMode != FillDrawModeEnum.None;

                FilterDrawMode Histogram = (FilterDrawMode)EnumPropertys["Histogram"].Value;
                bool bHistogram = Histogram == FilterDrawMode.Show;

                SourceDrawModeEnum SourceMode = (SourceDrawModeEnum)EnumPropertys["SourceMode"].Value;

                BaseFilter drawSourceFilter = Graph.GetFilter(EnumPropertys["DrawSource"].Value);
                List<DataSrc> drawDataSrcs = null;
                int drawDataCnt = 0;
                if (drawSourceFilter != null)
                {
                    drawDataSrcs = drawSourceFilter.GetOut();
                    drawDataCnt = drawDataSrcs.Count;
                }


                foreach (var src in Sources)
                {
                    var outData = src.GetOut();

                    int dataID = 0; // перебор картинок в сорсе
                    int exsourceID = 0; // перебор картинок стороннего источника при DrawSourceMode

                    Mat currentImg = null;
                    while ((dataID < outData.Count) || ((SourceMode == SourceDrawModeEnum.DrawSource) && (exsourceID < drawDataCnt)))
                    {
                        string dataInfo = "unknow";
                        if (dataID < outData.Count)
                        {
                            dataInfo = outData[dataID].Info;
                            currentImg = outData[dataID++].Image;
                        }

                        if (currentImg == null)
                            break;

                        Mat dst = null;
                        switch (SourceMode)
                        {
                            case SourceDrawModeEnum.Self: dst = currentImg.Clone(); break;
                            case SourceDrawModeEnum.None: dst = new Mat(currentImg.Size, currentImg.Depth, currentImg.NumberOfChannels); break;
                            case SourceDrawModeEnum.DrawSource:
                                if (exsourceID < drawDataCnt)
                                {
                                    dataInfo = drawDataSrcs[exsourceID].Info;
                                    dst = drawDataSrcs[exsourceID++].Image.Clone();                                    
                                }
                                else
                                    dst = new Mat(currentImg.Size, currentImg.Depth, currentImg.NumberOfChannels);
                                break;
                        }

                        CvContours contours = new CvContours();
                        contours.Update(currentImg);

                        if (FillMode == FillDrawModeEnum.SourceMeanColor)
                            contours.UpdateMainColorAsMeanOfMask(dst);

                        if (centerDrawMode == CenterDrawMode.HistogramCenter)
                            contours.UpdateXYHistogram();

                        if (DrawOn == FilterDrawMode.Show)
                            contours.Draw(dst, bFill, bShowBoundingRect, centerDrawMode, IntPropertys["Thickness"].Value, bHistogram);

                        this.m_contours.Add(contours);
                        this.m_out.Add(new DataSrc(dst, dataInfo, false));
                    }
                }
            }
            catch (CvException ex)
            {
                return false;
            }
            return true;
        }

    }

    /// <summary>
    /// Отсекает контура по набору признаков
    /// </summary>
    public class ContourSortFilter : BaseFilter, IContourFilter
    {
        List<CvContours> m_contours = new List<CvContours>();

        public List<CvContours> GetResultContours()
        {
            return m_contours;
        }

        public ContourSortFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.ContourSort;

            EnumPropertys["DrawOn"] = new EnumProperty(typeof(FilterDrawMode));
            IntPropertys["Thickness"] = new NumProperty<int>() { MaxVal = 10, MinVal = 0, Value = 3 };
            EnumPropertys["BoundingRect"] = new EnumProperty(typeof(FilterDrawMode));
            EnumPropertys["CenterDrawMode"] = new EnumProperty(typeof(CenterDrawMode));
            EnumPropertys["FillMode"] = new EnumProperty(typeof(FillDrawModeEnum));

            EnumPropertys["Histogram"] = new EnumProperty(typeof(FilterDrawMode));
            EnumPropertys["Histogram"].Set("Hide");

            FloatPropertys["MinArea"] = new NumProperty<double>() { MinVal = 0, MaxVal = 50000, Value = 0 };
            FloatPropertys["MaxArea"] = new NumProperty<double>() { MinVal = 0, MaxVal = 50000, Value = 50000 };

            FloatPropertys["MinPerimeter"] = new NumProperty<double>() { MinVal = 0, MaxVal = 50000, Value = 0 };
            FloatPropertys["MaxPerimeter"] = new NumProperty<double>() { MinVal = 0, MaxVal = 50000, Value = 50000 };

            IntPropertys["MinHeight"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 0 };
            IntPropertys["MaxHeight"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 4096 };

            IntPropertys["MinWidth"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 0 };
            IntPropertys["MaxWidth"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 4096 };
        }


        public override bool Do()
        {
            try
            {
                m_contours.Clear();
                FilterDrawMode DrawOn = (FilterDrawMode)EnumPropertys["DrawOn"].Value;
                FilterDrawMode BoundingRect = (FilterDrawMode)EnumPropertys["BoundingRect"].Value;
                bool bShowBoundingRect = BoundingRect == FilterDrawMode.Show;
                CenterDrawMode centerDrawMode = (CenterDrawMode)EnumPropertys["CenterDrawMode"].Value;
                FillDrawModeEnum FillMode = (FillDrawModeEnum)EnumPropertys["FillMode"].Value;
                bool bFill = FillMode != FillDrawModeEnum.None;

                FilterDrawMode Histogram = (FilterDrawMode)EnumPropertys["Histogram"].Value;
                bool bHistogram = Histogram == FilterDrawMode.Show;

                foreach (var src in Sources)
                {
                    IContourFilter searchFilter = src as IContourFilter;
                    if (searchFilter == null)
                        continue;

                    var outData = src.GetOut();
                    var contourSrcs = searchFilter.GetResultContours();

                    for (int dataID = 0; dataID < outData.Count; dataID++)
                    {
                        var dst = outData[dataID].Image.Clone();
                        string dataInfo = outData[dataID].Info;

                        if (dataID >= contourSrcs.Count)
                        {
                            this.m_out.Add(new DataSrc(dst, dataInfo, false));
                            continue;
                        }

                        var sortAreaContour = contourSrcs[dataID].GetPerimeterInRange(FloatPropertys["MinArea"].Value, FloatPropertys["MaxArea"].Value);
                        var sortPerimeterContour = sortAreaContour.GetPerimeterInRange(FloatPropertys["MinPerimeter"].Value, FloatPropertys["MaxPerimeter"].Value);
                        var sortBoundingRectContour = sortPerimeterContour.GetBoundingRectInRange(IntPropertys["MinWidth"].Value, IntPropertys["MaxWidth"].Value,
                                                                                          IntPropertys["MinHeight"].Value, IntPropertys["MaxHeight"].Value);

                        if (FillMode == FillDrawModeEnum.SourceMeanColor)
                            sortBoundingRectContour.UpdateMainColorAsMeanOfMask(dst);

                        m_contours.Add(sortBoundingRectContour);

                        if (centerDrawMode == CenterDrawMode.HistogramCenter)
                            sortBoundingRectContour.UpdateXYHistogram();

                        if (DrawOn == FilterDrawMode.Show)
                            sortBoundingRectContour.Draw(dst, bFill, bShowBoundingRect, centerDrawMode, IntPropertys["Thickness"].Value, bHistogram);

                        this.m_out.Add(new DataSrc(dst, dataInfo, false));
                    }
                }
            }
            catch (CvException ex)
            {
                return false;
            }
            return true;
        }

    }


    /// <summary>
    /// Отсекает все контура не попавшие в выделенную область
    /// </summary>
    public class ContourAreaSelectFilter : BaseFilter, IContourFilter
    {
        List<CvContours> m_contours = new List<CvContours>();

        public List<CvContours> GetResultContours()
        {
            return m_contours;
        }

        public ContourAreaSelectFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.ContourAreaSelect;
            EnumPropertys["DrawOn"] = new EnumProperty(typeof(FilterDrawMode));
            EnumPropertys["BoundingRect"] = new EnumProperty(typeof(FilterDrawMode));

            IntPropertys["X"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 100 };
            IntPropertys["Y"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 100 };
            IntPropertys["SizeX"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 100 };
            IntPropertys["SizeY"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 100 };
        }

        public override bool Do()
        {
            try
            {
                m_contours.Clear();

                FilterDrawMode DrawOn = (FilterDrawMode)EnumPropertys["DrawOn"].Value;
                FilterDrawMode BoundingRect = (FilterDrawMode)EnumPropertys["BoundingRect"].Value;
                bool bShowBoundingRect = BoundingRect == FilterDrawMode.Show;

                Rectangle rect = new Rectangle(IntPropertys["X"].Value - IntPropertys["SizeX"].Value / 2,
                                               IntPropertys["Y"].Value - IntPropertys["SizeY"].Value / 2,
                                               IntPropertys["SizeX"].Value, IntPropertys["SizeY"].Value);


                foreach (var src in Sources)
                {
                    IContourFilter searchFilter = src as IContourFilter;
                    if (searchFilter == null)
                        continue;
                    var outData = src.GetOut();
                    var contourSrcs = searchFilter.GetResultContours();
                    for (int dataID = 0; dataID < outData.Count; dataID++)
                    {
                        var dst = outData[dataID].Image.Clone();
                        var insideContours = contourSrcs[dataID].GetInsideRectArea(rect);
                        m_contours.Add(insideContours);

                        if (DrawOn == FilterDrawMode.Show)
                        {
                            insideContours.Draw(dst, false, bShowBoundingRect, CenterDrawMode.BoundingRectCenter);
                            CvInvoke.Rectangle(dst, rect, new MCvScalar(255,77,34), 5);
                        }

                        this.m_out.Add(new DataSrc(dst, outData[dataID].Info, false));
                    }
                }
                
            }
            catch (CvException ex)
            {
                return false;
            }
            return true;
        }
    }


    public enum FilterSelectMode
    {
        MaxArea = 0,
        NearPointBR = 1,
        NearPointW = 2,
        NearPointH = 3,
    }


    public class SelectSingleContourFilter : BaseFilter, IContourFilter
    {
        List<CvContours> m_contours = new List<CvContours>();

        public List<CvContours> GetResultContours()
        {
            return m_contours;
        }

        public SelectSingleContourFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.ContourSingleSelect;
            EnumPropertys["DrawOn"] = new EnumProperty(typeof(FilterDrawMode));
            EnumPropertys["BoundingRect"] = new EnumProperty(typeof(FilterDrawMode));
            EnumPropertys["SelectMode"] = new EnumProperty(typeof(FilterSelectMode));

            IntPropertys["X"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 100 };
            IntPropertys["Y"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 100 };


            EnumPropertys["FillMode"] = new EnumProperty(typeof(FillDrawModeEnum));
            IntPropertys["Red"] = new NumProperty<int>() { MaxVal = 255, MinVal = 0, Value = 0 };
            IntPropertys["Green"] = new NumProperty<int>() { MaxVal = 255, MinVal = 0, Value = 0 };
            IntPropertys["Blue"] = new NumProperty<int>() { MaxVal = 255, MinVal = 0, Value = 0 };
        }

        public override bool Do()
        {
            try
            {
                m_contours.Clear();

                FilterDrawMode DrawOn = (FilterDrawMode)EnumPropertys["DrawOn"].Value;
                FilterDrawMode BoundingRect = (FilterDrawMode)EnumPropertys["BoundingRect"].Value;
                bool bShowBoundingRect = BoundingRect == FilterDrawMode.Show;

                FilterSelectMode SelectMode = (FilterSelectMode)EnumPropertys["SelectMode"].Value;
                Point hearPoint = new Point(IntPropertys["X"].Value, IntPropertys["Y"].Value);

                FillDrawModeEnum FillMode = (FillDrawModeEnum)EnumPropertys["FillMode"].Value;
                bool bFill = FillMode != FillDrawModeEnum.None;

                foreach (var src in Sources)
                {
                    IContourFilter searchFilter = src as IContourFilter;
                    if (searchFilter == null)
                        continue;
                    var outData = src.GetOut();
                    var contourSrcs = searchFilter.GetResultContours();
                    for (int dataID = 0; dataID < outData.Count; dataID++)
                    {
                        var dst = outData[dataID].Image.Clone();

                        CvContours selContours = null;
                        switch (SelectMode)
                        {
                            case FilterSelectMode.MaxArea:
                                selContours = contourSrcs[dataID].GetMaxAreaContour(); break;

                            case FilterSelectMode.NearPointBR:
                                selContours = contourSrcs[dataID].GetNearPointContour(hearPoint, CenterDrawMode.BoundingRectCenter); break;

                            case FilterSelectMode.NearPointW:
                                selContours = contourSrcs[dataID].GetNearPointContour(hearPoint, CenterDrawMode.PixelWeightCenter); break;

                            case FilterSelectMode.NearPointH:
                                selContours = contourSrcs[dataID].GetNearPointContour(hearPoint, CenterDrawMode.HistogramCenter); break;

                        }


                        selContours.SetColor(new MCvScalar(IntPropertys["Blue"].Value, IntPropertys["Green"].Value, IntPropertys["Red"].Value));
                        m_contours.Add(selContours);


                        if (DrawOn == FilterDrawMode.Show)
                        {
                            if (SelectMode != FilterSelectMode.MaxArea)
                            {
                                Point ln11 = new Point(IntPropertys["X"].Value, 0);
                                Point ln12 = new Point(IntPropertys["X"].Value, dst.Height);

                                Point ln21 = new Point(0, IntPropertys["Y"].Value);
                                Point ln22 = new Point(dst.Width, IntPropertys["Y"].Value);
                                CvInvoke.Line(dst, ln11, ln12, new MCvScalar(255, 0, 0), 4);
                                CvInvoke.Line(dst, ln21, ln22, new MCvScalar(255, 0, 0), 4);
                            }

                            selContours.Draw(dst, bFill, bShowBoundingRect);
                        }

                        this.m_out.Add(new DataSrc(dst, outData[dataID].Info, false));
                    }
                }
            }
            catch (CvException ex)
            {
                return false;
            }
            return true;
        }
    }


    public class ToAraundContourFilter : BaseFilter, IContourFilter
    {
        List<CvContours> m_contours = new List<CvContours>();

        public List<CvContours> GetResultContours()
        {
            return m_contours;
        }

        public ToAraundContourFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.ContourAraund;
            EnumPropertys["DrawOn"] = new EnumProperty(typeof(FilterDrawMode));
            EnumPropertys["BoundingRect"] = new EnumProperty(typeof(FilterDrawMode));
            EnumPropertys["FillMode"] = new EnumProperty(typeof(FillDrawModeEnum));

            EnumPropertys["CenterMode"] = new EnumProperty(typeof(CenterDrawMode));
            IntPropertys["HScale"] = new NumProperty<int>() { MinVal = 1, MaxVal = 50, Value = 1 };
            FloatPropertys["RScale"] = new NumProperty<double>() { MinVal = 0, MaxVal = 2, Value = 1 };


            EnumPropertys["MovingType"] = new EnumProperty(typeof(MovingPredictType));
            IntPropertys["MovingRange"] = new NumProperty<int>() { MinVal = 1, MaxVal = 10, Value = 1 };
        }

        public override bool Do()
        {
            try
            {
                m_contours.Clear();

                FilterDrawMode DrawOn = (FilterDrawMode)EnumPropertys["DrawOn"].Value;
                CenterDrawMode CenterMode = (CenterDrawMode)EnumPropertys["CenterMode"].Value;


                FilterDrawMode BoundingRect = (FilterDrawMode)EnumPropertys["BoundingRect"].Value;
                bool bShowBoundingRect = BoundingRect == FilterDrawMode.Show;
                FillDrawModeEnum FillMode = (FillDrawModeEnum)EnumPropertys["FillMode"].Value;
                bool bFill = FillMode != FillDrawModeEnum.None;


                MovingPredictType movingType = (MovingPredictType)EnumPropertys["MovingType"].Value;


                foreach (var src in Sources)
                {
                    IContourFilter searchFilter = src as IContourFilter;
                    if (searchFilter == null)
                        continue;
                    var outData = src.GetOut();
                    var contourSrcs = searchFilter.GetResultContours();
                    for (int dataID = 0; dataID < outData.Count; dataID++)
                    {
                        var dst = outData[dataID].Image.Clone();
                        var aroundContours = contourSrcs[dataID].GetAroundContours(  CenterMode, 
                                                                                    IntPropertys["HScale"].Value, 
                                                                                    FloatPropertys["RScale"].Value,
                                                                                    movingType, IntPropertys["MovingRange"].Value
                                                                                    );
                        m_contours.Add(aroundContours);

                        if (DrawOn == FilterDrawMode.Show)
                        {
                            if (FillMode == FillDrawModeEnum.SourceMeanColor)
                                aroundContours.UpdateMainColorAsMeanOfMask(dst);

                            aroundContours.Draw(dst, bFill, bShowBoundingRect, CenterMode);
                        }

                        this.m_out.Add(new DataSrc(dst, outData[dataID].Info, false));
                    }
                }

            }
            catch (CvException ex)
            {
                return false;
            }
            return true;
        }
    }


    public class MergeWithMaskContourFilter : BaseFilter, IContourFilter
    {
        List<CvContours> m_contours = new List<CvContours>();

        public List<CvContours> GetResultContours()
        {
            return m_contours;
        }


        public MergeWithMaskContourFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.ContourMaskMerge;
            EnumPropertys["DrawSource"] = new EnumProperty(graph.GetAddedNames());
            EnumPropertys["Exchange"] = new EnumProperty(typeof(FilterLogicMode));
        }

        public override bool Do()
        {
            try
            {
                var ExchangeMode = (FilterLogicMode)EnumPropertys["Exchange"].Value;

                BaseFilter drawSourceFilter = Graph.GetFilter(EnumPropertys["DrawSource"].Value);               
                if (drawSourceFilter == null)
                    return false;
                List<DataSrc> drawDatas = drawSourceFilter.GetOut();

                foreach (var src in Sources)
                {
                    IContourFilter searchFilter = src as IContourFilter;

                    if (searchFilter == null)
                        continue;

                    m_contours = searchFilter.GetResultContours();

                    int dataID = 0;
                    var outData = src.GetOut();
                    
                    // список коллекций кунтуров сопоставим с imgs
                    while ((dataID < outData.Count) && (dataID < m_contours.Count) && (dataID < drawDatas.Count))
                    {
                        string dataInfo = "";
                        List<Mat> mergeLst = null;
                        switch (ExchangeMode)
                        {
                            case FilterLogicMode.Off:
                                mergeLst = m_contours[dataID].MergeWithContourMask(outData[dataID].Image, drawDatas[dataID].Image);
                                dataInfo = outData[dataID].Info;  //drawDatas[dataID].Info;
                                break;

                            case FilterLogicMode.On:
                                mergeLst = m_contours[dataID].MergeWithContourMask(drawDatas[dataID].Image, outData[dataID].Image);                                
                                dataInfo = outData[dataID].Info;
                                break;
                        }

                        foreach ( var mergeImg in mergeLst)
                            this.m_out.Add(new DataSrc(mergeImg, dataInfo, false));

                        dataID++;
                    }
                }
            }
            catch (CvException ex)
            {
                return false;
            }
            return true;
        }
    }



    public class ContourShiftFilter : BaseFilter, IContourFilter
    {
        List<CvContours> m_contours = new List<CvContours>();

        public List<CvContours> GetResultContours()
        {
            return m_contours;
        }

        public ContourShiftFilter(GraphFilter graph, string name = null) : base(graph, name)
        {
            Type = FilterType.ContourShift;
            EnumPropertys["DrawOn"] = new EnumProperty(typeof(FilterDrawMode));
            EnumPropertys["BoundingRect"] = new EnumProperty(typeof(FilterDrawMode));

            IntPropertys["ShiftX"] = new NumProperty<int>() { MinVal = -2048, MaxVal = 2048, Value = 0 };
            IntPropertys["ShiftY"] = new NumProperty<int>() { MinVal = -2048, MaxVal = 2048, Value = 0 };
        }

        public override bool Do()
        {
            try
            {
                m_contours.Clear();

                FilterDrawMode DrawOn = (FilterDrawMode)EnumPropertys["DrawOn"].Value;
                FilterDrawMode BoundingRect = (FilterDrawMode)EnumPropertys["BoundingRect"].Value;
                bool bShowBoundingRect = BoundingRect == FilterDrawMode.Show;

                foreach (var src in Sources)
                {
                    IContourFilter searchFilter = src as IContourFilter;
                    if (searchFilter == null)
                        continue;
                    var outData = src.GetOut();
                    var contourSrcs = searchFilter.GetResultContours();
                    for (int dataID = 0; dataID < outData.Count; dataID++)
                    {
                        var dst = outData[dataID].Image.Clone();

                        var shiftContours = contourSrcs[dataID].Shift(  IntPropertys["ShiftX"].Value, 
                                                                        IntPropertys["ShiftY"].Value,
                                                                        outData[dataID].Image.Width, outData[dataID].Image.Height );

                        m_contours.Add(shiftContours);

                        if (DrawOn == FilterDrawMode.Show)
                            shiftContours.Draw(dst, false, bShowBoundingRect, CenterDrawMode.BoundingRectCenter);

                        this.m_out.Add(new DataSrc(dst, outData[dataID].Info, false));
                    }
                }

            }
            catch (CvException ex)
            {
                return false;
            }
            return true;
        }
    }
}
