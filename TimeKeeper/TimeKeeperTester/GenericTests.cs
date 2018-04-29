using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeperTester
{
    [TestClass]
    public class GenericTests
    {
        [TestMethod]
        public void TestTypeReturn()
        {
            Type typer = typeof(string);

            Assert.AreEqual(typeof(string), typer.UnderlyingSystemType);
    
        }

    }
}
