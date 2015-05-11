using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Logic.Commands.UI
{
    public class UICommandHelper
    {
        string locatorType { get; set; }

        public void ExtractMethodName(string tarString,out string methodName,out string target)
        {
            try
            {
                if (tarString.Contains('='))
                {
                    locatorType = tarString.Split('=')[0];
                    switch (locatorType.ToLower())
                    {
                        case "css":
                            methodName = "CssSelector";
                            target = Regex.Replace(tarString,"css=", "", RegexOptions.IgnoreCase);
                            break;
                        case "link":
                            methodName = "LinkText";
                            target = Regex.Replace(tarString, "link=", "", RegexOptions.IgnoreCase);
                            break;
                        case "xpath":
                            methodName = "XPath";
                            target = Regex.Replace(tarString, "xpath=", "", RegexOptions.IgnoreCase);
                            break;
                        case "name":
                            methodName = "Name";
                            target = Regex.Replace(tarString, "name=", "", RegexOptions.IgnoreCase);
                            break;
                        case "id":
                            methodName = "Id";
                            target = Regex.Replace(tarString, "id=", "", RegexOptions.IgnoreCase);
                            break;
                        default:
                            throw (new Exception("The locator value shoule have prefix like xpath=,css=,link=,id=,name="));
                    }
                }
                else
                {
                    throw (new Exception("The locator value shoule have prefix like xpath=,css=,link=,id=,name="));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
