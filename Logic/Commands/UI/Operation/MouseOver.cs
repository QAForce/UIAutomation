using Logic.Commands.UI;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Logic.Commands
{
     [Serializable]
    public class MouseOver : Command
    {
         public MouseOver(String id, String description, KeyValuePair<String, Tuple<Type, String>> output, Dictionary<String, Tuple<Type, String>> parameters)
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

                 // this makes sure the element is visible before you try to do anything
                 // for slow loading pages
                 WebDriverWait wait = new WebDriverWait(container.Driver, TimeSpan.FromSeconds(container._commandTimeout));
                 var element = wait.Until(ExpectedConditions.ElementIsVisible(by));
                 
                 Actions action = new Actions(container.Driver);
                 action.MoveToElement(element);
                 Thread.Sleep(10000);
                 action.Perform();
                 
                 
                 /// TODO: Pass
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
