using Microsoft.Practices.EnterpriseLibrary.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logic.Commands
{
    public class VirtualCommand : Command
    {
        public List<Command> SubCommands { get; private set; }

        public VirtualCommand(String id, String description
            , KeyValuePair<String, Tuple<Type, String>> output
            , Dictionary<String, Tuple<Type, String>> parameters
            , List<Command> subCommands)
            : base(id, description, output, parameters)
        {
            this.SubCommands = subCommands;
            this.SubCommands.Select(p => p.Output)
                .Where(p => String.IsNullOrEmpty(p.Key) == false && this.Parameters.ContainsKey(p.Key) == false)
                .ToList()
                .ForEach(p =>
                {
                    if (this.Parameters.ContainsKey(p.Key) == false)
                    {
                        this.Parameters.Add(p.Key, new Tuple<Type, String>(p.Value.Item1, p.Value.Item2));
                    }
                });
        }

        public override void Run(TestContainer container)
        {
            ConsoleReportPrint.VirtualCommandPrint(this.Id, this.Description);
            //modify by zhuqianqian log4net start
            //Logger.Write(@"\t Command:" + this.Id + "==>" + this.Description, "Info");
            Logging.SaveLog(@"\t Command:" + this.Id + "==>" + this.Description, ELogType.Info);
            //modify by zhuqianqian log4net end

            //* add for IsExecuteCommand start
            if (!base.IsCommandContinue(container))
            {
                this.PassTest = true;
                this.SkipTest = true;
                return;
            }
            //* add for IsExecuteCommand end

            foreach (var cmd in this.SubCommands)
           {
                try
                {
                    cmd.ApplyParameters(this.Parameters);
                    cmd.ApplyParameters(container.GetGlobalParameters());
                    cmd.Run(container);



                    if (String.IsNullOrEmpty(cmd.Output.Key) == false)
                    {
                        if (String.IsNullOrEmpty(this.Output.Key) == false && this.Output.Key.ToLower() == cmd.Output.Key.ToLower())
                            this.Output = new KeyValuePair<String, Tuple<Type, String>>(this.Output.Key, cmd.Output.Value);

                        //If command had output, add or update value to Test Case parameters for next command use
                        if (this.Parameters.ContainsKey(cmd.Output.Key))
                            this.Parameters[cmd.Output.Key] = cmd.Output.Value;
                    }
                    ConsoleReportPrint.VirtualCommandDetailPrint(cmd.Id, cmd.Description,cmd.SkipTest, cmd.PassTest);
                    //modify by zhuqianqian log4net start
                    //Logger.Write(@"\t\t Command:" + cmd.Id.PadRight(34, '.') + "==>" + this.Description + (cmd.PassTest ? "Pass" : "Fail"), "Info");
                    Logging.SaveLog(@"\t\t Command:" + cmd.Id.PadRight(34, '.') + "==>" + this.Description + (cmd.SkipTest ? "Skip" : (cmd.PassTest ? "Pass" : "Fail")), ELogType.Info);
                    //modify by zhuqianqian log4net end
                }
                catch (Exception ex)
                {
                    Logging.SaveLog(ex, ELogType.Error);
                    Console.WriteLine("ERROR Occure, Interrupt this test case");
                    throw (new Exception(ex.Message));
                }
            }

            // set this virtual command pass or fail
            if (SubCommands.Where(c => c.PassTest.Equals(false)).Count() == 0)
            {
                this.PassTest = true;
            }

            //* add for output and IsExpectedFail start
            this.PassTest = GetTestPassExpected(this.PassTest);
            //* add for output and IsExpectedFail end
        }
    }
}
