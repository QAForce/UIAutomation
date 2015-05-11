using Logic.Commands;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using Utilities;

namespace Logic
{
    public class TestContainer
    {
        //[20150323][Tom] Private environment setting
        private bool _openBrowserInTest { get; set; }
        private bool _closeBrowserAfterTestComplete { get; set; }
        private Dictionary<string, Dictionary<string, string>> _browserExectionSetting = new Dictionary<string, Dictionary<string, string>>();

        //private Dictionary<string, Dictionary<string, string>> _databaseSetting = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, List<string>> _testFileImportSetting = new Dictionary<string, List<string>>();
        private Dictionary<string, Dictionary<string, string>> _commandSetting = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<String, Tuple<Type, String>> _globalParameters = new Dictionary<String, Tuple<Type, String>>();        
        //todo[20150323][Tom] Refactor to TestCaseFactory
        private ReadOnlyCollection<TestCase> _testCases;
        
        //public property for command internal access
        public IWebDriver Driver { get; private set; }

        public string _configFilePath { get; private set; }
        public int _commandTimeout { get; private set; }
        public bool _screenshotOnFail { get; private set; }
        public string _filePathofScreenshotOnFail { get; private set; }

        public TestContainer(String configFilePath, Func<XAttribute, Boolean>[] filterTestCase = null)
        {

            //todo[20150323][Tom] Change the environment setting file from ini to XML

            #region old ini file parsing method
            //var iniFile = new IniFile(iniFilePath);
            //this._globalParameters = iniFile.GetSectionValuesAsList("GlobalParameters").ToDictionary(p => "${" + p.Key + "}", p => new Tuple<Type, String>(typeof(String), p.Value), StringComparer.OrdinalIgnoreCase);
            //this._browserSetting = iniFile.GetSectionValuesAsList("Browser").ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase);
            //this._commandTimeout = int.Parse(iniFile.GetSectionValuesAsList("Commands").ToDictionary(c => c.Key, c => c.Value)["Timeout"]);
            //var sqlCmdSetting = iniFile.GetSectionValuesAsList("SQLCommands").Select(p => p.Value).ToList();
            //var virCmdSetting = iniFile.GetSectionValuesAsList("VirtualCommands").Select(p => p.Value).ToList();
            //var testCaseSetting = iniFile.GetSectionValuesAsList("TestCases").Select(p => p.Value).ToList();
            #endregion

            #region New method: Parsing environment and execution setting from Config XML
            this._configFilePath = configFilePath;
            ParsingEnvironmentSettingConfig(configFilePath);
           
            var sqlCmdSetting = _testFileImportSetting["SQLCommands"];
            var virCmdSetting = _testFileImportSetting["VirtualCommands"];
            var testCaseSetting = _testFileImportSetting["TestCases"];
            //*add for RestfulCommands start
            var restfulCmdSetting = _testFileImportSetting["RestfulCommands"];
            //*add for RestfulCommands end
            #endregion

            var cmdFactory = new CommandFactory();
            List<SQLExecute> _sqlCommands = cmdFactory.GetSqlCommand(sqlCmdSetting);
            //*add for RestfulCommands start
            List<RestfulCommand> _restfulCommands = cmdFactory.GetRestfulCommand(restfulCmdSetting);
            //List<RestfulCommand> _restfulCommands = new List<RestfulCommand>();
            //*add for RestfulCommands end
            List<VirtualCommand> _virtualCommands = cmdFactory.GetVirtualCommand(virCmdSetting, _sqlCommands, _restfulCommands);
            Func<XAttribute, Boolean> testCasefilter = (testCaseAttr) => { return true; };
            if (filterTestCase != null)
            {
                testCasefilter = (testCaseAttr) =>
                {
                    bool rtn = true;
                    foreach (var filter in filterTestCase)
                    {
                        rtn = rtn && filter(testCaseAttr);
                    }

                    return rtn;
                };
            }
            this._testCases = cmdFactory.GetTestCase(testCaseSetting, _sqlCommands, _restfulCommands ,_virtualCommands, testCasefilter).AsReadOnly();
        }

        public void SetBrowserDriver(KeyValuePair<string,Dictionary<string,string>> browserSetting)
        {
            //if (this._browserSetting["Active"].Equals("On") == false)
            //    return;

            try
            {
                switch (browserSetting.Key)
                {
                    case "IE":
                        InternetExplorerOptions options = new InternetExplorerOptions();
                        options.IntroduceInstabilityByIgnoringProtectedModeSettings = true;
                        this.Driver = new InternetExplorerDriver(options);
                        break;
                    case "Firefox":
                        FirefoxProfile profile = null;
                        if (!string.IsNullOrEmpty(browserSetting.Value["Profile"]))
                        {
                            profile = new FirefoxProfile(browserSetting.Value["Profile"]);
                        }
                        else
                        {
                            profile = new FirefoxProfile();
                        }
                        this.Driver = new FirefoxDriver(profile);                     
                        this.Driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(8));
                        break;
                    case "Chrome":
                        ChromeOptions chromeOptions = new ChromeOptions();
                        if (!string.IsNullOrEmpty(browserSetting.Value["Profile"]))
                        {
                            chromeOptions.AddArguments(@"--user-data-dir=" + browserSetting.Value["Profile"]);
                        }                 
                        this.Driver = new ChromeDriver(chromeOptions);
                        break;
                    default:
                        throw new NotSupportedException(String.Format(@"The argument [Browser].Core({0}) not supported!", browserSetting.Key));
                }
            }
            catch (Exception ex)
            {
                Logging.SaveLog(ex, ELogType.Error);
                Console.WriteLine("Active browser fail!");
                throw;
            }
        }

