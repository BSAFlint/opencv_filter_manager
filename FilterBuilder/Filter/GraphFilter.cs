using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Filter
{

    public class GraphFilter
    {
        private List<BaseFilter> m_filterList;
        private static BaseFilter CreateFilter(FilterType type, GraphFilter graph = null, string name = null)
        {
            switch(type)
            {
                case FilterType.Source: return new MatSourceFilter(graph, name);
                case FilterType.BGRBlank: return new CreateBgrFilter(graph, name);
                case FilterType.File: return new LoadBgrFilter(graph, name);
                case FilterType.SourceSwipe: return new SourceSwipeFilter(graph, name);
                case FilterType.Split: return new SplitFilter(graph, name);
                case FilterType.Select: return new SelectFilter(graph, name);
                case FilterType.Append: return new AppendSourceFilter(graph, name);
                case FilterType.Merge: return new MergeFilter(graph, name);
                case FilterType.MergeInSrc: return new MergeInSrcFilter(graph, name);
                case FilterType.Negative: return new NegativeFilter(graph, name);                    
                case FilterType.Bgr2Gray: return new Bgr2GrayFilter(graph, name);
                case FilterType.Gray2Bgr: return new Gray2BgrFilter(graph, name);
                case FilterType.ResizeAbs: return new ResizeAbsFilter(graph, name);
                case FilterType.Resize: return new ResizeFilter(graph, name);
                case FilterType.Rotation: return new RotationFilter(graph, name);
                case FilterType.Blur: return new BlurFilter(graph, name);
                case FilterType.Morphology: return new MorphologyFilter(graph, name);
                case FilterType.Threshold: return new ThresholdFilter(graph, name);
                case FilterType.Gradient: return new GradientFilter(graph, name);
                case FilterType.Conv2D3K: return new Conv2DFilter(graph, name, 3) { Type = FilterType.Conv2D3K};
                case FilterType.Conv2D5K: return new Conv2DFilter(graph, name, 5) { Type = FilterType.Conv2D5K }; ;
                case FilterType.Conv2D7K: return new Conv2DFilter(graph, name, 7) { Type = FilterType.Conv2D7K }; ;
                case FilterType.PixelSum: return new PixelSumFilter(graph, name);
                case FilterType.AddRect: return new RectDataAddFilter(graph, name);
                case FilterType.RectShift: return new RectShiftFilter(graph, name);
                case FilterType.CropRect: return new CropRectFilter(graph, name);
                case FilterType.RectExport: return new RectExportFilter(graph, name);
                case FilterType.Histogram: return new HistogramFilter(graph, name);
                case FilterType.ContourSearch: return new ContourSearchFilter(graph, name);
                case FilterType.ContourSort: return new ContourSortFilter(graph, name);
                case FilterType.ContourAreaSelect: return new ContourAreaSelectFilter(graph, name);
                case FilterType.ContourSingleSelect: return new SelectSingleContourFilter(graph, name);
                case FilterType.ContourAraund: return new ToAraundContourFilter(graph, name);
                case FilterType.ContourShift: return new ContourShiftFilter(graph, name);
                case FilterType.ContourMaskMerge: return new MergeWithMaskContourFilter(graph, name);
                case FilterType.LUT: return new BgrLUTFilter(graph, name);
            }
            return null;
        }
        private static BaseFilter CreateFilter(string type, GraphFilter graph = null, string name = null)
        {            
            string[] names = Enum.GetNames(typeof(FilterType));
            var values = (int[])Enum.GetValues(typeof(FilterType));
            for (int n = 0; n < values.Length; n++)
                if (names[n] == type)
                    return CreateFilter((FilterType)values[n], graph, name);
            return null;
        }

        public static string[] GetFilterNames ()
        {
            return Enum.GetNames(typeof(FilterType));
        }

        public string[] GetAddedNames()
        {
            string[] names = new string[m_filterList.Count];
            for (int i = 0; i < m_filterList.Count; i++)
                names[i] = m_filterList[i].Name;
            return names;
        }

        public BaseFilter FindFilter(string name)
        {
            foreach (var filter in m_filterList)
                if (filter.Name == name)
                    return filter;
            return null;
        }

        public BaseFilter FindFirtsFilter(FilterType type)
        {
            foreach (var filter in m_filterList)
                if (filter.Type == type)
                    return filter;
            return null;
        }

        public BaseFilter FindLastFilter(FilterType type)
        {
            BaseFilter ans = null;
            foreach (var filter in m_filterList)
                if (filter.Type == type)
                    ans = filter;
            return ans;
        }

        // Нужна для enum вызова из проперти
        public BaseFilter GetFilter(int id)
        {
            if (m_filterList.Count <= id)
                return null;
            return m_filterList[id];
        }

        // получает все хвотсовые части
        public List<BaseFilter> GetTails()
        {
            List<BaseFilter> lst = new List<BaseFilter>();
            foreach (var filter in m_filterList)
                if (filter.GetChildrenCnt == 0)
                    lst.Add(filter);
            return lst;
        }

        public List<DataSrc> GetOuts()
        {
            List<DataSrc> lst = new List<DataSrc>();
            foreach(var filter in GetTails())
                lst.AddRange(filter.GetOut());
            return lst;
        }
        // Dispoce

        public GraphFilter()
        {
            m_filterList = new List<BaseFilter>();
        }

        public BaseFilter Add(FilterType type, string name = null)
        {
            var filter = CreateFilter(type, this, name);
            m_filterList.Add(filter);
            return filter;
        }


        public BaseFilter Add(string type, string name = null)
        {
            var filter = CreateFilter(type, this, name);
            if (filter!=null)
                m_filterList.Add(filter);
            return filter;
        }

        // Удаляет по ссылке из графа, удаляет себя из всех фильтров-источников
        public bool RemoveFromTail(BaseFilter filter)
        {
            if (filter.GetChildrenCnt > 0)
                return false;

            filter.DisconnectSelfFromSource();
            m_filterList.Remove(filter);
            return true;
        }
    }
}
