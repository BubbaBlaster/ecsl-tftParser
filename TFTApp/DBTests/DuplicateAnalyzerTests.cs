using static Agora.SDK;
using addresses;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace TFTApp_Tests
{
    [TestClass]
    public class DuplicateAnalyzerTests
    {
        DataManager DM;
        public DuplicateAnalyzerTests()
        {
            DM = DataManager.Instance;
            DM.Initialize();
            Assert.IsNotNull(DM.CurrentYearDB);
            Assert.AreEqual("TestData.csv", DM.CurrentYearDB.TFTFilename);
            Assert.AreNotEqual(0, DM.CurrentYearDB.Data.Rows.Count,
                $"Cannot read '{DM.CurrentYearDB.TFTFilename}'.  Do you have it open?");
        }

        [TestMethod]
        public void TestAreNumbersUnique()
        {
            int[] unique = new int[] { 0 };
            Assert.IsTrue(DuplicatesAnalyzer.AreNumbersUnique(unique));
            unique = new int[] { };
            Assert.IsTrue(DuplicatesAnalyzer.AreNumbersUnique(unique));
            unique = new int[] { 1, 2, 3 };
            Assert.IsTrue(DuplicatesAnalyzer.AreNumbersUnique(unique));
            unique = new int[] { 3, 2, 1 };
            Assert.IsTrue(DuplicatesAnalyzer.AreNumbersUnique(unique));
            unique = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Assert.IsTrue(DuplicatesAnalyzer.AreNumbersUnique(unique));

            int[] notUnique = new int[] { 0, 0 };
            Assert.IsFalse(DuplicatesAnalyzer.AreNumbersUnique(notUnique));
            notUnique = new int[] { 0, 1, 0 };
            Assert.IsFalse(DuplicatesAnalyzer.AreNumbersUnique(notUnique));
            notUnique = new int[] { 0, 1, 1 };
            Assert.IsFalse(DuplicatesAnalyzer.AreNumbersUnique(notUnique));
            notUnique = new int[] { 0, 0, 2, 3, 4, 5, 3 };
            Assert.IsFalse(DuplicatesAnalyzer.AreNumbersUnique(notUnique));
        }

        [TestMethod]
        public void TestAnalyzer()
        {
            DuplicatesAnalyzer DA = new(DM.CurrentYearDB);
            DM.Errors.Clear();

            DA.Run();

            Assert.AreEqual(1, DM.Errors.Count);
        }
    }
}