        public void StartTest()
        {
            //*modify for OpenBrowserInTest 
            if(_browserExectionSetting.Count > 0)
            {
                foreach (var browserSetting in _browserExectionSetting)
                {
                    if (this._openBrowserInTest)
                    {
                        //*modify for OpenBrowserInTest 
                        this.SetBrowserDriver(browserSetting);
                    }
                    foreach (TestCase tCase in this._testCases)
                    {
                        try
                        {
                            //modify by zhuqianqian log4net start
                            //Logger.Write(@"Run:" + tCase.Id + ":" + tCase.Description, "Info");
                            Logging.SaveLog(@"Run:" + tCase.Id + ":" + tCase.Description, ELogType.Info);
                            //modify by zhuqianqian log4net end
                            ConsoleReportPrint.TestCaseStartPrint(tCase.Id, tCase.Description);
                            tCase.Run(this);
                            //modify by zhuqianqian log4net start
                            //Logger.Write("Test Result:" + tCase.Id.PadRight(54, '.') + (tCase.PassTest ? "Pass" : "Fail"), "Info");
                            Logging.SaveLog("Test Result:" + tCase.Id.PadRight(54, '.') + (tCase.PassTest ? "Pass" : "Fail"), ELogType.Info);
                            //modify by zhuqianqian log4net end
                            ConsoleReportPrint.TestCaseCompletePrint(tCase.Id, tCase.PassTest);
                        }
                        catch (Exception ex)
                        {
                            /// Log without throw exception
                            //Logger.Write(@"ERROR Occure, Interrupt this test case", "Info");
                            //Logger.Write("Test Case:" + tCase.Id + " Exception:" + ex.Message, "Exception");
                            Logging.SaveLog(@"ERROR Occure, Interrupt this test case", ELogType.Error);
                            Logging.SaveLog("Test Case:" + tCase.Id + " Exception:" + ex.Message, ELogType.Error);
                            Console.WriteLine("Exception:" + tCase.Id + " Message:" + ex.Message);
                        }
                    }
                    this.PrintTestResult();
                    if (this.Driver != null && this._openBrowserInTest && _closeBrowserAfterTestComplete)
                    {
                        //*modify for closeBrowserAfterTestComplete
                        this.Driver.Close();
                    }
                }
            }
            else if (!this._openBrowserInTest)
            {
                foreach (TestCase tCase in this._testCases)
                {
                    try
                    {
                        Logging.SaveLog(@"Run:" + tCase.Id + ":" + tCase.Description, ELogType.Info);
                        ConsoleReportPrint.TestCaseStartPrint(tCase.Id, tCase.Description);
                        tCase.Run(this);
                        Logging.SaveLog("Test Result:" + tCase.Id.PadRight(54, '.') + (tCase.PassTest ? "Pass" : "Fail"), ELogType.Info);
                        ConsoleReportPrint.TestCaseCompletePrint(tCase.Id, tCase.PassTest);
                    }
                    catch (Exception ex)
                    {
                        Logging.SaveLog(@"ERROR Occure, Interrupt this test case", ELogType.Error);
                        Logging.SaveLog("Test Case:" + tCase.Id + " Exception:" + ex.Message, ELogType.Error);
                        Console.WriteLine("Exception:" + tCase.Id + " Message:" + ex.Message);
                    }
                }
                this.PrintTestResult();
            }
        }

        private void PrintTestResult()
        {
            Console.WriteLine("\n\n====================== Test Result ======================");
            foreach (var tCase in this._testCases)
            {
                Console.WriteLine(tCase.Id.PadRight(53, '.') + (tCase.PassTest ? "Pass" : "Fail"));
            }
        }

