using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;
using Logic;
using System.IO;

namespace Test
{
    [TestClass]
    public class TestArchitecture
    {
        [TestMethod]
        public void TestMethod1()
        {
            Func<XAttribute, Boolean> tmpIdFilter = Helper.CreateIdFilterPredicate(new[] { "SMP003" });
            Func<XAttribute, Boolean> tmpTypeFilter = Helper.CreateTagFilterPredicate(new[] { "Query" });
            var iniFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".\Data\Environment\Setting_Sample.config");

            Logging.LoadLog(iniFilePath);
            Logging.SaveLog("Console started!", ELogType.Info);

            TestContainer tmp = new TestContainer(iniFilePath, new[] { tmpIdFilter, tmpTypeFilter });
            tmp.StartTest();
        }
    }
}
