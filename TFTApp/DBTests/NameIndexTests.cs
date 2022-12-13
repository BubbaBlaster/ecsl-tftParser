using static Agora.SDK;
using addresses;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace TFTApp_Tests;

[TestClass]
public class NameIndexerTests
{
    DataManager DM;
    public NameIndexerTests()
    {
        DM = DataManager.Instance;
        DM.Initialize();
        Assert.IsNotNull(DM.CurrentYearDB);
        Assert.AreEqual("TestData.csv", DM.CurrentYearDB.TFTFilename);
        Assert.AreNotEqual(0, DM.CurrentYearDB.Data.Rows.Count,
            $"Cannot read '{DM.CurrentYearDB.TFTFilename}'.  Do you have it open?");
        Assert.AreEqual(18, DM.CurrentYearDB.Data.Rows.Count);
        Assert.AreEqual(81, DM.CurrentYearDB.Data.Columns.Count);
    }

    [TestMethod]
    public void TestNameIndexer()
    {
        NameIndexer indexer = new();

        var row = DM.CurrentYearDB.Data.NewRow();
        row[ColumnName.ChildFirst1] = "N1";
        row[ColumnName.ChildFirst2] = "N1";
        row[ColumnName.ChildFirst3] = "N1";
        row[ColumnName.ChildFirst4] = "N1";

        var indexes = indexer.GetNameIndices(row);
        Assert.AreEqual(4, indexes.Length);
        Assert.AreEqual(0, indexes[0]);
        Assert.IsTrue(indexes[0] == indexes[1] &&
                      indexes[0] == indexes[2] &&
                      indexes[0] == indexes[3]);
        int index0 = indexes[0];

        row[ColumnName.ChildFirst5] = "N2";

        indexes = indexer.GetNameIndices(row);
        Assert.AreEqual(5, indexes.Length);
        Assert.AreEqual(0, indexes[0]);
        Assert.AreEqual(1, indexes[4]);
        Assert.IsTrue(indexes[0] == indexes[1] &&
                      indexes[0] == indexes[2] &&
                      indexes[0] == indexes[3] &&
                      index0 == indexes[0]);
        Assert.IsTrue(indexes[0] != indexes[4]);
    }
}