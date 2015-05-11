using System;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Reflection;
using Logic.Commands;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Collections;
using Newtonsoft.Json;
using System.Data;

namespace Logic
{
    public static class Helper
    {
        ///// <summary>
        ///// Perform a deep Copy of the object.
        ///// </summary>
        ///// <typeparam name="T">The type of object being copied.</typeparam>
        ///// <param name="source">The object instance to copy.</param>
        ///// <returns>The copied object.</returns>
        //public static T Clone<T>(T source)
        //{
        //    if (!typeof(T).IsSerializable)
        //    {
        //        throw new ArgumentException("The type must be serializable.", "source");
        //    }

        //    // Don't serialize a null object, simply return the default for that object
        //    if (Object.ReferenceEquals(source, null))
        //    {
        //        return default(T);
        //    }

        //    IFormatter formatter = new BinaryFormatter();
        //    Stream stream = new MemoryStream();
        //    using (stream)
        //    {
        //        formatter.Serialize(stream, source);
        //        stream.Seek(0, SeekOrigin.Begin);
        //        return (T)formatter.Deserialize(stream);
        //    }
        //}

        public static readonly string CommandOutputTokenPattern = @"\$\{([a-zA-Z0-9]*)\}";

        public static readonly string CommandTokenPattern = @"\$\{{0}(\[(?<index>\d+)\])?\.?(?<column>[a-zA-Z0-9]*?)\}";

        public static string SerializeToJSon(this DataTable dt)
        {
            var columns = dt.Columns.Cast<DataColumn>().ToList();
            List<Dictionary<string, object>> dataRows = new List<Dictionary<string, object>>();
            dt.Rows.Cast<DataRow>().ToList().ForEach(dataRow =>
            {
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                columns.ForEach(column =>
                {
                    row.Add(column.ColumnName, dataRow[column]);
                });
                dataRows.Add(row);
            });
            return JsonConvert.SerializeObject(dataRows);
        }

        public static Command CreateCommand(Type commandType, Dictionary<string, object> parameters)
        {
            var constructor = commandType.GetConstructors()
                .Where(p => p.GetParameters().Length >= parameters.Count)
                .First();
            var constructorParameters = constructor.GetParameters().Select(p =>
            {
                if (parameters.ContainsKey(p.Name))
                {
                    return parameters[p.Name];
                }
                else
                {
                    return p.ParameterType.GetDefaultValue();
                }
            }).ToArray();
            var cmd = constructor.Invoke(constructorParameters);
            return (Command)cmd;
        }

        public static object GetDefaultValue(this Type t)
        {
            if (t.IsValueType && Nullable.GetUnderlyingType(t) == null)
                return Activator.CreateInstance(t);
            else
                return null;
        }

        public static Func<XAttribute, Boolean> CreateIdFilterPredicate(IList<string> idSearchPattern)
        {
            if (idSearchPattern == null)
                return (XAttribute idAttr) => { return true; };


            var arrIdPattern = idSearchPattern.Where(p => p.Trim().Length > 0).Select(p => p.ToLower().Trim()).ToArray();
            if (arrIdPattern != null && arrIdPattern.Length == 0)
                return (XAttribute idAttr) => { return true; };


            return (XAttribute idAttr) =>
            {
                if (idAttr == null || idAttr.Name.ToString().ToLower() != "id")
                    return true;

                if (arrIdPattern.Where(p => p.Contains("*") == false && idAttr.Value.ToLower() == p).Any())
                    return true;

                if (arrIdPattern.Where(p => p.Contains("*") == true && idAttr.Value.ToLower().StartsWith(p.Replace("*", ""))).Any())
                    return true;

                return false;
            };
        }

        public static Func<XAttribute, Boolean> CreateTagFilterPredicate(IList<string> tagSearchPattern)
        {
            if (tagSearchPattern == null)
                return (XAttribute tagAttr) => { return true; };


            var arrTagPattern = tagSearchPattern.Where(p => p.Trim().Length > 0).Select(p => p.ToLower().Trim()).ToArray();
            if (arrTagPattern != null && arrTagPattern.Length == 0)
                return (XAttribute tagAttr) => { return true; };


            return (XAttribute tagAttr) =>
            {
                if (tagAttr == null || tagAttr.Name.ToString().ToLower() != "tag")
                    return true;


                var tag = tagAttr.Value.Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Trim().Length > 0).Select(x => x.Trim()).ToArray();
                if (arrTagPattern.Intersect(tag.Select(p => p.ToLower())).Any())
                    return true;
                else
                    return false;
            };
        }
    }
}
