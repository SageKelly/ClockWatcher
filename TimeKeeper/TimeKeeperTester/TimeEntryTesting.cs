using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeKeeper;

namespace TimeKeeperTester
{
    [TestClass]
    public class TimeEntryTesting
    {
        Guid EntryID;
        Guid SessionID;
        const string TESTING = "Testing";

        public void Setup(bool timeEntry = true)
        {
            SessionID = Gateway.CreateSession(DateTimeOffset.Now, Guid.Empty);
            EntryID = Gateway.CreateTimeEntry(DateTimeOffset.Now, DateTimeOffset.Now, TESTING, SessionID);
        }

        [TestMethod]
        public void TestTimeEntryCreate()
        {
            Setup(false);
            Assert.AreNotEqual(Guid.Empty, SessionID);

            EntryID = Gateway.CreateTimeEntry(DateTimeOffset.Now, DateTimeOffset.Now, TESTING, SessionID);

            Assert.AreNotEqual(Guid.Empty, EntryID);

            Cleanup();
        }

        [TestMethod]
        public void TestTimeEntryCreateNoComment()
        {
            Setup(false);
            Assert.AreNotEqual(Guid.Empty, SessionID);

            EntryID = Gateway.CreateTimeEntry(DateTimeOffset.Now, DateTimeOffset.Now, TESTING, SessionID);

            Assert.AreNotEqual(Guid.Empty, EntryID);

            Cleanup();
        }

        [TestMethod]
        public void TestTimeEntryRead()
        {
            Setup();

            object[] result = Gateway.FindTimeEntry(EntryID);

            Assert.AreNotEqual(0, result.Length);


            Assert.AreEqual(EntryID, (Guid)result[0]);

            Cleanup();
        }

        [TestMethod]
        public void TestTimeEntryReadAll()
        {
            Setup();

            List<object[]> result = Gateway.FindAllTimeEntries();

            Assert.AreNotEqual(0, result.Count);
            Assert.AreNotEqual(0, result[0].Length);

            Cleanup();
        }

        [TestMethod]
        public void TestTimeEntryUpdate()
        {
            Setup();

            DateTimeOffset curtime = DateTimeOffset.Now;

            List<object[]> result = Gateway.UpdateTimeEntry(EntryID, curtime, curtime, "Testing", SessionID);

            Assert.AreNotEqual(0, result.Count);

            Assert.AreEqual(4, result[0].Length);

            Assert.AreEqual(TESTING, result[0][2]);
            Assert.AreEqual(curtime.DateTime.ToString(), result[0][1].ToString());

            Cleanup();
        }

        [TestMethod]
        public void TestTimeEntryDelete()
        {
            Setup();

            Guid result = Gateway.DeleteTimeEntry(EntryID);
            Assert.AreEqual(result, EntryID);

            Cleanup();
        }

        public void Cleanup()
        {
            Gateway.DeleteSession(SessionID);
            Gateway.DeleteTimeEntry(EntryID);
        }

    }
}
