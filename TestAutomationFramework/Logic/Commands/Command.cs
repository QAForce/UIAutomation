using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using System.IO;

namespace Logic.Commands
{
    public abstract class Command
    {
        public String Id { get; private set; }
        public String Description { get; private set; }
        public KeyValuePair<String, Tuple<Type, String>> Output { get; set; }
        public Dictionary<String, Tuple<Type, String>> Parameters { get; protected set; }
        public Boolean PassTest { get; protected set; }
        public Boolean SkipTest { get; protected set; }
        //[20150328][Tom] For if..else feature
        public Boolean IsExecuteCommand { get; set; }
        public Boolean IsExpectedFail { get; set; }

        public Command(String id, String description, KeyValuePair<String, Tuple<Type, String>> output, Dictionary<String, Tuple<Type, String>> parameters)
        {
            this.Id = id;
            this.Description = description;
            this.PassTest = false;
            this.SkipTest = false;
            this.Output = output;
            this.Parameters = parameters;
            //[20150328][Tom] For if..else feature
            this.IsExecuteCommand = true;
            this.IsExpectedFail = false;
        }

        public String GetParameter(String parameterName)
        {
            var tuple = this.GetParameters(parameterName);
            if (tuple.Item1 == typeof(String))
            {
                return tuple.Item2;
            }
            else
            {
                //[20150428][Tom] For non string format, return the value directly
                return tuple.Item2;
                //throw new NotSupportedException(String.Format("Parameter({0}) type({1}) not supported!", parameterName, tuple.Item1.ToString()));
            }
        }

        public Tuple<Type, String> GetParameters(String parameterName)
        {
            if (this.Parameters.ContainsKey(parameterName))
                return this.Parameters[parameterName];
            else
                throw new KeyNotFoundException(String.Format("ParameterKey({0}) not found!", parameterName));
        }

