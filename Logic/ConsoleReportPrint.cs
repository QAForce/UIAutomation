using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic
{
    public static class ConsoleReportPrint
    {
        public static void TestCaseStartPrint(string id,string description)
        {
            Console.WriteLine("Test Case:" + id + ":" + description);
        }

        public static void TestCaseCompletePrint(string id, bool result)
        {
            Console.WriteLine("Test Result:" + id.PadRight(80, '.') + (result ? "Pass" : "Fail"));
        }

        public static void CaseCommandPrint(string cmdId, string cmdDescription,bool isSkip, bool result)
        {
            string prefix = "[" + cmdId + "]:" + cmdDescription;
            int maxLength = (prefix.Length > 75 ? 75 : prefix.Length);
            Console.WriteLine(string.Empty.PadRight(5, ' ') + prefix.Substring(0, maxLength).PadRight(75, '.') + (isSkip ? "Skip" : (result ? "Pass" : "Fail")));
        }

        public static void VirtualCommandPrint(string cmdId,string description)
        {
            Console.WriteLine(string.Empty.PadRight(5, ' ') + "["+cmdId + "]:" + description);
        }

        public static void VirtualCommandDetailPrint(string cmdId, string cmdDescription,bool isSkip, bool result)
        {
            string prefix = "[" + cmdId + "]:" + cmdDescription;
            int maxLength = (prefix.Length > 70 ? 70 : prefix.Length);
            Console.WriteLine(string.Empty.PadRight(10, ' ') + prefix.Substring(0, maxLength).PadRight(75, '.') + (isSkip ? "Skip" : (result ? "Pass" : "Fail")));
        }

        
    }
}
