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
    public class VerifyWebTableColumnText:Command
    {
        public VerifyWebTableColumnText(String id, String description, KeyValuePair<String, Tuple<Type, String>> output, Dictionary<String, Tuple<Type, String>> parameters)
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

                List<IWebElement> lstRows = elem.FindElements(By.TagName("tr")).ToList();

                string actualValue = "";
                if (lstRows.Count() == 1)
                {
                    List<IWebElement> lstCells = lstRows[0].FindElements(By.TagName("td")).ToList();
                    actualValue = actualValue + lstCells[0].Text + ",";
                }
                for (int i = 1; i < lstRows.Count(); i++) //ignore header, start at row 1
                {
                    List<IWebElement> lstCells = lstRows[i].FindElements(By.TagName("td")).ToList();
                    try
                    {
                        if (int.Parse(base.GetParameter("ColumnName").ToUpper().Replace("COLUMN", "")) >= lstCells.Count)
                        {
                            this.PassTest = false;
                        }
                        else if(int.Parse(base.GetParameter("ColumnName").ToUpper().Replace("COLUMN", ""))<0)
                        {
                            this.PassTest = false;
                        }
                    }
                    catch
                    {
                        this.PassTest = false;
                    }

                    int ColumnNumber=int.Parse(base.GetParameter("ColumnName").ToUpper().Replace("COLUMN", ""));
                    if (lstCells[ColumnNumber].Text.ToString().ToUpper().Equals(base.GetParameter("Value").ToUpper()))
                    {
                        this.PassTest = true;
                    }
                    else
                    {
                        actualValue = actualValue + lstCells[ColumnNumber].Text + ",";
                    }
                }
                if (actualValue.Length > 0)
                {
                    actualValue = actualValue.Substring(0, actualValue.Length - 1);
                }

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
                    Logging.SaveLog("CommandId:" + this.Id + "=>Expecte value:" + base.GetParameter("Value") + "   Actual value:" + actualValue, ELogType.Info);
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
