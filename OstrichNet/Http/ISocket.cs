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
using System.Net;

namespace OstrichNet.Http
{
    /// <summary>
    /// Represents a socket which supports asynchronous IO operations.
    /// </summary>
    public interface ISocket : IDisposable
    {
        /// <summary>
        /// The IP end point of the connected peer.
        /// </summary>
        IPEndPoint RemoteEndPoint { get; }

        /// <summary>
        /// Returns an observable which, upon subscription, begins an asynchronous write
        /// operation. When the operation completes, the observable yields the number of
        /// bytes written and completes.
        /// </summary>
        Action<Action<int>, Action<Exception>> Write(byte[] buffer, int offset, int count);

        /// <summary>
        /// Returns an observable which, upon subscription, begins copying a file
        /// to the socket. When the copy operation completes, the observable completes.
        /// </summary>
        Action<Action, Action<Exception>> WriteFile(string file);

        /// <summary>
        /// Returns an observable which, upon subscription, begins an asynchronous read
        /// operation. When the operation completes, the observable yields the number of
        /// bytes read and completes.
        /// </summary>
        Action<Action<int>, Action<Exception>> Read(byte[] buffer, int offset, int count);
    }

}
