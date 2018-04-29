using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeKeeper;

namespace TimeKeeperTester
{
    [TestClass]
    public class UserTesting
    {
        public Guid UserID;

        [TestMethod]
        public void TestUserCreate()
        {
            UserID = Gateway.CreateUser(DateTimeOffset.Now, "Test", "Test");
            Assert.IsNotNull(UserID);
            CleanUp();
        }

        [TestMethod]
        public void TestUserRead()
        {
            Setup();
            object[] results = Gateway.FindUser(UserID)?[0];

            UserID = (Guid)results[0];
            string firstName = (string)results[2];
            string secondName = (string)results[3];

            Assert.IsNotNull(UserID);
            Assert.AreEqual("Test", firstName);
            Assert.AreEqual("Test", secondName);
            CleanUp();
        }


        [TestMethod]
        public void TestUserReadAll()
        {
            Setup();
            List<object[]> results = Gateway.FindAllUsers();
            object[] myUser = null;
            foreach (object[] row in results)
            {
                if ((Guid)row[0] == UserID)
                {
                    myUser = row;
                    break;
                }
            }

            Assert.IsNotNull(myUser);

            string firstName = (string)myUser[2];
            string secondName = (string)myUser[3];

            Assert.AreEqual("Test", firstName);
            Assert.AreEqual("Test", secondName);
            CleanUp();
        }


        [TestMethod]
        public void TestUserUpdate()
        {
            Setup();
            List<object[]> results = Gateway.UpdateUser(UserID, DateTimeOffset.Now, "Test1", "Test1");

            Assert.AreEqual("Test1", (string)results[0][4]);
            Assert.AreEqual("Test1", (string)results[0][5]);
            CleanUp("Test1");
        }


        [TestMethod]
        public void TestUserDelete()
        {
            Setup();
            Guid result = Gateway.DeleteUser(UserID);

            Assert.AreNotEqual(result, Guid.Empty);

            CleanUp();
        }

        public void Setup()
        {
            UserID = Gateway.CreateUser(DateTimeOffset.Now, "Test", "Test");
        }

        public void CleanUp(string name = "Test")
        {
            using (SqlConnection conn = new SqlConnection("Data Source = VELVEETA\\DEVELOPMENT; Initial Catalog = TimeWatcher; Integrated Security = true"))
            {
                SqlCommand cmd = new SqlCommand()
                {
                    Connection = conn,
                    CommandText = "DELETE FROM Dev.Users WHERE FIRST_NAME = '" + name + "' AND LAST_NAME = '" + name + "'",
                    CommandType = CommandType.Text
                };

                conn.Open();

                cmd.ExecuteReader();

                conn.Close();
            }
        }

        /*
         Hi, Youngblood. How are you doing today?

I just wanted to ask you  a question. One of my associates is hosting this event down in Augusta GA, and I've been tasked with finding some musical talent for it. Since the clientele is notably near our age, I was thinking they'd might like our music more. That was when I thought of you. I'm not sure, but I'm curious if you handle hosting events like this. We would handle all your expenses. If you are interested, please contact me using the given information.

Thank you for your time.
         * */

    }
}