        /// <summary>
        /// Parsing Environment Setting From XML
        /// </summary>
        /// <param name="xmlFilePath"></param>
        private void ParsingEnvironmentSettingConfig(String configFilePath)
        {
            //[20150323][Tom] Notice: XML Parsing is case sensitive
            var doc = XDocument.Load(configFilePath);

            #region Parsing BrowserSection
            var browserActivationConfiguration = doc.Descendants("configuration").Descendants("BrowserSection").Descendants("ActivationConfiguration");
            _openBrowserInTest = bool.Parse(browserActivationConfiguration.Descendants("OpenBrowserInTest").Attributes("value").FirstOrDefault().Value);
            _closeBrowserAfterTestComplete = bool.Parse(browserActivationConfiguration.Descendants("CloseBrowserAfterTestComplete").Attributes("value").FirstOrDefault().Value);
            
            //Parsing Browser Execution
            var BrowserExecutionConfiguration = doc.Descendants("configuration").Descendants("BrowserSection").Descendants("BrowserExecution");
            foreach (var p in BrowserExecutionConfiguration.Descendants("Browser"))
            {
                _browserExectionSetting.Add(p.Attributes("Type").FirstOrDefault().Value,
                                            p.Attributes().Where(attr => attr.Name.LocalName.ToLower() != "type").ToDictionary(t => t.Name.ToString(), t => t.Value.ToString()));                                           
            }
            #endregion

            #region Database Section
            //[20150328][Tom] It is not required anymore
            //var databaseConnection = doc.Descendants("EnvironmentSetting").Descendants("DatabaseSection").Descendants("connectionStrings");
            //foreach (var p in databaseConnection.Descendants("add"))
            //{
            //    _databaseSetting.Add(p.Attributes("name").ToString(), null);
            //    _databaseSetting["name"] = (p.Attributes().Where(attr => attr.Name.LocalName.ToLower() != "name").ToDictionary(t => t.Name.ToString(), t => t.Value.ToString())); 
            //}
            #endregion

            #region Parsing Test File Section
            var fileImportSection = doc.Descendants("configuration").Descendants("TestFileImportSection");
            _testFileImportSetting.Add("TestCases", new List<string>());
            _testFileImportSetting.Add("VirtualCommands", new List<string>());
            _testFileImportSetting.Add("SQLCommands", new List<string>());
            _testFileImportSetting.Add("RestfulCommands", new List<string>());       
            _testFileImportSetting.Add("APICommands", new List<string>());
            //TestCase
            foreach (var p in fileImportSection.Descendants("TestCases").Descendants("FilePath"))
            {
                if (!String.IsNullOrEmpty(p.Attribute("value").Value.Trim()))
                {
                    _testFileImportSetting["TestCases"].Add(p.Attribute("value").Value);
                }
            }

            //VirtualCommands
            foreach (var p in fileImportSection.Descendants("VirtualCommands").Descendants("FilePath"))
            {
                if (!String.IsNullOrEmpty(p.Attribute("value").Value.Trim()))
                {
                    _testFileImportSetting["VirtualCommands"].Add(p.Attribute("value").Value);
                }
            }

            //SQLCommands
            foreach (var p in fileImportSection.Descendants("SQLCommands").Descendants("FilePath"))
            {
                if (!String.IsNullOrEmpty(p.Attribute("value").Value.Trim()))
                {
                    _testFileImportSetting["SQLCommands"].Add(p.Attribute("value").Value);
                }
            }

            //RestfulCommands
            foreach (var p in fileImportSection.Descendants("RestfulCommands").Descendants("FilePath"))
            {
                if (!String.IsNullOrEmpty(p.Attribute("value").Value.Trim()))
                {
                    _testFileImportSetting["RestfulCommands"].Add(p.Attribute("value").Value);
                }
            }

            //todo[20150323][Tom] method required
            //APICommands
            #endregion

            #region Parsing Command Setting
            foreach (var p in doc.Descendants("configuration").Descendants("CommandSection").Elements())
            {
                _commandSetting.Add(p.Name.ToString(), p.Attributes().ToDictionary(t => t.Name.ToString(), t => t.Value.ToString()));
            }
            _commandTimeout = int.Parse(_commandSetting["CommandTimeout"]["value"]);
            _screenshotOnFail = bool.Parse(_commandSetting["ScreenshotWhenUICommandFail"]["value"]);
            _filePathofScreenshotOnFail = _commandSetting["ScreenshotWhenUICommandFail"]["FilePath"];
            #endregion

            #region Parsing Execution Plan



            #endregion

            #region Parsing Global Parameters
            //Static Parameters
            _globalParameters = doc.Descendants("configuration")
                .Descendants("GlobalParametersSection")
                .Descendants("StaticParameters")
                .Descendants("add")
                .ToDictionary(p => "${" + p.Attribute("key").Value + "}", p => new Tuple<Type, String>(typeof(String), p.Attribute("value").Value), StringComparer.OrdinalIgnoreCase);
            
            //foreach (var p in staticParameters.Descendants("add"))
            //{
            //    _globalParameters.Add("${" + p.Attribute("key").Value + "}", new Tuple<Type, String>(typeof(String), p.Attribute("value").ToString()));
            //}

            //Dynamic Parameter
            //todo[20150323][Tom] we need provide basic dynamic function

            #endregion
        }

        #region GlobalParameters
        public Dictionary<String, Tuple<Type, String>> GetGlobalParameters()
        {
            return this._globalParameters;
        }

        public void GlobalParametersAddValue(String key, Tuple<Type, String> value)
        {
            if (this._globalParameters.ContainsKey(key))
                throw new ArgumentException(@"An element with the same key already exists in GlobalParameters.");

            this._globalParameters.Add(key, value);
        }
        #endregion
    }
}
