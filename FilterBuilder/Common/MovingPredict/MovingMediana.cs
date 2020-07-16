using FilterBuilder.Common.MovingPredict;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Common
{
    public class MovingMediana : IMovingPredict
    {
        double[]    m_listOfValues;         // физически не перемещаются
        List<int>   m_sortedIndexList;      // будем сортировать не сам накопител, а только массив их индексов.

        int m_size;
        int m_maxIndex;
        int m_medianId;
        int m_insertPos;

        public void Clear()
        {
            m_insertPos = 0;
            m_sortedIndexList.Clear();

            for (int n = 0; n < m_size; n++)
            {
                m_listOfValues[n] = 0;
                m_sortedIndexList.Add(n);
            }
        }

        public MovingMediana(int size)
        {
            m_size = size;
            m_maxIndex = m_size - 1;
            m_medianId = m_size / 2;

            m_sortedIndexList = new List<int>();
            m_listOfValues = new double[size];
            Clear();
        }

        public double Predict(double x)
        {
            // remove
            int nRemoveIndex = 0;
            while ((nRemoveIndex < m_maxIndex) && (m_sortedIndexList[nRemoveIndex] != m_insertPos))
                nRemoveIndex++;
            m_sortedIndexList.RemoveAt(nRemoveIndex);

            // insert
            int nInsertIndex = 0;
            while ((nInsertIndex < m_maxIndex) && (m_listOfValues[m_sortedIndexList[nInsertIndex]] > x)) nInsertIndex++;
            m_sortedIndexList.Insert(nInsertIndex, m_insertPos);

            m_listOfValues[m_insertPos] = x;
            m_insertPos = (m_insertPos + 1) % m_size;

            return m_listOfValues[m_sortedIndexList[m_medianId]];
        }


        public double CurrentVal()
        {
            return m_listOfValues[m_sortedIndexList[m_medianId]];
        }
    }
}
