using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Logic.Commands
{
    public abstract class SQLExecute : Command
    {
        public String Database { get; private set; }

        public SQLExecute(String id, String description, KeyValuePair<String, Tuple<Type, String>> output, Dictionary<String, Tuple<Type, String>> parameters
            , String database)
            : base(id, description, output, parameters)
        {
            this.Database = database;
        }

        public override void ApplyParameters(Dictionary<String, Tuple<Type, String>> parameters)
        {
            base.ApplyParameters(parameters);
            var sqlStatement = this.Parameters["Statement"].Item2;
            var sqlStatementTokens = Regex.Matches(sqlStatement, Helper.CommandOutputTokenPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline)
                                                            .Cast<Match>()
                                                            .Select(p => p.Value).ToArray();
            var tmp = this.Parameters
                .Where(p => p.Value.Item1 == typeof(String) && sqlStatementTokens.Contains(p.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(p => p.Key, p => p.Value.Item2);
            //TODO:Using SQLParameters
            foreach (var item in tmp)
            {
                sqlStatement = Regex.Replace(sqlStatement, Regex.Escape(item.Key), item.Value, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }
            this.Parameters["Statement"] = new Tuple<Type, String>(typeof(String), sqlStatement);
        }

        public override void Run(TestContainer container)
        {
            throw new NotSupportedException();
        }
    }
}
