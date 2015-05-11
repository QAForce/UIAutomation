using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Commands.UI;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;

namespace Logic.Commands
{
    public class GetWebTable : Command
    {
        public GetWebTable(String id, String description, KeyValuePair<String, Tuple<Type, String>> output, Dictionary<String, Tuple<Type, String>> parameters)
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

                string jsonString = "";

                //if (String.IsNullOrEmpty(this.Output.Key) == false)
                //{
                //    var json = dtWeb.SerializeToJSon();
                //    this.Output = new KeyValuePair<String, Tuple<Type, String>>(this.Output.Key, new Tuple<Type, String>(typeof(List<Dictionary<String, String>>), json));
                //}

                this.PassTest = true;

                if (String.IsNullOrEmpty(this.Output.Key) == false)
                {
                    jsonString = dtWeb.SerializeToJSon();
                }

                //* add for output and IsExpectedFail start
                this.Output = base.GetOutPut(this.Output.Key, this.Output, this.PassTest, true, jsonString);
                //* add for output and IsExpectedFail end

                //* add for output and IsExpectedFail start
                this.PassTest = GetTestPassExpected(this.PassTest);
                //* add for output and IsExpectedFail end

                //*add for ScreenShot start
                if (!this.PassTest)
                {
                    base.CommandFailScreenShot(container);
                }
                //*add for ScreenShot end
            }
            catch (Exception ex)
            {
                //*add for ScreenShot start
                base.CommandFailScreenShot(container);
                //*add for ScreenShot end
                throw ex;

            }
        }
    }
}
