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
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace OstrichNet.Service
{
    public class DiagnosticsHttpModule : IHttpModule
    {
        private const string TimerKey = "__diagnostics:timer:key";
        private static readonly Regex pathsToTime = new Regex(@"^/+(.+)(/|.aspx|.asp)$");

        private static readonly Lazy<HttpDiagnosticsService> diagnosticsService = new Lazy<HttpDiagnosticsService>(() =>
        {
            var key = typeof(HttpDiagnosticsService).FullName + ".port";

            int port;
            if (!ConfigurationManager.AppSettings.AllKeys.Contains(key) || !Int32.TryParse(ConfigurationManager.AppSettings[key], out port))
            {
                throw new InvalidOperationException("You must have an AppSetting of '" + key +
                                                    "' that specifies the port " +
                                                    " for the diagnostics service to use");
            }
            var service = new HttpDiagnosticsService(port);

            service.Start();

            return service;

        }, LazyThreadSafetyMode.ExecutionAndPublication);


        /// <summary>
        /// Initializes a module and prepares it to handle requests.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpApplication"/> that provides access to the methods, properties, and events common to all application objects within an ASP.NET application</param>
        public void Init(HttpApplication context)
        {
            var service = diagnosticsService.Value;

            context.BeginRequest += BeginRequest;
            context.EndRequest += EndRequest;
        }        

        private static void BeginRequest(object sender, EventArgs e)
        {
            var httpApplication = (HttpApplication)sender;
            var path = httpApplication.Context.Request.Path.ToLower();

            var key = GetKey(path);
            if (string.IsNullOrEmpty(key)) return;

            key = key.Replace("/", ".");

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            httpApplication.Context.Items[TimerKey] = Tuple.Create(key, stopWatch);
        }

        private static void EndRequest(object sender, EventArgs e)
        {
            var httpApplication = (HttpApplication)sender;
            var pair = httpApplication.Context.Items[TimerKey] as Tuple<string, Stopwatch>;
            
            if (pair == null) return;

            Stats.Time(pair.Item1, pair.Item2);                
        }

        private static string GetKey(string url)
        {
            var matches = pathsToTime.Matches(url);
            return matches.Count > 0 ? matches[0].Groups[1].Value : string.Empty;
        }

        /// <summary>
        /// Disposes of the resources (other than memory) used by the module that implements <see cref="T:System.Web.IHttpModule"/>.
        /// </summary>
        public void Dispose()
        {
        }

		~DiagnosticsHttpModule() {
			try {
              diagnosticsService.Value.Dispose();
			} catch {
			  //ignored
			}
		}
    }
}
