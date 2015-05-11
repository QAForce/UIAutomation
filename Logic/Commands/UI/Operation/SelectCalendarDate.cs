using Logic.Commands.UI;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Logic.Commands
{
    public class SelectCalendarDate:Command
    {
        public SelectCalendarDate(String id, String description, KeyValuePair<String, Tuple<Type, String>> output, Dictionary<String, Tuple<Type, String>> parameters)
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

                //Calendar locator
                string invokeMethodName;
                string invokeParameter;

                UICommandHelper cmdHelp = new UICommandHelper();
                cmdHelp.ExtractMethodName(base.GetParameter("Target"), out invokeMethodName, out invokeParameter);

                By by = typeof(By).GetMethod(invokeMethodName).Invoke(null, new[] { invokeParameter }) as By;

                WebDriverWait wait = new WebDriverWait(container.Driver, TimeSpan.FromSeconds(container._commandTimeout));
                IWebElement elem = wait.Until(ExpectedConditions.ElementIsVisible(by));


                //Next Month Locator
                string invokeNextMonthMethodName;
                string invokeNextMonthParameter;
                cmdHelp.ExtractMethodName(base.GetParameter("NextMonthLocator"), out invokeNextMonthMethodName, out invokeNextMonthParameter);
                By byOfNextMonth = typeof(By).GetMethod(invokeNextMonthMethodName).Invoke(null, new[] { invokeNextMonthParameter }) as By;
                IWebElement elemNextMonth = wait.Until(ExpectedConditions.ElementIsVisible(byOfNextMonth));


                //Previous Month Locator
                string invokePreMonthMethodName;
                string invokePreMonthParameter;
                cmdHelp.ExtractMethodName(base.GetParameter("PreMonthLocator"), out invokePreMonthMethodName, out invokePreMonthParameter);
                By byOfPreMonth = typeof(By).GetMethod(invokePreMonthMethodName).Invoke(null, new[] { invokePreMonthParameter }) as By;
                IWebElement elemPreMonth = wait.Until(ExpectedConditions.ElementIsVisible(byOfPreMonth));


                DateTime dtSelectDate = DateTime.ParseExact(base.GetParameter("Date"), "yyyy-MM-dd", CultureInfo.CurrentUICulture);
                int yearDiff = dtSelectDate.Year - DateTime.UtcNow.Year;
                int monthDiff = dtSelectDate.Month - DateTime.UtcNow.Month;
                int dayDiff = dtSelectDate.Day - DateTime.UtcNow.Day;
                int selectDay = dtSelectDate.Day;

                bool isFuture = true;
                int clickMonthTimes = 0;

                int totalDiffMonth = yearDiff * 12 + monthDiff;
                if (totalDiffMonth > 0)
                {
                    clickMonthTimes = totalDiffMonth;
                }
                else if (totalDiffMonth == 0)
                {
                    clickMonthTimes = 0;
                    if (dayDiff <= 0)
                    {
                        isFuture = false;
                    }
                }
                else
                {
                    clickMonthTimes = -(totalDiffMonth);
                    isFuture = false;
                }

                if (isFuture)
                {
                    //IWebElement elemNextMonth = elem.FindElement(By.XPath("thead/tr[1]/th[3]"));
                    for (int i = 0; i < clickMonthTimes; i++)
                    {
                        elemNextMonth.Click();
                    }
                }
                else
                {
                    //IWebElement elemPreviousMonth = elem.FindElement(By.XPath("thead/tr[1]/th[1]"));
                    for (int i = 0; i < clickMonthTimes; i++)
                    {
                        elemPreMonth.Click();
                    }
                }

                //Select Day
                bool isFindFirstDay=false;
                bool isFindoutAndClick = false;
                List<IWebElement> lstRows = elem.FindElement(By.TagName("tbody")).FindElements(By.TagName("tr")).ToList();
                for (int i = 0; i < lstRows.Count(); i++)
                {
                    List<IWebElement> lstCells = lstRows[i].FindElements(By.TagName("td")).ToList();
                    for (int j = 0; j < lstCells.Count; j++)
                    {
                        if (lstCells[j].Text.Trim().Equals("1"))
                        {
                            isFindFirstDay=true;
                        }
                        if (isFindFirstDay&&lstCells[j].Text.Trim().Equals(selectDay.ToString()))
                        {
                                lstCells[j].Click();
                                isFindoutAndClick = true;
                                break;                            
                        }
                    }
                    if (isFindoutAndClick)
                    {
                        break;
                    }
                }

                this.PassTest = true;

                //* add for output and IsExpectedFail start
                this.Output = base.GetOutPut(this.Output.Key, this.Output, this.PassTest);
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
