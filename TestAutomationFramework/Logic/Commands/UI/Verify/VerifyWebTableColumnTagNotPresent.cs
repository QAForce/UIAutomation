using Logic.Commands.UI;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Commands
{
    public class VerifyWebTableColumnTagNotPresent:Command
    {
        public VerifyWebTableColumnTagNotPresent(String id, String description, KeyValuePair<String, Tuple<Type, String>> output, Dictionary<String, Tuple<Type, String>> parameters)
            : base(id, description, output, parameters)
        {

        }

        public override void Run(TestContainer container)
        {
            try
            {
                //* add for IsExecuteCommand start
                if (!base.IsCommandContinue(container))
                {
                    this.PassTest = true;
                    this.SkipTest = true;
                    return;
                }
                //* add for IsExecuteCommand end

                string invokeMethodName;
                string invokeParameter;

                UICommandHelper cmdHelp = new UICommandHelper();
                cmdHelp.ExtractMethodName(base.GetParameter("Target"), out invokeMethodName, out invokeParameter);

                By by = typeof(By).GetMethod(invokeMethodName).Invoke(null, new[] { invokeParameter }) as By;

                WebDriverWait wait = new WebDriverWait(container.Driver, TimeSpan.FromSeconds(container._commandTimeout));
                IWebElement elem = wait.Until(ExpectedConditions.ElementIsVisible(by));

                bool bolPass = true;

                List<IWebElement> lstRows = elem.FindElements(By.TagName("tr")).ToList();
                string actualValue = "";
                for (int i = 1; i < lstRows.Count(); i++) //ignore header, start at row 1
                {
                    List<IWebElement> lstCells = lstRows[i].FindElements(By.TagName("td")).ToList();
                    try
                    {
                        if (int.Parse(base.GetParameter("ColumnName").ToUpper().Replace("COLUMN", "")) >= lstCells.Count)
                        {
                            bolPass = false;
                        }
                        else if(int.Parse(base.GetParameter("ColumnName").ToUpper().Replace("COLUMN", ""))<0)
                        {
                            bolPass = false;
                        }
                    }
                    catch
                    {
                        bolPass = false;
                    }

                    for (int j = 0; j < lstCells.Count; j++)
                    {
                        if (base.GetParameter("ColumnName").ToUpper().Equals("COLUMN" + j.ToString()))
                        {
                            try
                            {
                                IWebElement chkElem = lstCells[j].FindElement(By.TagName(base.GetParameter("TagName")));
                                bolPass = false;
                                actualValue = lstCells[j].GetAttribute("innerHTML") + ",";
                            }
                            catch
                            {

                            }
                        }
                    }
                }
                

                this.PassTest = bolPass;

                //* add for output and IsExpectedFail start
                this.Output = base.GetOutPut(this.Output.Key, this.Output, this.PassTest);
                //* add for output and IsExpectedFail end

                //* add for output and IsExpectedFail start
                this.PassTest = GetTestPassExpected(this.PassTest);
                //* add for output and IsExpectedFail end

                //add by zhuqianqian ScreenShot start
                if (!this.PassTest)
                {
                    CommandFailScreenShot(container);
                    Logging.SaveLog("CommandId:" + this.Id + "=>Expecte value Not:" + base.GetParameter("TagName") + "   Actual value:" + actualValue, ELogType.Info);
                }
                //add by zhuqianqian ScreenShot end
            }
            catch (Exception ex)
            {
                //add by zhuqianqian ScreenShot start
                CommandFailScreenShot(container);
                throw ex;
                //add by zhuqianqian ScreenShot end
            }
        }
    }
}
