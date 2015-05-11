using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Logic.Commands.UI;

namespace Logic.Commands
{
    public class VerifyNotChecked:Command
    {
        public VerifyNotChecked(String id, String description, KeyValuePair<String, Tuple<Type, String>> output, Dictionary<String, Tuple<Type, String>> parameters)
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
                IWebElement element = wait.Until(ExpectedConditions.ElementIsVisible(by));

                IWebElement checkbox = element.FindElement(by);

                if (!checkbox.Selected)
                {
                    /// TODO: Pass
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
                    Logging.SaveLog("CommandId:" + this.Id + "=>Expecte value:NotChecked" + "   Actual value:Checked", ELogType.Info);
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
