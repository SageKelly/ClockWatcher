using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeKeeper;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace TimeKeeperTester
{
    [TestClass]
    public class SessionTesting
    {

        Guid SessionID;

        [TestMethod]
        public void TestSessionCreate()
        {
            SessionID = Gateway.CreateSession(DateTimeOffset.Now, Guid.Empty);
            Assert.IsNotNull(SessionID);
            Cleanup(SessionID);
        }

        public void TestSessionCreate2()
        {
            SessionID = Gateway.CreateSession(DateTimeOffset.Now);
            Assert.IsNotNull(SessionID);
            Cleanup(SessionID);
        }

        [TestMethod]
        public void TestSessionRead()
        {
            Setup();
            List<object[]> result = Gateway.FindSession(SessionID);

            if (result != null)
            {
                Assert.AreEqual(SessionID, (Guid)result[0][0]);
            }

            Assert.IsNotNull(result);
            Cleanup(SessionID);
        }

        [TestMethod]
        public void TestSessionReadAll()
        {
            Setup();
            List<object[]> result = Gateway.FindAllSessions();

            if (result != null)
            {
                foreach (object[] row in result)
                {
                    Debug.WriteLine("{0}\t{1}\t{2}",
                        row[0].ToString(),
                        row[1].ToString(),
                        row[2].ToString());
                }
            }

            Assert.IsNotNull(result);
            Cleanup(SessionID);
        }

        [TestMethod]
        public void TestSessionUpdate()
        {
            Setup();
            DateTimeOffset curTime = DateTimeOffset.Now;
            List<object[]> result = Gateway.UpdateSession(SessionID, curTime, curTime, Guid.Empty);

            //1256
            Assert.AreEqual(curTime.DateTime.ToString(), result[0][1].ToString());

            Cleanup(SessionID);
        }

        [TestMethod]
        public void TestSessionDelete()
        {
            Setup();
            Guid DeletedSession = Guid.Empty;
            if (SessionID != null)
            {
                DeletedSession = Gateway.DeleteSession(SessionID);
            }
            Assert.AreNotEqual(Guid.Empty, DeletedSession);
            Cleanup(SessionID);
        }
        

        public void Setup()
        {
            SessionID = Gateway.CreateSession(DateTimeOffset.Now, Guid.Empty);
        }

        public void Cleanup(Guid session)
        {
            Gateway.DeleteSession(session);
        }


    }
}
