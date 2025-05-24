using System;
using System.Collections;
using System.Windows.Forms;

namespace NGramm
{
    public class CollocationListViewSorter : IComparer
    {
        private readonly int col;

        public CollocationListViewSorter(int column)
        {
            col = column;
        }

        public int Compare(object x, object y)
        {
            string a = ((ListViewItem)x).SubItems[col].Text;
            string b = ((ListViewItem)y).SubItems[col].Text;

            // Сортування як текст (лексикографічно), включно з колонкою "#"
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}