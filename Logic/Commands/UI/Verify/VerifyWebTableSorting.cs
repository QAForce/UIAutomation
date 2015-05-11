using Logic.Commands.UI;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Commands
{
    public class VerifyWebTableSorting:Command
    {
        public VerifyWebTableSorting(String id, String description, KeyValuePair<String, Tuple<Type, String>> output, Dictionary<String, Tuple<Type, String>> parameters)
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

                DataTable dtWeb = new DataTable();

                WebDriverWait wait = new WebDriverWait(container.Driver, TimeSpan.FromSeconds(container._commandTimeout));
                IWebElement elem = wait.Until(ExpectedConditions.ElementIsVisible(by));

                List<IWebElement> lstHeader = elem.FindElements(By.TagName("th")).ToList();
                
                //Treat header like a row
                for(int i=0;i<lstHeader.Count();i++)
                {
                    dtWeb.Columns.Add("Column" + i.ToString());
                }

                DataRow drHeader = dtWeb.NewRow();
                for (int i = 0; i < lstHeader.Count(); i++)
                {
                    drHeader[i] = lstHeader[i].Text;
                }
                dtWeb.Rows.Add(drHeader);

                List<IWebElement> lstRows = elem.FindElements(By.TagName("tr")).ToList();
                for (int i = 1; i < lstRows.Count(); i++) //ignore header, start at row 1
                {
                    DataRow drWeb = dtWeb.NewRow();
                    List<IWebElement> lstCells = lstRows[i].FindElements(By.TagName("td")).ToList();
                    for (int j = 0; j < lstCells.Count; j++)
                    {
                        if (lstCells[j].Text.Trim().Equals(""))
                        {
                            /// TODO
                            try
                            {
                                drWeb[j] = lstCells[j].FindElement(By.TagName("i")).GetAttribute("class");
                            }
                            catch
                            {
                                drWeb[j] = lstCells[j].Text;
                            }
                        }
                        else
                        {
                            drWeb[j] = lstCells[j].Text;
                        }
                    }
                    dtWeb.Rows.Add(drWeb);
                }

                List<string> lstSource=new List<string>();
                List<string> lstTarget=new List<string>();

                for (int i = 1; i < dtWeb.Rows.Count; i++)
                {
                    lstSource.Add(dtWeb.Rows[i][base.GetParameter("ColumnName")].ToString());
                    lstTarget.Add(dtWeb.Rows[i][base.GetParameter("ColumnName")].ToString());
                }

                string strActual = "";
                if (base.GetParameter("SortBy").ToUpper().Equals("ASC"))
                {
                    lstTarget = lstTarget.OrderBy(p => p).ToList();
                    strActual = "DESC";
                }
                else
                {
                    lstTarget = lstTarget.OrderByDescending(p => p).ToList();
                    strActual = "ASC";
                }

                if (lstTarget.SequenceEqual(lstSource))
                {
                    this.PassTest = true;
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
                    Logging.SaveLog("CommandId:" + this.Id + "=>Expecte value:" + base.GetParameter("SortBy").ToUpper() + "   Actual value:" + strActual, ELogType.Info);
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
