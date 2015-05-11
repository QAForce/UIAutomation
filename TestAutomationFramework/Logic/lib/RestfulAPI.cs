using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Microsoft.Http;


namespace Logic.Commands
{
    public class RestfulAPI : Command
    {
        public RestfulAPI(String id, String description, KeyValuePair<String, Tuple<Type, String>> output, Dictionary<String, Tuple<Type, String>> parameters)
            : base(id, description, output, parameters)
        {
        }

        public override void Run(TestContainer container)
        {
            try
            {
                var jsonResult = String.Empty;
                var target = base.GetParameter("Target");
                var methodType = base.GetParameter(@"MethodType").ToUpper();
                var jsonHeader = base.GetParameter(@"Header");
                var jsonBody = base.GetParameter(@"Body");



                using (Microsoft.Http.HttpClient client = new Microsoft.Http.HttpClient())
                {
                    var headers = JsonConvert.DeserializeObject<Dictionary<String, String>>(jsonHeader);
                    client.DefaultHeaders.Add("Content-Type","application/json");
                    foreach (KeyValuePair<string, string> item in headers)
                    {
                        client.DefaultHeaders.Add(item.Key, item.Value);
                    }
                    var content = Microsoft.Http.HttpContent.Create(jsonBody.Replace(@"'", @""""));

                    HttpMethod httpMethod = (HttpMethod)Enum.Parse(typeof(HttpMethod), methodType);
                    HttpResponseMessage responseMessage = client.Send(httpMethod, new Uri(target), client.DefaultHeaders, content);
                    var stringContent = responseMessage.Content.ReadAsString();
                    var statusCode = responseMessage.StatusCode;
                    this.PassTest = statusCode == System.Net.HttpStatusCode.OK ? true : false;
                    if (this.PassTest == false)
                    {
                        throw (new Exception(String.Format(@"uri:{0}, methodType:{1}, statusCode:{2}, response message:{3}.", target, methodType, statusCode, stringContent)));
                    }

                    if (string.IsNullOrEmpty(this.Output.Key) == false)
                    {
                        if (String.IsNullOrEmpty(stringContent))
                        {
                            this.Output = new KeyValuePair<String, Tuple<Type, String>>(this.Output.Key, new Tuple<Type, String>(typeof(String), String.Empty));
                        }
                        else
                        {
                            stringContent = stringContent.Trim();
                            if (stringContent.StartsWith("[") == false && stringContent.EndsWith("[") == false)
                            {
                                stringContent = "[" + stringContent + "]";
                                JsonConvert.DeserializeObject<List<Dictionary<String, String>>>(stringContent);
                            }
                            this.Output = new KeyValuePair<String, Tuple<Type, String>>(this.Output.Key, new Tuple<Type, String>(typeof(List<Dictionary<String, String>>), stringContent));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw (new Exception(ex.Message));
            }
        }
    }
}
