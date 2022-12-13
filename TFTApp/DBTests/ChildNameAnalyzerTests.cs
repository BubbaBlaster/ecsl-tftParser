using static Agora.SDK;
using addresses;
using Microsoft.Extensions.Configuration;


namespace TFTApp_Tests
{
    [TestClass]
    public class ChildNameAnalyzerTests
    {
        DataManager DM;
        public ChildNameAnalyzerTests() 
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
        public void TestChildFirstNameAnalyzer()
        {
            ChildFirstNameAnalyzer.Run( DM.CurrentYearDB! );
            Assert.AreEqual(1, DM.Errors.Count);
            Assert.AreEqual(2, DM.Errors[DM.CurrentYearDB.TFTFilename].Count);
        }
    }
}