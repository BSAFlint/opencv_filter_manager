using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FilterBuilder.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Filter
{

    // Те кто хочет использовать периметры как доп данные, должны наслеоваться от этого интерфейса
    public interface IRectangleFilter
    {
        List<Rectangle> GetResultRectangles();
    }

    // Фильтр создаёт дополнительные данные на прямоугольную область
    public class RectDataAddFilter : BaseFilter, IRectangleFilter
    {

        List<Rectangle> m_rectangles = new List<Rectangle>();

        public List<Rectangle> GetResultRectangles()
        {
            return m_rectangles;
        }

        public RectDataAddFilter(GraphFilter graph, string name) : base(graph, name)
        {
            Type = FilterType.AddRect;
            EnumPropertys["DrawOn"] = new EnumProperty(typeof(FilterDrawMode));
            IntPropertys["Color"] = new NumProperty<int>() { MinVal = 0, MaxVal = 255, Value = 0 };

            IntPropertys["x"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 0 };
            IntPropertys["y"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 0 };

            IntPropertys["width"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 0 };
            IntPropertys["height"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 0 };

            EnumPropertys["PixelSumCenter"] = new EnumProperty(typeof(FilterLogicMode));
            EnumPropertys["PixelSumCenter"].Set("Off");
        }

        public override bool Do()
        {
            try
            {
                m_rectangles.Clear();
                FilterDrawMode DrawOn = (FilterDrawMode)EnumPropertys["DrawOn"].Value;
                bool bDraw = DrawOn == FilterDrawMode.Show;

                int clVal = IntPropertys["Color"].Value;
                var drawColor = new MCvScalar((clVal * 2) % 255, (clVal * 0.5) % 255, clVal);

                var CenterMode = (FilterLogicMode)EnumPropertys["PixelSumCenter"].Value;

                foreach (var src in Sources)
                    foreach (var outData in src.GetOut())
                    {
                        Mat dst = outData.Image.Clone();

                        int xx = 0;
                        int yy = 0;

                        int ww = IntPropertys["width"].Value;
                        int hh = IntPropertys["height"].Value;

                        switch (CenterMode)
                        {
                            case FilterLogicMode.Off:
                                xx = IntPropertys["x"].Value;
                                yy = IntPropertys["y"].Value;
                                break;

                            case FilterLogicMode.On:
                                var pointCenter = OpenCVHelper.GetXYPixelSumCenter(outData.Image);
                                xx = pointCenter.X - ww / 2;
                                yy = pointCenter.Y - hh / 2;
                                break;
                        }

                        if (xx < 0)
                            xx = 0;

                        if (yy < 0)
                            yy = 0;

                        if (xx > outData.Image.Width)
                            xx = outData.Image.Width;

                        if (yy > outData.Image.Height)
                            xx = outData.Image.Height;

                        if ((ww + xx) > outData.Image.Width)
                            ww = outData.Image.Width - xx;

                        if ((hh + yy) > outData.Image.Height)
                            hh = outData.Image.Height - yy;

                        Rectangle rect = new Rectangle(xx, yy, ww, hh);

                        if (m_rectangles.Count == 0)
                            m_rectangles.Add(rect); // буду делать только один ректангл, на все GetOut's, тут множить смысла не даёю, да лишний CropRectFilter делает

                        if (bDraw)
                            CvInvoke.Rectangle(dst, rect, drawColor, 3);

                        this.m_out.Add(new DataSrc(dst, outData.Info, false));
                    }
            }
            catch { return false; }
            return true;
        }
    }

    public class RectShiftFilter : BaseFilter, IRectangleFilter
    {

        List<Rectangle> m_rectangles = new List<Rectangle>();

        public List<Rectangle> GetResultRectangles()
        {
            return m_rectangles;
        }

        public RectShiftFilter(GraphFilter graph, string name) : base(graph, name)
        {
            Type = FilterType.RectShift;
            EnumPropertys["DrawOn"] = new EnumProperty(typeof(FilterDrawMode));
            IntPropertys["Color"] = new NumProperty<int>() { MinVal = 0, MaxVal = 255, Value = 0 };

            IntPropertys["ShiftX"] = new NumProperty<int>() { MinVal = -2048, MaxVal = 2048, Value = 0 };
            IntPropertys["ShiftY"] = new NumProperty<int>() { MinVal = -2048, MaxVal = 2048, Value = 0 };

            if (graph != null)
            {
                var names = graph.GetAddedNames();
                EnumPropertys["DrawSource"] = new EnumProperty(names);

                if (names.Length > 0)
                    EnumPropertys["DrawSource"].Set(names[names.Length - 1]);
            }
        }


        public override bool Do()
        {
            try
            {
                m_rectangles.Clear();
                FilterDrawMode DrawOn = (FilterDrawMode)EnumPropertys["DrawOn"].Value;
                bool bDraw = DrawOn == FilterDrawMode.Show;

                int clVal = IntPropertys["Color"].Value;
                var drawColor = new MCvScalar((clVal * 2) % 255, (clVal * 0.5) % 255, clVal);


                BaseFilter drawSourceFilter = Graph.GetFilter(EnumPropertys["DrawSource"].Value);
                if (drawSourceFilter == null)
                    return false;


                foreach (var src in Sources)
                {
                    IRectangleFilter rectFilter = src as IRectangleFilter;
                    if (rectFilter == null)
                        continue;

                    src.GetOut(); // нужно обновить принудительно


                    foreach (var outData in drawSourceFilter.GetOut())
                        foreach(var rct in rectFilter.GetResultRectangles())
                        {
                            Mat dst = outData.Image.Clone();
                            int xx = rct.X + IntPropertys["ShiftX"].Value;
                            int yy = rct.Y + IntPropertys["ShiftY"].Value;
                            int ww = rct.Width;
                            int hh = rct.Height;

                            if (xx > outData.Image.Width)
                                xx = outData.Image.Width;

                            if (yy > outData.Image.Height)
                                xx = outData.Image.Height;

                            if ((ww + xx) > outData.Image.Width)
                                ww = outData.Image.Width - xx;

                            if ((hh + yy) > outData.Image.Height)
                                hh = outData.Image.Height - yy;

                            Rectangle rect = new Rectangle(xx, yy, ww, hh);

                            if (m_rectangles.Count == 0)
                                m_rectangles.Add(rect); // буду делать только один ректангл, на все GetOut's, тут множить смысла не даёю, да лишний CropRectFilter делает

                            if (bDraw)
                                CvInvoke.Rectangle(dst, rect, drawColor, 3);

                            this.m_out.Add(new DataSrc(dst, outData.Info, false));
                        }
                }
            }
            catch { return false; }
            return true;
        }
    }


    public class CropRectFilter : BaseFilter
    {
        public CropRectFilter(GraphFilter graph, string name) : base(graph, name)
        {
            Type = FilterType.CropRect;
        }

        public static Mat RioImg(Mat img, Rectangle rect)
        {
            int xx = rect.X;
            int yy = rect.Y;
            int ww = rect.Width;
            int hh = rect.Height;

            if (xx > img.Width)
                xx = img.Width;

            if (yy > img.Height)
                xx = img.Height;

            if ((ww + xx) > img.Width)
                ww = img.Width - xx;

            if ((hh + yy) > img.Height)
                hh = img.Height - yy;
            Rectangle usedRect = new Rectangle(xx, yy, ww, hh);


            Image<Bgr, Byte> buffer_im = img.ToImage<Bgr, Byte>();
            buffer_im.ROI = usedRect;
            Image<Bgr, Byte> cropped_im = buffer_im.Copy();
            return cropped_im.Mat;
        }

        public override bool Do()
        {
            try
            {
                foreach (var src in Sources)
                {
                    IRectangleFilter rectFilter = src as IRectangleFilter;
                    if (rectFilter == null)
                        continue;

                    var outData = src.GetOut();
                    var contourSrcs = rectFilter.GetResultRectangles();

                    for (int dataID = 0; dataID < outData.Count; dataID++)
                        foreach (var useRect in contourSrcs)
                        {
                            Mat dst = RioImg(outData[dataID].Image, useRect);
                            this.m_out.Add(new DataSrc(dst, outData[dataID].Info, false));
                        }
                }
            }
            catch { return false; }
            return true;
        }
    }


    public enum RectExportMode
    {
        EachBoundingCenterAbs = 0,
        EachWeightCenterAbs = 1,
        EachBoundingCenterScale = 2, // НЕТ ВЕРСИИ СО СМЕЩЁННЫМ ЦЕНТРОМ, Т.К. ТАМ НЕ ЯСНО ЧТО ИСПОЛЬЗОВАТЬ ЗА SIZE
        ConcatenateBoundingCenterAbs = 3,
        ConcatenateeWeightCenterAbs = 4
    }



    /// <summary>
    /// Вытаскивает из контуров их BoundingRect, для использования кропа CropRectFilter
    /// рендер - Ректы буду привязаны к каналам кнтуров, а в список ону будут добавленны по очереди Src.Img.Rect
    /// </summary>
    public class RectExportFilter : BaseFilter, IRectangleFilter
    {
        List<Rectangle> m_rectangles = new List<Rectangle>();

        public List<Rectangle> GetResultRectangles()
        {
            return m_rectangles;
        }


        public RectExportFilter(GraphFilter graph, string name) : base(graph, name)
        {
            Type = FilterType.RectExport;
            EnumPropertys["DrawOn"] = new EnumProperty(typeof(FilterDrawMode));
            EnumPropertys["ExportMode"] = new EnumProperty(typeof(RectExportMode));

            IntPropertys["AbsWidth"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 100 };
            IntPropertys["AbsHeight"] = new NumProperty<int>() { MinVal = 0, MaxVal = 4096, Value = 100 };

            FloatPropertys["ScaleWidth"] = new NumProperty<double>() { MinVal = 0, MaxVal = 20, Value = 1 };
            FloatPropertys["ScaleHeight"] = new NumProperty<double>() { MinVal = 0, MaxVal = 20, Value = 1 };
        }

        public override bool Do()
        {
            try
            {
                m_rectangles.Clear();
                FilterDrawMode DrawOn = (FilterDrawMode)EnumPropertys["DrawOn"].Value;
                RectExportMode ExportMode = (RectExportMode)EnumPropertys["ExportMode"].Value;

                int drawSubID = 0;
                foreach (var src in Sources)
                {
                    var outData = src.GetOut();

                    IContourFilter contourFilter = src as IContourFilter;
                    if (contourFilter == null)
                        continue;

                    var contourOfImgs = contourFilter.GetResultContours();

                    for (int dataID = 0; dataID < outData.Count; dataID++)
                    {
                        switch(ExportMode)
                        {
                            case RectExportMode.ConcatenateBoundingCenterAbs:
                            case RectExportMode.ConcatenateeWeightCenterAbs:
                                m_rectangles.Add(
                                contourOfImgs[dataID].GeRectWithConcatenateCentert( IntPropertys["AbsWidth"].Value,
                                                                                   IntPropertys["AbsHeight"].Value,
                                                                                   ExportMode == RectExportMode.ConcatenateeWeightCenterAbs
                                                                                   ));
                                break;
                            case RectExportMode.EachBoundingCenterAbs:
                            case RectExportMode.EachWeightCenterAbs:
                                foreach (var rect in contourOfImgs[dataID].ExportRects( IntPropertys["AbsWidth"].Value,
                                                                                       IntPropertys["AbsHeight"].Value,
                                                                                       ExportMode == RectExportMode.EachWeightCenterAbs))
                                    m_rectangles.Add(rect);
                                break;

                            case RectExportMode.EachBoundingCenterScale:
                                foreach(var rect in contourOfImgs[dataID].ExportBoundingRects(FloatPropertys["ScaleWidth"].Value, FloatPropertys["ScaleHeight"].Value))
                                    m_rectangles.Add(rect);
                                break;
                        }



                        var dst = outData[dataID].Image.Clone();

                        if (DrawOn == FilterDrawMode.Show)
                        for(int rectID = drawSubID; rectID< m_rectangles.Count; rectID++)
                            {
                                MCvScalar color = new MCvScalar((rectID * 23) % 255, (rectID * 44) % 255, ((rectID + 33) * 4) % 255);
                                CvInvoke.Rectangle(dst, m_rectangles[rectID], color, 3);
                            }
                        drawSubID = m_rectangles.Count;

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
