using System.Data;

namespace addresses;

public class NameIndexer
{
    readonly Dictionary<string, int> DictNameToIndex = new();
    int nextChildIndex = 0;

    public NameIndexer() { }

    public void Clear()
    {
        DictNameToIndex.Clear();
        nextChildIndex = 0;
    }

    public int[] GetNameIndices(DataRow r)
    {
        List<string> names = new();
        string[] colNames = { ColumnName.ChildFirst1,
                              ColumnName.ChildFirst2,
                              ColumnName.ChildFirst3,
                              ColumnName.ChildFirst4,
                              ColumnName.ChildFirst5,
                              ColumnName.ChildFirst6,
                              ColumnName.ChildFirst7,
                              ColumnName.ChildFirst8,
                              ColumnName.ChildFirst9,
                              ColumnName.ChildFirst10,};
        foreach (var colName in colNames)
        {
            if (r[colName] is not DBNull)
            {
                var name = (string)r[colName];
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
            }
        }

        var indexes = new int[names.Count];
        int index = 0;

        foreach (var name in names)
        {
            if (DictNameToIndex.TryGetValue(name, out int nameIndex))
            {
                indexes[index] = nameIndex;
                index++;
            }
            else
            {
                int newIndex = nextChildIndex++;
                DictNameToIndex.Add(name, newIndex);
                indexes[index] = newIndex;
                index++;
            }
        }

        return indexes;
    }

}
