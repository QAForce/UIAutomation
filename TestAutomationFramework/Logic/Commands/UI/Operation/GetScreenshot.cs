using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Commands
{
    public class GetScreenshot:Command
    {
        public GetScreenshot(String id, String description, KeyValuePair<String, Tuple<Type, String>> output, Dictionary<String, Tuple<Type, String>> parameters)
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

                Screenshot screenShot = ((ITakesScreenshot)container.Driver).GetScreenshot();
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd-hhmm-ss");
                string folderPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, base.GetParameter("Path")));
                
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName = "";
                //[20150312][Tom] Add fixed file name
                if (!base.GetParameter("FileName").Trim().Equals(""))
                {
                    fileName = folderPath + base.GetParameter("FileName") + ".png"; ;
                }
                else if (!base.GetParameter("PrefixFileName").Trim().Equals(""))
                {
                    fileName = folderPath + base.GetParameter("PrefixFileName") + timestamp + ".png";
                }
                else
                {
                    fileName = folderPath + timestamp + ".png";
                }
                
                //[20150312][Tom] Remove
                //fileName = folderPath + base.GetParameter("PrefixFileName") + timestamp + ".jpeg";

                screenShot.SaveAsFile(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);

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
