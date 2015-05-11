using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace Logic.Commands
{
    public class OpenURL : Command
    {
        public OpenURL(String id, String description, KeyValuePair<String, Tuple<Type, String>> output, Dictionary<String, Tuple<Type, String>> parameters)
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

                container.Driver.Navigate().GoToUrl(base.GetParameter("Target"));

                //* add for output and IsExpectedFail start
                this.Output = base.GetOutPut(this.Output.Key, this.Output, true);
                //* add for output and IsExpectedFail end

                //* add for output and IsExpectedFail start
                this.PassTest = GetTestPassExpected(true);
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
