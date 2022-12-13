using static Agora.SDK;
using addresses;
using Microsoft.Extensions.Configuration;


namespace TFTApp_Tests
{
    [TestClass]
    public class DBTests
    {
        DataManager DM;
        public DBTests() 
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
        public void TestPretty()
        {
            Assert.AreEqual("Happy", Utilities.Pretty("happY"));
            Assert.AreEqual("123 Happy", Utilities.Pretty("123 happY"));
            Assert.AreEqual("Charolette, Happy", Utilities.Pretty("charolette,happY"));
        }

        [TestMethod]
        public void TestPhone()
        {
            Assert.AreEqual("123-456-7890", Utilities.PhoneString("1234567890"));
            Assert.AreEqual("123-456-7890", Utilities.PhoneString("123.456.7890"));
            Assert.AreEqual("123-456-7890", Utilities.PhoneString("123-456-7890"));
            Assert.AreEqual("123-456-7890", Utilities.PhoneString(" 123 4567890"));
            Assert.AreEqual("1 123 4567890", Utilities.PhoneString(" 1 123 4567890"));
        }
    }
}