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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NUnit.Framework;
using OstrichNet.Service;

namespace OstrichNet.Tests
{
    /// <summary>
    /// Regression tests for the http server
    /// </summary>
    [TestFixture]
    public class HttpServerTests
    {
        [Test]
        public void HtppServiceStarts()
        {
            using (var service = new HttpDiagnosticsService())
                service.Start();
        }

        [Ignore]
        [TestCase("/stats.txt")]
        [TestCase("/server_info.txt")]
        [TestCase("/graph_data")]
        [TestCase("/stats")]
        [TestCase("/server_info")]
        [TestCase("/ping")]
        [TestCase("/static/index.html")]
        [TestCase("/graph")]
        [TestCase("/report")]
        public void HttpServerReturnsValidPage(String page)
        {
            using (var service = new HttpDiagnosticsService())
            {
                service.Start();
                Assert.That(StatusOfPage(page, service.Port), Is.EqualTo(WebExceptionStatus.Success));
            }
        }      

        private WebExceptionStatus StatusOfPage(String url, int port)
        {
            using (var client = new WebClient())
            {
                try
                {
                    using (client.OpenRead(new Uri(String.Format("http://localhost:{0}{1}", port, url))))
                    {
                        return WebExceptionStatus.Success;
                    }                    
                }
                catch (WebException ex)
                {
                    return ex.Status;
                }  
            }
        }
    }
}
