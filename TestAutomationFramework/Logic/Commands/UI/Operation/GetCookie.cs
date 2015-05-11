using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Logic.Commands.UI.Operation
{
    public class GetCookie:Command
    {
        public GetCookie(String id, String description, KeyValuePair<String, Tuple<Type, String>> output, Dictionary<String, Tuple<Type, String>> parameters)
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


               Cookie cookie = container.Driver.Manage().Cookies.GetCookieNamed(base.GetParameter("Target"));
               
               if (cookie == null)
               {
                   throw (new Exception("Cookie:" + base.GetParameter("Target") + " not found!"));
               }

               //if (string.IsNullOrEmpty(this.Output.Key) == false)
               //{
               //    this.Output = new KeyValuePair<String, Tuple<Type, String>>(this.Output.Key, new Tuple<Type, String>(typeof(String), cookie.Value));
               //}

               this.PassTest = true;

               //* add for output and IsExpectedFail start
               this.Output = base.GetOutPut(this.Output.Key, this.Output, this.PassTest, true, cookie.Value);
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
