using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using OstrichNet.Util;

namespace OstrichNet.Service
{
    internal interface IResourceHandler
    {
        void Handle(string path, NameValueCollection parameters, Action<string, IDictionary<string, IList<string>>, IEnumerable<object>> response);
    }

    internal abstract class AbstractResourceHandler : IResourceHandler
    {
        protected const string Ok = "200 OK";
        protected const string NotFound = "404 Not Found";
        protected const string HtmlFor404 = "<html><head><title>404</title></head><body>Not Found</body></html>";

        protected static readonly string ApplicationJson = "application/json"; 
        protected static readonly string TextPlain = "text/plain";
        protected static readonly string TextHtml = "text/html";
        protected static readonly string TextJavascript = "text/javascript";

        protected static IDictionary<string, IList<string>> GetContentType(string type)
        {
            return new Dictionary<string, IList<string>>
                                {
                                    { "Content-Type",  new[] { type } }
                                };
        }

        protected IEnumerable<object> Encode(string response)
        {
            return new[] { Encoding.ASCII.GetBytes(response) };
        }

        protected static void SetPrettyPrint(JsonWriter jsonWriter, NameValueCollection parameters)
        {
            if (parameters.AllKeys.SingleOrDefault(k => k == "pretty") != null)
                jsonWriter.Formatting = Formatting.Indented; 
        }

        public abstract void Handle(string path, NameValueCollection parameters, Action<string, IDictionary<string, IList<string>>, IEnumerable<object>> response);
    }

    internal class ServerInfoHandler : AbstractResourceHandler
    {
        public override void Handle(string path, NameValueCollection parameters, Action<string, IDictionary<string, IList<string>>, IEnumerable<object>> response)
        {
            var serviceInfo = GetServerInfo();
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(path) && path.EndsWith(".txt"))
            {
                sb.AppendLine("server_info:");
                serviceInfo.Write(sb, 1);
                response(Ok, GetContentType(TextPlain), Encode(sb.ToString()));
                return;
            }

            using (var sw = new StringWriter(sb))
            using (var jsonWriter = new JsonTextWriter(sw))
            {
                var serializer = new JsonSerializer();
                SetPrettyPrint(jsonWriter, parameters);
                serializer.Serialize(jsonWriter, serviceInfo);
            }

            response(Ok, GetContentType(ApplicationJson), Encode(sb.ToString()));
        }

        private static IDictionary<string, object> GetServerInfo()
        {
            var machineName = Environment.MachineName;
            var process = Process.GetCurrentProcess();
            var name = process.ProcessName;
            var startTime = process.StartTime;
            var entryAssembly = Assembly.GetEntryAssembly();
            var assembly = entryAssembly != null ? entryAssembly.FullName : AppDomain.CurrentDomain.BaseDirectory;
            return new Dictionary<string, object>
            {
                { "machine", machineName },
                { "process", name },
                { "app_name", assembly },
                { "start_time", startTime.ToIso8601(DateTimePrecision.Seconds) },
                { "uptime", Convert.ToString((DateTime.Now - startTime).TotalMilliseconds) }
            };
        }
    }

    internal class StatsResourceHandler : AbstractResourceHandler
    {
        public override void Handle(string path, NameValueCollection parameters, Action<string, IDictionary<string, IList<string>>, IEnumerable<object>> response)
        {
            var sb = new StringBuilder();
            
            if (path.EndsWith(".txt"))
            {
                Stats.ToDictionary().Write(sb, 0);
                response(Ok, GetContentType(TextPlain), Encode(sb.ToString()));
                return;
            }

            using (var sw = new StringWriter(sb))
            using (var jsonWriter = new JsonTextWriter(sw))
            {
                SetPrettyPrint(jsonWriter, parameters);
                Stats.WriteJson(jsonWriter);
            }

            response(Ok, GetContentType(ApplicationJson), Encode(sb.ToString()));
        }
    }
    
    internal class PingHandler : AbstractResourceHandler
    {
        public override void Handle(string path, NameValueCollection parameters, Action<string, IDictionary<string, IList<string>>, IEnumerable<object>> response)
        {
            response(Ok, GetContentType(TextPlain), Encode("pong"));
        }
    }

    internal class NotFoundHandler : AbstractResourceHandler
    {
        public override void Handle(string path, NameValueCollection parameters, Action<string, IDictionary<string, IList<string>>, IEnumerable<object>> response)
        {
            response(NotFound, GetContentType(TextHtml), Encode(HtmlFor404));
        }
    }

    internal class StaticResourceHandler : AbstractResourceHandler
    {
        public override void Handle(string path, NameValueCollection parameters, Action<string, IDictionary<string, IList<string>>, IEnumerable<object>> response)
        {
            Stream binary;
            string type;
            if (TryGetStaticResource(path, out binary, out type))
            {
                using (var reader = new StreamReader(binary))
                {
                    response(Ok, GetContentType(type), Encode(reader.ReadToEnd()));
                }
            }
            else
                response(NotFound, GetContentType(TextHtml), new object[] { Encoding.ASCII.GetBytes(HtmlFor404) });
        }

        private static bool TryGetStaticResource(string path, out Stream binary, out string type)
        {
            type = path.EndsWith("js") ? "text/javascript" : "text/html";

            var resource = path.Substring(8);
            binary = null;
            try
            {
                binary = Assembly.GetExecutingAssembly().GetManifestResourceStream("OstrichNet.Service.Static." + resource);
            }
            catch (Exception)
            {
                return false;
            }
            return binary != null;
        }
    }

    internal class GraphDataHandler : AbstractResourceHandler
    {
        private readonly TimeSeriesCollector collector;

        public GraphDataHandler(TimeSeriesCollector collector)
        {
            this.collector = collector;
        }

        public override void Handle(string path, NameValueCollection parameters, Action<string, IDictionary<string, IList<string>>, IEnumerable<object>> response)
        {
            var items = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (items.Length < 2)
            {
                var sb = new StringBuilder();
                using (var sw = new StringWriter(sb))
                using (var jsonWriter = new JsonTextWriter(sw))
                {
                    SetPrettyPrint(jsonWriter, parameters);
                    var s = new JsonSerializer();
                    s.Serialize(jsonWriter, new Dictionary<string, IEnumerable<string>> { { "keys", collector.GetKeys() } });
                }

                response(Ok, GetContentType(ApplicationJson), Encode(sb.ToString())); response(Ok, GetContentType(ApplicationJson), Encode(sb.ToString()));
            } 
            else
            {
                var graphItem = items[1];
                if (graphItem.IndexOf("?") != -1)
                    graphItem = graphItem.Substring(0, graphItem.IndexOf("?"));

                var sel = parameters["p"];
                var selection = !string.IsNullOrEmpty(sel) ? sel.Split(',').Select(s => Convert.ToInt32(s)) : null;

                var timeSeries = collector.Get(graphItem, selection);

                var sb = new StringBuilder();
                using (var sw = new StringWriter(sb))
                using (var jsonWriter = new JsonTextWriter(sw))
                {
                    SetPrettyPrint(jsonWriter, parameters);
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName(graphItem);
                    jsonWriter.WriteStartArray();
                    foreach (var row in timeSeries)
                    {
                        jsonWriter.WriteStartArray();
                        row.Each(jsonWriter.WriteValue);
                        jsonWriter.WriteEndArray();
                    }
                    jsonWriter.WriteEndArray();
                    jsonWriter.WriteEndObject();
                }
                response(Ok, GetContentType(ApplicationJson), Encode(sb.ToString()));
            }
        }
    }
}

