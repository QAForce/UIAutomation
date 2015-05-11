using Microsoft.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Logic.Commands
{
    public class RestfulCommand : Command
    {
        public RestfulCommand(String id
            , String description
            , KeyValuePair<String, Tuple<Type, String>> output
            , Dictionary<String, Tuple<Type, String>> parameters)
            : base(id, description, output, parameters)
        {

        }

        public override void ApplyParameters(Dictionary<String, Tuple<Type, String>> parameters)
        {
            base.ApplyParameters(parameters);
        }

        

        public override void Run(TestContainer container)
        {
            var target = "";
            var methodType = "";
            try
            {
                Dictionary<String, Tuple<Type, String>> parameters = this.Parameters.Where(p => p.Key.StartsWith("${")).ToDictionary(p => p.Key, p => p.Value);
                base.ApplyParameters(parameters);
                base.ApplyParameters(container.GetGlobalParameters());
                var jsonResult = String.Empty;
                target = base.GetParameter("${Host}");
                methodType = base.GetParameter(@"${Method}").ToUpper();
                var jsonHeader = base.GetParameter(@"Header");
                var jsonBody = base.GetParameter(@"Body");
                var headers = JsonConvert.DeserializeObject<Dictionary<String, String>>(jsonHeader);

                var endPoint = new Uri(target);
                var webRequest = (HttpWebRequest)WebRequest.Create(endPoint);

                foreach (KeyValuePair<string, string> item in headers)
                {
                    //if(item.Key.ToLower() == "applicationname")
                    //{
                    //    webRequest.Headers.Add("ApplicationName", item.Value);
                    //}
                    //if(item.Key.ToLower() == "tenantid")
                    //{
                    //    webRequest.Headers.Add("TenantId", item.Value);
                    //}
                    //if(item.Key.ToLower() == "traceid")
                    //{
                    //    webRequest.Headers.Add("TraceID", item.Value);
                    //}
                    webRequest.Headers.Add(item.Key.ToLower(), item.Value);
                }
                webRequest.Method = methodType;
                webRequest.ContentType = "application/json";
                webRequest.Timeout = 600000;

                if (!string.IsNullOrEmpty(jsonBody))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(jsonBody);
                    webRequest.ContentLength = bytes.Length;
                    using (var requestStream = webRequest.GetRequestStream())
                    {
                        requestStream.Write(bytes, 0, bytes.Length);
                    }
                }
                else
                {
                    webRequest.ContentLength = 0;
                }

                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
                Stream receiveStream = webResponse.GetResponseStream();
                StreamReader readStream = new StreamReader(receiveStream, Encoding.GetEncoding("utf-8"));
                String resJson = readStream.ReadToEnd();
                var statusCode = webResponse.StatusCode;
                this.PassTest = statusCode == System.Net.HttpStatusCode.OK ? true : false;
                if (this.PassTest == false)
                {
                    throw (new Exception(String.Format(@"uri:{0}, methodType:{1}, statusCode:{2}, response message:{3}.", target, methodType, statusCode, resJson)));
                }

                if (string.IsNullOrEmpty(this.Output.Key) == false)
                {
                    if (String.IsNullOrEmpty(resJson))
                    {
                        this.Output = new KeyValuePair<String, Tuple<Type, String>>(this.Output.Key, new Tuple<Type, String>(typeof(String), String.Empty));
                    }
                    else
                    {
                        resJson = resJson.Trim();
                        if (resJson.StartsWith("[") == false && resJson.EndsWith("[") == false)
                        {
                            resJson = "[" + resJson + "]";
                            JsonConvert.DeserializeObject<List<Dictionary<String, String>>>(resJson);
                        }
                        this.Output = new KeyValuePair<String, Tuple<Type, String>>(this.Output.Key, new Tuple<Type, String>(typeof(List<Dictionary<String, String>>), resJson));
                    }
                    Logging.SaveLog("The API response:" + this.Output.Value.Item2.Trim(), ELogType.Info);
                    //Console.WriteLine(this.Output.Value.Item2.Trim());
                }

            }
            catch (WebException ex)
            {
                HttpWebResponse webResponse = ex.Response as HttpWebResponse;
                if (webResponse != null && webResponse.Headers["LiberalErrorCode"] != null)
                {
                    string errCode = Convert.ToString(webResponse.Headers["LiberalErrorCode"]);
                    string errMsg = Convert.ToString(webResponse.Headers["LiberalErrorMessage"]);
                    throw (new Exception(String.Format(@"uri:{0}, methodType:{1}, Error:{2}", target, methodType, errCode + ":" + errMsg)));
                }
                else
                {
                    throw (new Exception(String.Format(@"uri:{0}, methodType:{1}, Error:{2}", target, methodType, ex.ToString())));
                }
                
            }
            catch (Exception ex)
            {
                throw (new Exception(String.Format(@"uri:{0}, methodType:{1}, Error:{2}", target, methodType, ex.ToString())));
            }

        }
    }
}


   