        public virtual void ApplyParameters(Dictionary<String, Tuple<Type, String>> parameters)
        {
            foreach (var item in parameters)
            {
                if (item.Value.Item1 == typeof(List<Dictionary<String, String>>))
                {
                    if (Regex.IsMatch(item.Key, Helper.CommandOutputTokenPattern) == false)
                        continue;

                    var dataTableName = "" + item.Key.Replace("${", String.Empty).Replace("}", String.Empty);
                    this.Parameters.Where(p => p.Value.Item1 == typeof(String)
                        && Regex.IsMatch(p.Value.Item2, Helper.CommandTokenPattern.Replace("{0}", dataTableName), RegexOptions.IgnoreCase | RegexOptions.Multiline))
                        .ToList()
                        .ForEach(p =>
                        {
                            var match = Regex.Match(p.Value.Item2, Helper.CommandTokenPattern.Replace("{0}", dataTableName), RegexOptions.IgnoreCase | RegexOptions.Multiline);
                            string token = match.Value;


                            if (String.IsNullOrEmpty(match.Groups["index"].Value) && String.IsNullOrEmpty(match.Groups["column"].Value))
                            {
                                this.Parameters[p.Key] = new Tuple<Type, String>(typeof(List<Dictionary<String, String>>), item.Value.Item2); 
                            }
                            else
                            {
                                int index = int.Parse("0" + match.Groups["index"].Value);
                                string column = match.Groups["column"].Value;


                                var dt = JsonConvert.DeserializeObject<List<Dictionary<String, String>>>(item.Value.Item2);
                                if ((index + 1) > dt.Count)
                                    throw new ArgumentOutOfRangeException(String.Format(@"Index({0}) out of range({1}).", index, dt.Count - 1));

                                var dtRow = new Dictionary<String, String>(dt.ElementAt(index), StringComparer.OrdinalIgnoreCase);
                                if (dtRow.ContainsKey(column) == false)
                                    throw new KeyNotFoundException("Unable to find key:" + column + " in ${" + dataTableName + "}");


                                var value = Regex.Replace(p.Value.Item2, Regex.Escape(token), dtRow[column], RegexOptions.IgnoreCase | RegexOptions.Multiline);
                                this.Parameters[p.Key] = new Tuple<Type, String>(typeof(String), value);
                            }
                        });

                }
                else
                {
                    var test = this.Parameters.Where(p => p.Value.Item1 == typeof(String)
                        && Regex.IsMatch(p.Value.Item2, Regex.Escape(item.Key), RegexOptions.IgnoreCase | RegexOptions.Multiline))
                        .ToList();

                    this.Parameters.Where(p => p.Value.Item1 == typeof(String)
                        && Regex.IsMatch(p.Value.Item2, Regex.Escape(item.Key), RegexOptions.IgnoreCase | RegexOptions.Multiline))
                        .ToList()
                        .ForEach(p =>
                        {
                            var value = Regex.Replace(p.Value.Item2, Regex.Escape(item.Key), item.Value.Item2, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                            this.Parameters[p.Key] = new Tuple<Type, String>(typeof(String), value);
                        });
                }
            }

            //foreach (var item in parameters)
            //{
            //    if (this.Parameters.ContainsKey(item.Key))
            //        this.Parameters[item.Key] = new Tuple<Type, String>(item.Value.Item1, item.Value.Item2);
            //}
        }

        public abstract void Run(TestContainer container);

        public Command DeepCopy()
        {
            Command other = (Command)this.MemberwiseClone();
            other.Output = new KeyValuePair<String, Tuple<Type, String>>(this.Output.Key
                , this.Output.Value == null ?
                new Tuple<Type, String>(typeof(String), String.Empty) :
                new Tuple<Type, String>(this.Output.Value.Item1, this.Output.Value.Item2));
            other.Parameters = new Dictionary<String, Tuple<Type, String>>(this.Parameters);
            return other;
        }

        /// <summary>
        /// Screenshot On Fail
        /// </summary>
        /// <param name="container">TestContainer</param>
        public void CommandFailScreenShot(TestContainer container)
        {
            string fileName = "";
            if (container._screenshotOnFail)
            {
                Screenshot screenShot = ((ITakesScreenshot)container.Driver).GetScreenshot();
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, container._filePathofScreenshotOnFail);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd-hhmm-ss");
                fileName = folderPath + timestamp + "-" + this.Id + ".png";
                screenShot.SaveAsFile(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            Logging.SaveLog(fileName, ELogType.Error);
        }

        /// <summary>
        /// IsCommandContinue
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public bool IsCommandContinue(TestContainer container)
        {
            Boolean isExecuteCommand = IsExecuteCommand;
            String stringIsExecuteCommand = GetParameterWithoutException("IsExecuteCommand");
            if (!string.IsNullOrEmpty(stringIsExecuteCommand))
            {
                Boolean.TryParse(stringIsExecuteCommand, out isExecuteCommand);
            }         
            if (!isExecuteCommand)
            {
                Logging.LoadLog(container._configFilePath);
                Logging.SaveLog(Id + " ExecuteCommand:" + stringIsExecuteCommand, ELogType.Info);
                return false;
            }
            return true;
        }

        /// <summary>
        /// GetParameterWithoutException
        /// </summary>
        /// <param name="parameterName"></param>
        /// <returns></returns>
        public String GetParameterWithoutException(String parameterName)
        {
            try
            {
                return GetParameter(parameterName);
            }
            catch
            {
                return "";
            }
 
        }

        /// <summary>
        /// GetOutPut
        /// </summary>
        /// <param name="key"></param>
        /// <param name="output"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public KeyValuePair<String, Tuple<Type, String>> GetOutPut(string key, KeyValuePair<String, Tuple<Type, String>> output, bool pass,bool hasValue=false , string outputValue = "")
        {
            if (string.IsNullOrEmpty(key) == false && !hasValue)
            {
                return new KeyValuePair<String, Tuple<Type, String>>(this.Output.Key, new Tuple<Type, String>(typeof(String), pass.ToString()));
            }
            else if (string.IsNullOrEmpty(key) == false && hasValue)
            {
                return new KeyValuePair<String, Tuple<Type, String>>(this.Output.Key, new Tuple<Type, String>(typeof(String), outputValue));
            }
            return output;     
        }

        /// <summary>
        /// GetPassTest
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool GetTestPassExpected(bool result)
        {
            Boolean isExpectedFail = IsExpectedFail;
            String stringIsExpectedFail = GetParameterWithoutException("IsExpectedFail");
            if (!string.IsNullOrEmpty(stringIsExpectedFail))
            {
                Boolean.TryParse(stringIsExpectedFail, out isExpectedFail);
            }
            return result & (!isExpectedFail);
        }
    }
}
