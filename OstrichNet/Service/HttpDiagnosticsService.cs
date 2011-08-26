/*
 *  Licensed to the Apache Software Foundation (ASF) under one or more
 *  contributor license agreements.  See the NOTICE file distributed with
 *  this work for additional information regarding copyright ownership.
 *  The ASF licenses this file to You under the Apache License, Version 2.0
 *  (the "License"); you may not use this file except in compliance with
 *  the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *
 */
﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;
using System.Reflection;
using System.Web;
using log4net;
using OstrichNet.Http;
using OstrichNet.Util;

namespace OstrichNet.Service
{
    public class HttpDiagnosticsService : IDisposable
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly IResourceHandler serviceInfoHandler = new ServerInfoHandler();
        private static readonly IResourceHandler statsHandler = new StatsResourceHandler();
        private static readonly IResourceHandler pingHandler = new PingHandler();
        private static readonly IResourceHandler staticResourceHandler = new StaticResourceHandler();
        private readonly IResourceHandler graphDataHandler;
        private readonly IServiceConfiguration configuration;
        private readonly TimeSeriesCollector collector;
        
        private IDisposable pipe;
        private readonly CarbonWriter carbonWriter;

        /// <summary>
        /// Get the configured tcp/ip port, or -1 if no configuration 
        /// has been set.
        /// </summary>
        public int Port
        {
            get { return configuration != null ? configuration.Port : -1; }
        }
        
        public HttpDiagnosticsService() : this(GetConfiguration())
        {
        }

        public HttpDiagnosticsService(int port) : this(new StaticServiceConfiguration(port))
        {
        }

        public HttpDiagnosticsService(IServiceConfiguration configuration)
        {
            this.configuration = configuration;
            collector = new TimeSeriesCollector();
            carbonWriter = CarbonWriterFactory.Instance();
            graphDataHandler = new GraphDataHandler(collector);
        }

        public void Start()
        {
            try
            {
                var configurationPort = configuration.Port;
                HttpServer server = null;
                pipe = Retry.AtMost(5).Try(() =>
                {
                    var host = new IPEndPoint(IPAddress.Any, configurationPort);
                    server = new HttpServer(host);
                    logger.Debug("Staring diagnostics service on " + host);
                    try
                    {
                        return server.Start();
                    }
                    catch (Exception)
                    {
                        configurationPort++;
                        throw;
                    }
                });

                server.Host((env, respond, error) =>
                {
                    try
                    {
                        var path = (string)env["Owin.RequestUri"];
                        path = path.ToLower();
                        if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);

                        var queryStringIdx = path.IndexOf("?");
                        var parameters = queryStringIdx == -1 ? new NameValueCollection() : HttpUtility.ParseQueryString(path.Substring(path.IndexOf("?")));

                        IResourceHandler handler;
                        if (path.StartsWith("/stats"))
                            handler = statsHandler;
                        else if (path.StartsWith("/server_info"))
                            handler = serviceInfoHandler;
                        else if (path.StartsWith("/ping"))
                            handler = pingHandler;
                        else if (path.StartsWith("/static"))
                            handler = staticResourceHandler;
                        else if (path.StartsWith("/report"))
                        {
                            path = "/static/report.html";
                            handler = staticResourceHandler;
                        }
                        else if (path.StartsWith("/graph_data"))
                            handler = graphDataHandler;
                        else if (path.StartsWith("/graph"))
                        {
                            path = "/static/graph.html";
                            handler = staticResourceHandler;
                        }
                        else
                        {
                            path = "/static/index.html";
                            handler = staticResourceHandler;
                        }

                        handler.Handle(path, parameters, respond);
                    }
                    catch (Exception e)
                    {
                        error(e);
                        respond("500 Internal Server Error", new Dictionary<string, IList<string>>
                                {
                                    { "Content-Type",  new[] { "text/html" } }
                                }, new[] { string.Empty });
                    }
                });

                logger.Debug("Listening on " + server.ListenEndPoint);
            }
// ReSharper disable EmptyGeneralCatchClause
            catch (Exception)
// ReSharper restore EmptyGeneralCatchClause
            {
            }
        }

        public void Dispose()
        {
            // ReSharper disable EmptyGeneralCatchClause
            try { collector.Dispose(); } catch { }
            try { pipe.Dispose(); } catch { }
            try { if (carbonWriter != null) carbonWriter.Dispose(); } catch {}
            // ReSharper restore EmptyGeneralCatchClause
        }

        private static IServiceConfiguration GetConfiguration()
        {
            IServiceConfiguration configuration = null;
            try
            {
                configuration = (ServiceConfiguration) ConfigurationManager.GetSection("etservice/diagnostics");
            }
            catch (Exception e)
            {
                logger.Warn("Could not load configuration section etservice/diagnostics. Using defaults", e);
            }
            return configuration ?? new StaticServiceConfiguration();
        }
    }
}
