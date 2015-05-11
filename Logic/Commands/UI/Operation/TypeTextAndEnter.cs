using Logic.Commands.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using System.ComponentModel;
using OpenQA.Selenium.Support.UI;

namespace Logic.Commands
{
    [Serializable]
    public class TypeTextAndEnter : Command
    {

        public TypeTextAndEnter(String id, String description, KeyValuePair<String, Tuple<Type, String>> output, Dictionary<String, Tuple<Type, String>> parameters)
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

                elem.Clear();
                elem.SendKeys(base.GetParameter("Value"));
                elem.SendKeys(OpenQA.Selenium.Keys.Enter);


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
