using Logic.Commands;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

namespace Logic
{
    public class TestCase
    {
        public String Id { get; private set; }
        public String Description { get; private set; }
        public String[] Tag { get; private set; }
        public Boolean PassTest { get; set; }
        public ReadOnlyCollection<Command> Commands { get; private set; }
        private Dictionary<String, Tuple<Type, String>> _parameters = new Dictionary<String, Tuple<Type, String>>();

        public TestCase(String id, String description, String[] tag, List<Command> commands)
        {
            this.Id = id;
            this.Description = description;
            this.Tag = tag;
            this.PassTest = false;
            this.Commands = commands.AsReadOnly();
            this._parameters = new Dictionary<String, Tuple<Type, String>>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in this.Commands.Select(p => p.Output.Key)
                .Distinct()
                .Where(p => String.IsNullOrEmpty(p) == false && this._parameters.ContainsKey(p) == false))
            {
                this._parameters.Add(p, new Tuple<Type, String>(typeof(String), String.Empty));
            }
        }

        public void Run(TestContainer container)
        {
            foreach (var cmd in this.Commands)
            {
                try
                {
                    cmd.ApplyParameters(this._parameters);
                    cmd.ApplyParameters(container.GetGlobalParameters());
                    cmd.Run(container);


                    if (String.IsNullOrEmpty(cmd.Output.Key) == false)
                    {
                        //If command had output, add or update value to Test Case parameters for next command use
                        if (this._parameters.ContainsKey(cmd.Output.Key))
                            this._parameters[cmd.Output.Key] = cmd.Output.Value;
                    }
                    ConsoleReportPrint.CaseCommandPrint(cmd.Id, cmd.Description,cmd.SkipTest, cmd.PassTest);
                    //modify by zhuqianqian log4net start
                    //Logger.Write(@"\t Command:" + cmd.Id + "=>" + cmd.Description.PadRight(42, '.') + (cmd.PassTest ? "Pass" : "Fail"), "Info");
                    Logging.SaveLog(@"\t Command:" + cmd.Id + "=>" + cmd.Description.PadRight(42, '.') + (cmd.SkipTest ? "Skip" : (cmd.PassTest ? "Pass" : "Fail")), ELogType.Info);
                    //modify by zhuqianqian log4net end
                }
                catch (Exception ex)
                {
                    Logging.SaveLog(ex, ELogType.Error);
                    Console.WriteLine("\t Command:" + cmd.Id + "=>" + cmd.Description + " ERROR Occure, Interrupt this test case");
                    throw (new Exception(ex.Message));
                }

            }

            //Set test case pass or fail
            if (this.Commands.Where(c => c.PassTest.Equals(false)).Count() == 0)
            {
                this.PassTest = true;
            }
        }
    }
}
