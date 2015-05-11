using Microsoft.Practices.EnterpriseLibrary.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;


namespace Logic.Commands
{
    public class CommandFactory
    {
        private String[] _preDefineAttr = { "type", "id", "database", "output", "description" };
        private ReadOnlyCollection<Type> _commandTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Command)) && type.IsAbstract == false)
            .OrderBy(type => type.Name)
            .ToList()
            .AsReadOnly();

        #region restfulCommand
        public List<RestfulCommand> GetRestfulCommand(List<String> setting)
        {
            var sqlPath = setting.Select(p => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p))).ToList();
            var invalidPath = sqlPath.Where(p => Directory.Exists(p) == false && (File.Exists(p) == false || (File.Exists(p) && Path.GetExtension(p).ToLower() != ".xml"))).ToArray();
            if (invalidPath.Any())
            {
                var exceptionMessage = String.Format(@"Restful command path({0}) not found or file without the XML file extension!", String.Join(",", invalidPath));
                throw new Exception(exceptionMessage);
            }

            var rtn = new List<RestfulCommand>();
            var arrXmlFilePath = sqlPath.Where(p => Directory.Exists(p))
                            .SelectMany(p => Directory.GetFiles(p, "*.xml", SearchOption.AllDirectories))
                            .Union(sqlPath.Where(p => File.Exists(p) && Path.GetExtension(p).ToLower() == ".xml"))
                            .ToArray();
            foreach (var xmlFilePath in arrXmlFilePath)
            {
                var tmp = this.XmlToRestfulCommand(xmlFilePath);
                var duplicateList = rtn.Select(p => p.Id).Intersect(tmp.Select(p => p.Id));
                if (duplicateList.Any())
                {
                    var exceptionMessage = String.Format(@"SQL command id({0}) is duplicated!", String.Join(",", duplicateList));
                    throw new Exception(exceptionMessage);
                }

                rtn.AddRange(tmp);
                Logging.SaveLog(String.Format("Restful commands file({0}) contains {1} statements!", xmlFilePath, tmp.Count()), ELogType.Info);
            }
            return rtn;
        }

        private List<RestfulCommand> XmlToRestfulCommand(String xmlFilePath)
        {
            try
            {
                var cmdType = typeof(RestfulCommand);
                var rtn = new List<RestfulCommand>();
                var doc = XDocument.Load(xmlFilePath);
                //var requiredAttributes = new[] { "id", "method", "host", "tenantid" };
                var requiredAttributes = new[] { "id", "method", "host"};
                foreach (var p in doc.Descendants("Restfuls").Descendants("Restful"))
                {
                    var missingAttributes = requiredAttributes.Where(x => p.Attributes().Any(tc => tc.Name.LocalName.ToLower() == x) == false);
                    if (missingAttributes.Any())
                    {
                        var exceptionMessage = String.Format(@"Restfuls element is missing required attributes({0})!", String.Join(",", missingAttributes));
                        throw new Exception(exceptionMessage);
                    }

                    var idAttr = p.Attributes().First(tc => tc.Name.LocalName.ToLower() == "id");
                    var descriptionAttr = p.Attributes().FirstOrDefault(tc => tc.Name.LocalName.ToLower() == "description");
                    //var methodAttr = p.Attributes().First(tc => tc.Name.LocalName.ToLower() == "method");
                    //var hostAttr = p.Attributes().First(tc => tc.Name.LocalName.ToLower() == "host");
                    //var tenantidAttr = p.Attributes().First(tc => tc.Name.LocalName.ToLower() == "tenantid");
                    var outputAttr = p.Attributes().FirstOrDefault(tc => tc.Name.LocalName.ToLower() == "output");
                    var cmdParameters = p.Attributes().Any(x => this._preDefineAttr.Contains(x.Name.LocalName.ToLower()) == false) ?
                            p.Attributes().Where(x => this._preDefineAttr.Contains(x.Name.LocalName.ToLower()) == false)
                            .OrderBy(att => att.Name.ToString())
                            .ToDictionary(att => "${" + att.Name.ToString() + "}"
                                , att => new Tuple<Type, String>(typeof(String), ("" + att.Value).Trim()), StringComparer.OrdinalIgnoreCase) :
                            new Dictionary<String, Tuple<Type, String>>(StringComparer.OrdinalIgnoreCase);
                    cmdParameters.Add("Header", new Tuple<Type, String>(typeof(String), p.Descendants("Header").First().Value));
                    cmdParameters.Add("Body", new Tuple<Type, String>(typeof(String), p.Descendants("Body").First().Value));
                    if (outputAttr != null && outputAttr.Value.Trim().StartsWith("${") && Regex.IsMatch(outputAttr.Value.Trim(), Helper.CommandOutputTokenPattern) == false)
                    {
                        var exceptionMessage = String.Format(@"Restful({0}) output parameter value({1}) is invalid!", idAttr.Value.Trim(), outputAttr.Value.Trim());
                        throw new Exception(exceptionMessage);
                    }


                    var output = new KeyValuePair<String, Tuple<Type, String>>(outputAttr == null ? String.Empty : outputAttr.Value.Trim(), new Tuple<Type, String>(typeof(String), String.Empty));
                    var constructorParameters = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase) { 
                        {@"id", idAttr.Value.Trim()},
                        {@"description", descriptionAttr == null ? String.Empty : descriptionAttr.Value.Trim()},
                        //{@"method", methodAttr.Value.Trim()},
                        //{@"host", hostAttr.Value.Trim()},
                        //{@"tenantid", tenantidAttr.Value.Trim()},
                        {@"output", output},
                        {@"parameters", cmdParameters}
                    };
                    var cmd = (RestfulCommand)Helper.CreateCommand(cmdType, constructorParameters);
                    rtn.Add(cmd);
                }
                return rtn;
            }
            catch (Exception ex)
            {
                Logging.SaveLog(String.Format(@"An unexpected error occurred({0} parsing failed).", xmlFilePath), ELogType.Info);
                Logging.SaveLog(ex, ELogType.Error);                throw;
            }
        }

        #endregion

        #region sqlCommand
        public List<SQLExecute> GetSqlCommand(List<String> setting)
        {
            var sqlPath = setting.Select(p => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p))).ToList();
            var invalidPath = sqlPath.Where(p => Directory.Exists(p) == false && (File.Exists(p) == false || (File.Exists(p) && Path.GetExtension(p).ToLower() != ".xml"))).ToArray();
            if (invalidPath.Any())
            {
                var exceptionMessage = String.Format(@"SQL command path({0}) not found or file without the XML file extension!", String.Join(",", invalidPath));
                throw new Exception(exceptionMessage);
            }


            var rtn = new List<SQLExecute>();
            var arrXmlFilePath = sqlPath.Where(p => Directory.Exists(p))
                            .SelectMany(p => Directory.GetFiles(p, "*.xml", SearchOption.AllDirectories))
                            .Union(sqlPath.Where(p => File.Exists(p) && Path.GetExtension(p).ToLower() == ".xml"))
                            .ToArray();
            foreach (var xmlFilePath in arrXmlFilePath)
            {
                var tmp = this.XmlToSqlCommand(xmlFilePath);
                var duplicateList = rtn.Select(p => p.Id).Intersect(tmp.Select(p => p.Id));
                if (duplicateList.Any())
                {
                    var exceptionMessage = String.Format(@"SQL command id({0}) is duplicated!", String.Join(",", duplicateList));
                    throw new Exception(exceptionMessage);
                }

                rtn.AddRange(tmp);
                //modify by zhuqianqian log4net start
                //Logger.Write(String.Format("SQL commands file({0}) contains {1} statements!", xmlFilePath, tmp.Count()), "Info");
                Logging.SaveLog(String.Format("SQL commands file({0}) contains {1} statements!", xmlFilePath, tmp.Count()), ELogType.Info);
                //modify by zhuqianqian log4net end
            }
            return rtn;
        }

        private List<SQLExecute> XmlToSqlCommand(String xmlFilePath)
        {
            try
            {
                var rtn = new List<SQLExecute>();
                var doc = XDocument.Load(xmlFilePath);
                var requiredAttributes = new[] { "id", "type", "database" };
                foreach (var p in doc.Descendants("SQLs").Descendants("SQL"))
                {
                    var missingAttributes = requiredAttributes.Where(x => p.Attributes().Any(tc => tc.Name.LocalName.ToLower() == x) == false);
                    if (missingAttributes.Any())
                    {
                        var exceptionMessage = String.Format(@"SQL element is missing required attributes({0})!", String.Join(",", missingAttributes));
                        throw new Exception(exceptionMessage);
                    }
                    if (p.Attributes().Any(tc => tc.Name.LocalName.ToLower() == "statement"))
                    {
                        var exceptionMessage = @"SQL element can't containts ""statement"" attribute!";
                        throw new Exception(exceptionMessage);
                    }
                    var idAttr = p.Attributes().First(tc => tc.Name.LocalName.ToLower() == "id");
                    var descriptionAttr = p.Attributes().FirstOrDefault(tc => tc.Name.LocalName.ToLower() == "description");
                    var typeAttr = p.Attributes().First(tc => tc.Name.LocalName.ToLower() == "type");
                    var dbAttr = p.Attributes().First(tc => tc.Name.LocalName.ToLower() == "database");
                    var outputAttr = p.Attributes().FirstOrDefault(tc => tc.Name.LocalName.ToLower() == "output");
                    var cmdType = this._commandTypes.First(type => type.Name.ToLower() == typeAttr.Value.Trim().ToLower());
                    var cmdParameters = p.Attributes().Any(x => this._preDefineAttr.Contains(x.Name.LocalName.ToLower()) == false) ?
                            p.Attributes().Where(x => this._preDefineAttr.Contains(x.Name.LocalName.ToLower()) == false)
                            .OrderBy(att => att.Name.ToString())
                            .ToDictionary(att => "${" + att.Name.ToString() + "}"
                                , att => new Tuple<Type, String>(typeof(String), ("" + att.Value).Trim()), StringComparer.OrdinalIgnoreCase) :
                            new Dictionary<String, Tuple<Type, String>>(StringComparer.OrdinalIgnoreCase);
                    cmdParameters.Add("Statement", new Tuple<Type, String>(typeof(String), p.Descendants("Expression").First().Value));
                    if (outputAttr != null && outputAttr.Value.Trim().StartsWith("${") && Regex.IsMatch(outputAttr.Value.Trim(), Helper.CommandOutputTokenPattern) == false)
                    {
                        var exceptionMessage = String.Format(@"SQL({0}) output parameter value({1}) is invalid!", idAttr.Value.Trim(), outputAttr.Value.Trim());
                        throw new Exception(exceptionMessage);                        
                    }


                    var output = new KeyValuePair<String, Tuple<Type, String>>(outputAttr == null ? String.Empty : outputAttr.Value.Trim(), new Tuple<Type, String>(typeof(String), String.Empty));
                    if (cmdType == typeof(SQLExecuteDataTable))
                    {
                        output = new KeyValuePair<String, Tuple<Type, String>>(outputAttr == null ? String.Empty : outputAttr.Value.Trim(), new Tuple<Type, String>(typeof(List<Dictionary<String, String>>), String.Empty));
                    }
                    var constructorParameters = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase) { 
                        {@"id", idAttr.Value.Trim()},
                        {@"description", descriptionAttr == null ? String.Empty : descriptionAttr.Value.Trim()},
                        {@"output", output},
                        {@"parameters", cmdParameters},
                        {@"database", dbAttr.Value.Trim()}
                    };

                    var cmd = (SQLExecute)Helper.CreateCommand(cmdType, constructorParameters);
                    rtn.Add(cmd);
                }
                return rtn;
            }
            catch (Exception ex)
            {
                //modify by zhuqianqian log4net start
                //Logger.Write(String.Format(@"An unexpected error occurred({0} parsing failed).", xmlFilePath), "Info");
                Logging.SaveLog(String.Format(@"An unexpected error occurred({0} parsing failed).", xmlFilePath), ELogType.Info);
                Logging.SaveLog(ex, ELogType.Error);
                //modify by zhuqianqian log4net end
                throw;
            }
        }
        #endregion

        #region virtualCommand
        public List<VirtualCommand> GetVirtualCommand(List<String> setting, List<SQLExecute> sqlCommands, List<RestfulCommand> restfulCommands)
        {
            var virtualFolderPath = setting.Select(p => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p))).ToList();
            var invalidPath = virtualFolderPath.Where(p => Directory.Exists(p) == false && (File.Exists(p) == false || (File.Exists(p) && Path.GetExtension(p).ToLower() != ".xml"))).ToArray();
            if (invalidPath.Any())
            {
                var exceptionMessage = String.Format(@"Virtual command path({0}) not found or file without the XML file extension!", String.Join(",", invalidPath));
                throw new Exception(exceptionMessage);
            }

            
            var rtn = new List<VirtualCommand>();
            var arrXmlFilePath = virtualFolderPath.Where(p => Directory.Exists(p))
                .SelectMany(p => Directory.GetFiles(p, "*.xml", SearchOption.AllDirectories))
                .Union(virtualFolderPath.Where(p => File.Exists(p) && Path.GetExtension(p).ToLower() == ".xml"))
                .ToArray();
            foreach (var xmlFilePath in arrXmlFilePath)
            {
                var tmp = this.XmlToVirtualCommand(xmlFilePath, sqlCommands, restfulCommands);
                var duplicateList = sqlCommands.Select(p => p.Id).Intersect(tmp.Select(p => p.Id));
                if (duplicateList.Any())
                {
                    var exceptionMessage = String.Format(@"Virtual command id({0}) already exists in SQLCommand!", String.Join(",", duplicateList));
                    throw new Exception(exceptionMessage);
                }
                //*add for restfulCommands start
                duplicateList = restfulCommands.Select(p => p.Id).Intersect(tmp.Select(p => p.Id));
                if (duplicateList.Any())
                {
                    var exceptionMessage = String.Format(@"Virtual command id({0}) already exists in RestfulCommand!", String.Join(",", duplicateList));
                    throw new Exception(exceptionMessage);
                }
                //*add for restfulCommands end
                duplicateList = rtn.Select(p => p.Id).Intersect(tmp.Select(p => p.Id));
                if (duplicateList.Any())
                {
                    var exceptionMessage = String.Format(@"Virtual command id({0}) is duplicated!", String.Join(",", duplicateList));
                    throw new Exception(exceptionMessage);
                }

                rtn.AddRange(tmp);
                //modify by zhuqianqian log4net start
                //Logger.Write(String.Format("Virtual commands file({0}) contains {1} commands!", xmlFilePath, tmp.Count()), "Info");
                Logging.SaveLog(String.Format("Virtual commands file({0}) contains {1} commands!", xmlFilePath, tmp.Count()), ELogType.Info);
                //modify by zhuqianqian log4net end
            }
            return rtn;
        }

        private List<VirtualCommand> XmlToVirtualCommand(String xmlFilePath, List<SQLExecute> sqlCommands, List<RestfulCommand> restfulCommands)
        {
            try
            {
                var cmdType = typeof(VirtualCommand);
                var rtn = new List<VirtualCommand>();
                var doc = XDocument.Load(xmlFilePath);
                var requiredAttributes = new[] { "id" };
                foreach (var p in doc.Descendants("VirtualCommands").Descendants("VirtualCommand"))
                {
                    var missingAttributes = requiredAttributes.Where(x => p.Attributes().Any(tc => tc.Name.LocalName.ToLower() == x) == false);
                    if (missingAttributes.Any())
                    {
                        var exceptionMessage = String.Format(@"VirtualCommand element is missing required attributes({0})!", String.Join(",", missingAttributes));
                        throw new Exception(exceptionMessage);
                    }
                    var idAttr = p.Attributes().First(tc => tc.Name.LocalName.ToLower() == "id");
                    var descriptionAttr = p.Attributes().FirstOrDefault(tc => tc.Name.LocalName.ToLower() == "description");
                    var outputAttr = p.Attributes().FirstOrDefault(tc => tc.Name.LocalName.ToLower() == "output");
                    var cmdParameters = p.Attributes().Any(x => this._preDefineAttr.Contains(x.Name.LocalName.ToLower()) == false) ?
                            p.Attributes().Where(x => this._preDefineAttr.Contains(x.Name.LocalName.ToLower()) == false)
                            .OrderBy(att => att.Name.ToString())
                            .ToDictionary(att => "${" + att.Name.ToString() + "}"
                                , att => new Tuple<Type, String>(typeof(String), ("" + att.Value).Trim()), StringComparer.OrdinalIgnoreCase) :
                            new Dictionary<String, Tuple<Type, String>>(StringComparer.OrdinalIgnoreCase);
                    if (outputAttr != null && outputAttr.Value.Trim().StartsWith("${") && Regex.IsMatch(outputAttr.Value.Trim(), Helper.CommandOutputTokenPattern) == false)
                    {
                        var exceptionMessage = String.Format(@"VirtualCommand({0}) output parameter value({1}) is invalid!", idAttr.Value.Trim(), outputAttr.Value.Trim());
                        throw new Exception(exceptionMessage);
                    }


                    var output = new KeyValuePair<String, Tuple<Type, String>>(outputAttr == null ? String.Empty : outputAttr.Value.Trim(), new Tuple<Type, String>(typeof(String), String.Empty));
                    var constructorParameters = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase) { 
                        {@"id", idAttr.Value.Trim()},
                        {@"description", descriptionAttr == null ? String.Empty : descriptionAttr.Value.Trim()},
                        {@"output", output},
                        {@"parameters", cmdParameters},
                        {@"subCommands", this.XmlToCommand(p.Descendants().ToArray(), sqlCommands, restfulCommands,null)}
                    };

                    var cmd = (VirtualCommand)Helper.CreateCommand(cmdType, constructorParameters);
                    rtn.Add(cmd);
                }
                return rtn;
            }
            catch (Exception ex)
            {
                //modify by zhuqianqian log4net start
                //Logger.Write(String.Format(@"An unexpected error occurred({0} parsing failed).", xmlFilePath), "Info");
                Logging.SaveLog(String.Format(@"An unexpected error occurred({0} parsing failed).", xmlFilePath), ELogType.Info);
                Logging.SaveLog(ex, ELogType.Error);
                //modify by zhuqianqian log4net end
                throw;
            }
        }
        #endregion

        #region TestCase
        public List<TestCase> GetTestCase(List<String> setting, List<SQLExecute> sqlCommands, List<RestfulCommand> restfulCommands , List<VirtualCommand> virtualCommands, Func<XAttribute, Boolean> filter)
        {
            var testCaseFolderPath = setting.Select(p => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p))).ToList();
            var invalidPath = testCaseFolderPath.Where(p => Directory.Exists(p) == false && (File.Exists(p) == false || (File.Exists(p) && Path.GetExtension(p).ToLower() != ".xml"))).ToArray();
            if (invalidPath.Any())
            {
                var exceptionMessage = String.Format(@"Test cases path({0}) not found or file without the XML file extension!", String.Join(",", invalidPath));
                throw new Exception(exceptionMessage);
            }


            var rtn = new List<TestCase>();
            var arrXmlFilePath = testCaseFolderPath.Where(p => Directory.Exists(p))
                .SelectMany(p => Directory.GetFiles(p, "*.xml", SearchOption.AllDirectories))
                .Union(testCaseFolderPath.Where(p => File.Exists(p) && Path.GetExtension(p).ToLower() == ".xml"))
                .ToArray();
            foreach (var xmlFilePath in arrXmlFilePath)
            {
                var tmp = this.XmlToTestCase(xmlFilePath, sqlCommands, restfulCommands,virtualCommands, filter).ToList();
                rtn.AddRange(tmp);
                //modify by zhuqianqian log4net start
                //Logger.Write(String.Format("Test Cases file({0}) contains {1} cases!", xmlFilePath, tmp.Count()), "Info");
                Logging.SaveLog(String.Format("Test Cases file({0}) contains {1} cases!", xmlFilePath, tmp.Count()), ELogType.Info);
                //modify by zhuqianqian log4net end
            }
            return rtn;
        }

        private List<TestCase> XmlToTestCase(String xmlFilePath, List<SQLExecute> sqlCommands,  List<RestfulCommand> restfulCommands, List<VirtualCommand> virtualCommands, Func<XAttribute, Boolean> filter)
        {
            try
            {
                var rtn = new List<TestCase>();
                var doc = XDocument.Load(xmlFilePath);
                var requiredAttributes = new[] { "id", "description" };
                foreach (var p in doc.Descendants("TestCases").Descendants("TestCase"))
                {
                    var missingAttributes = requiredAttributes.Where(x => p.Attributes().Any(tc => tc.Name.LocalName.ToLower() == x) == false);
                    if (missingAttributes.Any())
                    {
                        var exceptionMessage = String.Format(@"TestCase element is missing required attributes({0})!", String.Join(",", missingAttributes));
                        throw new Exception(exceptionMessage);
                    }
                    var idAttr = p.Attributes().First(tc => tc.Name.LocalName.ToLower() == "id");
                    var descAttr = p.Attributes().First(tc => tc.Name.LocalName.ToLower() == "description");
                    var tagAttr = p.Attributes().FirstOrDefault(tc => tc.Name.LocalName.ToLower() == "tag");
                    if (filter(idAttr) == false || filter(tagAttr) == false)
                        continue;


                    var cmd = new TestCase(idAttr.Value.Trim()
                               , descAttr.Value.Trim()
                               , tagAttr == null ? new string[] { } : tagAttr.Value.Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Trim().Length > 0).Select(x => x.Trim()).ToArray()
                               , this.XmlToCommand(p.Descendants().ToArray(), sqlCommands, restfulCommands,virtualCommands));
                    rtn.Add(cmd);
                }
                return rtn;
            }
            catch (Exception ex)
            {
                //modify by zhuqianqian log4net start
                //Logger.Write(String.Format(@"An unexpected error occurred({0} parsing failed).", xmlFilePath), "Info");
                Logging.SaveLog(String.Format(@"An unexpected error occurred({0} parsing failed).", xmlFilePath), ELogType.Error);
                Logging.SaveLog(ex, ELogType.Error);
                //modify by zhuqianqian log4net end
                throw;
            }
        }
        #endregion

        #region Helper
        private List<Command> XmlToCommand(XElement[] elements
            , List<SQLExecute> sqlCommands
            , List<RestfulCommand> restfulCommands
            , List<VirtualCommand> virtualCommands)
        {
            var list = new List<Command>();
            var requiredAttributes = new[] { "type" };
            foreach (var node in elements)
            {
                var missingAttributes = requiredAttributes.Where(x => node.Attributes().Any(tc => tc.Name.LocalName.ToLower() == x) == false);
                if (missingAttributes.Any())
                {
                    var exceptionMessage = String.Format(@"Command element is missing required attributes({0})!", String.Join(",", missingAttributes));
                    throw new Exception(exceptionMessage);
                }
                Command cmd;
                Type cmdType;
                Dictionary<String, Object> parameterValues;
                var typeAttr = node.Attributes().First(tc => tc.Name.LocalName.ToLower() == "type");
                var idAttr = node.Attributes().FirstOrDefault(tc => tc.Name.LocalName.ToLower() == "id");
                var descriptionAttr = node.Attributes().FirstOrDefault(tc => tc.Name.LocalName.ToLower() == "description");
                var outputAttr = node.Attributes().FirstOrDefault(tc => tc.Name.LocalName.ToLower() == "output");
                Dictionary<String, Tuple<Type, String>> cmdParameters = node.Attributes()
                    .Where(p => this._preDefineAttr.Contains(p.Name.LocalName.ToLower()) == false)
                    .OrderBy(att => att.Name.ToString())
                    .ToDictionary(att => att.Name.ToString(), att => new Tuple<Type, String>(typeof(String), ("" + att.Value).Trim()), StringComparer.OrdinalIgnoreCase);
                if (outputAttr != null && outputAttr.Value.Trim().StartsWith("${") && Regex.IsMatch(outputAttr.Value.Trim(), Helper.CommandOutputTokenPattern) == false)
                {
                    var exceptionMessage = String.Format(@"Command({0}) output parameter value({1}) is invalid!", idAttr.Value.Trim(), outputAttr.Value.Trim());
                    throw new Exception(exceptionMessage);
                }


                
                if (sqlCommands != null && sqlCommands.Any(p => p.Id.ToLower() == typeAttr.Value.Trim().ToLower()))
                {
                    var sqlcmd = sqlCommands.First(p => p.Id.ToLower() == typeAttr.Value.Trim().ToLower());
                    cmdType = sqlcmd.GetType();
                    String cmdId = sqlcmd.Id;
                    var cmdOutput = new KeyValuePair<String, Tuple<Type, String>>(outputAttr == null ? String.Empty : outputAttr.Value.Trim()
                        , new Tuple<Type, String>(sqlcmd.Output.Value.Item1, String.Empty));
                    cmdParameters = this.GetCommandParameters(sqlcmd.Parameters, cmdParameters);
                    parameterValues = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { @"id", cmdId},
                        { @"description", descriptionAttr == null ? String.Empty : descriptionAttr.Value.Trim()},
                        { @"output", cmdOutput},
                        { @"parameters", cmdParameters},
                        { @"database", sqlcmd.Database},
                    };
                    cmd = Helper.CreateCommand(cmdType, parameterValues);
                }
                else if (restfulCommands != null && restfulCommands.Any(p => p.Id.ToLower() == typeAttr.Value.Trim().ToLower()))
                {
                    //*add for restfulCommands start
                    var restfulCommand = restfulCommands.First(p => p.Id.ToLower() == typeAttr.Value.Trim().ToLower());
                    cmdType = restfulCommand.GetType();
                    String cmdId = restfulCommand.Id;
                    var cmdOutput = new KeyValuePair<String, Tuple<Type, String>>(outputAttr == null ? String.Empty : outputAttr.Value.Trim()
                        , new Tuple<Type, String>(restfulCommand.Output.Value.Item1, String.Empty));
                    cmdParameters = this.GetCommandParameters(restfulCommand.Parameters, cmdParameters);
                   // cmdParameters = this.GetCommandParameters(templateParameters, cmdParameters);
                    parameterValues = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { @"id", cmdId},
                        { @"description", descriptionAttr == null ? String.Empty : descriptionAttr.Value.Trim()},
                        { @"output", cmdOutput},
                        { @"parameters", cmdParameters}
                    };
                    cmd = Helper.CreateCommand(cmdType, parameterValues);
                    //*add for restfulCommands end
                }
                else if (virtualCommands != null && virtualCommands.Any(p => p.Id.ToLower() == typeAttr.Value.Trim().ToLower()))
                {
                    var vircmd = virtualCommands.First(p => p.Id.ToLower() == typeAttr.Value.Trim().ToLower());
                    cmdType = vircmd.GetType();
                    String cmdId = vircmd.Id;
                    var cmdOutput = new KeyValuePair<String, Tuple<Type, String>>(outputAttr == null ? String.Empty : outputAttr.Value.Trim()
                        , new Tuple<Type, String>(vircmd.Output.Value.Item1, String.Empty));


                    var templateParameters = vircmd.Parameters;
                    if (String.IsNullOrEmpty(cmdOutput.Key) == false)
                    {
                        templateParameters = templateParameters.ToDictionary(p => Regex.Replace(p.Key, Regex.Escape(vircmd.Output.Key), cmdOutput.Key, RegexOptions.IgnoreCase | RegexOptions.Multiline)
                            , p => new Tuple<Type, String>(p.Value.Item1, Regex.Replace(p.Value.Item2, Regex.Escape(vircmd.Output.Key), cmdOutput.Key, RegexOptions.IgnoreCase | RegexOptions.Multiline)));
                    }
                    cmdParameters = this.GetCommandParameters(templateParameters, cmdParameters);


                    var cmdSubCommands = vircmd.SubCommands.Select(p => p.DeepCopy()).ToList();
                    if (String.IsNullOrEmpty(cmdOutput.Key) == false)
                    {
                        cmdSubCommands.ForEach(p => {
                            p.Output = new KeyValuePair<String, Tuple<Type, String>>(Regex.Replace(p.Output.Key, Regex.Escape(vircmd.Output.Key), cmdOutput.Key, RegexOptions.IgnoreCase | RegexOptions.Multiline)
                                , new Tuple<Type, String>(p.Output.Value.Item1, Regex.Replace(p.Output.Value.Item2, Regex.Escape(vircmd.Output.Key), cmdOutput.Key, RegexOptions.IgnoreCase | RegexOptions.Multiline)));
                        });
                    }

                    parameterValues = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { @"id", cmdId},
                        { @"description", descriptionAttr == null ? String.Empty : descriptionAttr.Value.Trim()},
                        { @"output", cmdOutput},
                        { @"parameters", cmdParameters},
                        { @"subCommands", cmdSubCommands}
                    };
                    cmd = Helper.CreateCommand(cmdType, parameterValues);
                }
                else
                {
                    cmdType = this._commandTypes.FirstOrDefault(type => type.Name == typeAttr.Value.Trim());
                    if (cmdType == null)
                    {
                        var exceptionMessage = String.Format(@"An unexpected error occurred({0} type not found).", "Logic.Commands." + typeAttr.Value.Trim());
                        throw new Exception(exceptionMessage);
                    }

                    String cmdId = (idAttr == null) ? typeAttr.Value.Trim() : idAttr.Value.Trim();
                    var cmdOutput = new KeyValuePair<String, Tuple<Type, String>>(outputAttr == null ? String.Empty : outputAttr.Value.Trim()
                        , new Tuple<Type, String>(typeof(String), String.Empty));
                    parameterValues = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { @"id", cmdId},
                        { @"description", descriptionAttr == null ? String.Empty : descriptionAttr.Value.Trim()},
                        { @"output", cmdOutput},
                        { @"parameters", cmdParameters}
                    };
                    cmd = Helper.CreateCommand(cmdType, parameterValues);
                }
                list.Add(cmd);
            }
            return list;
        }

        private Dictionary<String, Tuple<Type, String>> GetCommandParameters(Dictionary<String, Tuple<Type, String>> template, Dictionary<String, Tuple<Type, String>> source)
        {
            var rtn = new Dictionary<String, Tuple<Type, String>>(template);
            var tmp = rtn.Where(p => source.Any(x => "${" + x.Key.ToString().ToLower() + "}" == p.Key.ToLower())).ToArray();
            foreach (var p in tmp)
            {
                var att = source.Where(x => "${" + x.Key.ToString().ToLower() + "}" == p.Key.ToLower()).First();
                rtn[p.Key] = new Tuple<Type, String>(att.Value.Item1, att.Value.Item2);
            }
            return rtn;
        }
        #endregion
    }
}
