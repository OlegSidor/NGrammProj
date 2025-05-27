using System;
using System.Collections;
using System.Windows.Forms;

public class CollocationListViewSorter : IComparer
{
    private int columnIndex;
    private bool ascending;

    public CollocationListViewSorter(int columnIndex, bool ascending = true)
    {
        this.columnIndex = columnIndex;
        this.ascending = ascending;
    }

    public int Compare(object x, object y)
    {
        ListViewItem item1 = (ListViewItem)x;
        ListViewItem item2 = (ListViewItem)y;

        string val1 = item1.SubItems[columnIndex].Text;
        string val2 = item2.SubItems[columnIndex].Text;

        int result;

        // Числове сортування для колонок # або Частота
        if (columnIndex == 0 || columnIndex == 2)
        {
            if (int.TryParse(val1, out int n1) && int.TryParse(val2, out int n2))
                result = n1.CompareTo(n2);
            else
                result = string.Compare(val1, val2, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            // Стандартне алфавітне сортування
            result = string.Compare(val1, val2, StringComparison.OrdinalIgnoreCase);
        }

        return ascending ? result : -result;
    }
}
