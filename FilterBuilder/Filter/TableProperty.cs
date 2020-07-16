using FilterBuilder.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Filter
{
    public class TableProperty<T>
    {
        public T[,] m_table;
        public int Rows { get; private set; }
        public int Columns { get; private set; }
        public T this[int row, int column]
        {
            set {
                if ((row >= Rows) || (column >= Columns))
                    return;
                m_table[row, column] = value;
            }

            get
            {
                if ((row >= Rows) || (column >= Columns))
                    return m_table[0,0];
                return m_table[row, column];
            }
        }
        public string Description { get; set; }
        public TableProperty(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            m_table = new T[Rows, Columns];

            
        }
        public void SetAll(T value)
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Columns; c++)
                    m_table[r, c] = value;
        }
        public override string ToString()
        {
            string str = "\n";
            for (int r = 0; r< Rows;r++ )
            {
                str += "[";
                for (int c = 0; c < Columns; c++)
                    str += m_table[r, c] + " ";
                str += "]\n";
            }
            return str;
        }
    }
}
