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
using System.IO;
using System.Reflection;
using log4net;

namespace OstrichNet.Http
{
    public delegate void
        OwinApplication(IDictionary<string, object> env,
        Action<string, IDictionary<string, IList<string>>, IEnumerable<object>> completed,
        Action<Exception> faulted); 

    public static partial class Extensions
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Host(this IKayakServer server, OwinApplication application)
        {
            server.Host(application, null);
        }

        public static void Host(this IKayakServer server, OwinApplication application, Action<Action> trampoline)
        {
            server.HostInternal(application, trampoline).AsContinuation<object>(trampoline)
                (_ => { }, e => logger.Error(e));
        }

        static IEnumerable<object> HostInternal(this IKayakServer server, OwinApplication application, Action<Action> trampoline)
        {
            while (true)
            {
                var accept = new ContinuationState<ISocket>((r, e) => server.GetConnection()(r));
                yield return accept;

                if (accept.Result == null)
                    break;

                accept.Result.ProcessSocket(new HttpSupport(), application, trampoline);
            }
        }

        public static void ProcessSocket(this ISocket socket, IHttpSupport http, OwinApplication application, Action<Action> trampoline)
        {
            socket.ProcessSocketInternal(http, application).AsContinuation<object>(trampoline)
                (_ => { }, e => logger.Error(e));
        }

        static IEnumerable<object> ProcessSocketInternal(this ISocket socket, IHttpSupport http, OwinApplication application)
        {
            var beginRequest = http.BeginRequest(socket);
            yield return beginRequest;

            var request = beginRequest.Result;

            var invoke = new ContinuationState<Tuple<string, IDictionary<string, IList<string>>, IEnumerable<object>>>
                ((r, e) => 
                    application(request, 
                                (s,h,b) => 
                                    r(new Tuple<string,IDictionary<string,IList<string>>,IEnumerable<object>>(s, h, b)), 
                                e));

            yield return invoke;

            var response = invoke.Result;

            yield return http.BeginResponse(socket, response.Item1, response.Item2);

            foreach (var obj in response.Item3)
            {
                var objectToWrite = obj;

                if (obj is Action<Action<object>, Action<Exception>>)
                {
                    var cs = new ContinuationState<object>(obj as Action<Action<object>, Action<Exception>>);

                    yield return cs;

                    objectToWrite = cs.Result;
                }

                if (objectToWrite is FileInfo)
                {
                    yield return new ContinuationState(socket.WriteFile((objectToWrite as FileInfo).Name));
                    continue;
                }

                var chunk = default(ArraySegment<byte>);

                if (objectToWrite is ArraySegment<byte>)
                    chunk = (ArraySegment<byte>)objectToWrite;
                else if (objectToWrite is byte[])
                    chunk = new ArraySegment<byte>(objectToWrite as byte[]);
                else
                    continue;
                    //throw new ArgumentException("Invalid object of type " + obj.GetType() + " '" + obj.ToString() + "'");

                var write = socket.WriteChunk(chunk);
                yield return write;

                // TODO enumerate to completion
                if (write.Exception != null)
                    throw write.Exception;
            }

            socket.Dispose();
        }
    }
}
