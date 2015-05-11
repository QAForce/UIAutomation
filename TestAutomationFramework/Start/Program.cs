using CommandLine;
using CommandLine.Text;
using Logic;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using System;
using System.Linq;
using System.Configuration;
using System.Collections.Generic;
using Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using System.IO;
using Logic.Commands;
using System.Reflection;
using System.Collections.ObjectModel;

/*
* -f LMP50_QA.ini -d LMP05*,LMP06* -t E2E,BVT
* -f LMP50_QA.INI -d LMP005 -t E2E,BVT
* -f LMP50_QA.ini
*/
namespace Start
{
    // Define a class to receive parsed values
    public class Options
    {
        [Option('f', "Environment setting file", Required = true,
          HelpText = @"config file name, blahblahblahblahblah.")]
        public string ConfigFileName { get; set; }

        [OptionList('d', "Test case id search pattern string", Required = false, Separator = ',',
          HelpText = @"Search by id to find matched test cases, format: LMP010, LMP05*, separated by a comma.")]
        public IList<string> IdSearchPattern { get; set; }

        [OptionList('t', "Test case tag filter pattern string", Required = false, Separator = ',',
          HelpText = @"Search by tag attribute to find matched test cases, separated by a comma.")]
        public IList<string> TagSearchPattern { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                //modify by zhuqianqian log4net start
                //IConfigurationSource configurationSource = ConfigurationSourceFactory.Create();
                //LogWriterFactory logWriterFactory = new LogWriterFactory(configurationSource);
                //Logger.SetLogWriter(logWriterFactory.Create());
                //Logger.Write("Console started!", "Info");
                //modify by zhuqianqian log4net end

                var options = new Options();
                if (CommandLine.Parser.Default.ParseArguments(args, options) == false)
                    return;

                //modify by zhuqianqian log4net start
                var iniFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".\Data\Environment\" + options.ConfigFileName);
                iniFilePath = System.IO.Path.GetFullPath(iniFilePath);
                if (File.Exists(iniFilePath) == false)
                {
                    throw new FileNotFoundException(String.Format("Config file({0}) is not found in the folder of \\Data\\Environment!", iniFilePath));
                }

                Logging.LoadLog(iniFilePath);
                Logging.SaveLog("Test started!", ELogType.Info);
                Logging.SaveLog(String.Format("ConfigFileName:{0}", "" + options.ConfigFileName),ELogType.Info);
                Logging.SaveLog(String.Format("ConfigFilePath:{0}", "" + iniFilePath), ELogType.Info);
                Logging.SaveLog(String.Format("IdSearchPattern:{0}", options.IdSearchPattern == null ? String.Empty : String.Join(",", options.IdSearchPattern)),ELogType.Info);
                Logging.SaveLog(String.Format("TagSearchPattern:{0}", options.TagSearchPattern == null ? String.Empty : String.Join(",", options.TagSearchPattern)), ELogType.Info);
                //Logger.Write(String.Format("ConfigFileName:{0}", "" + options.ConfigFileName), "Info");
                //Logger.Write(String.Format("IdSearchPattern:{0}", options.IdSearchPattern == null ? String.Empty : String.Join(",", options.IdSearchPattern)), "Info");
                //Logger.Write(String.Format("TagSearchPattern:{0}", options.TagSearchPattern == null ? String.Empty : String.Join(",", options.TagSearchPattern)), "Info");
                //modify by zhuqianqian log4net end



                TestContainer tmp = new TestContainer(iniFilePath
                    , new[] { Logic.Helper.CreateIdFilterPredicate(options.IdSearchPattern), Logic.Helper.CreateTagFilterPredicate(options.TagSearchPattern) });
                tmp.StartTest();

                //modify by zhuqianqian log4net start
                Logging.SaveLog("Test executed finish!", ELogType.Info);
                //Logger.Write("Console finished!", "Info");
                //modify by zhuqianqian log4net end

                Console.WriteLine("Press any key to continue....");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                var exceptionMessage = String.Format(@"{0}", ex.InnerException == null ? ex.Message : ex.InnerException.Message);
                //modify by zhuqianqian log4net start
                //todo[20150328][Tom] Please help put the full ex object content to the log
                //Logging.SaveLog(@"An unexpected error occurred.", ELogType.Info);
                //Logging.SaveLog(exceptionMessage,ELogType.Error);
                Logging.SaveLog(ex, ELogType.Error);
                //Logger.Write(@"An unexpected error occurred.", "Info");
                //Logger.Write(exceptionMessage, "Exeption");
                //modify by zhuqianqian log4net end
            }
        }
    }
}
