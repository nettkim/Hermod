﻿/*
 * Copyright (c) 2010-2019, Achim 'ahzf' Friedland <achim.friedland@graphdefined.com>
 * This file is part of Vanaheimr Hermod <http://www.github.com/Vanaheimr/Hermod>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace org.GraphDefined.Vanaheimr.Hermod.HTTP
{

    #region (class) HTTPRequestLogEvent

    /// <summary>
    /// An async event notifying about HTTP requests.
    /// </summary>
    public class HTTPRequestLogEvent
    {

        #region Data

        private readonly List<HTTPRequestLogHandler> subscribers;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new async event notifying about incoming HTTP requests.
        /// </summary>
        public HTTPRequestLogEvent()
        {
            subscribers = new List<HTTPRequestLogHandler>();
        }

        #endregion


        #region + / Add

        public static HTTPRequestLogEvent operator + (HTTPRequestLogEvent e, HTTPRequestLogHandler callback)
        {

            if (callback == null)
                throw new NullReferenceException("callback is null");

            if (e == null)
                e = new HTTPRequestLogEvent();

            lock (e.subscribers)
            {
                e.subscribers.Add(callback);
            }

            return e;

        }

        public HTTPRequestLogEvent Add(HTTPRequestLogHandler callback)
        {

            if (callback == null)
                throw new NullReferenceException("callback is null");

            lock (subscribers)
            {
                subscribers.Add(callback);
            }

            return this;

        }

        #endregion

        #region - / Remove

        public static HTTPRequestLogEvent operator - (HTTPRequestLogEvent e, HTTPRequestLogHandler callback)
        {

            if (callback == null)
                throw new NullReferenceException("callback is null");

            if (e == null)
                return null;

            lock (e.subscribers)
            {
                e.subscribers.Remove(callback);
            }

            return e;

        }

        public HTTPRequestLogEvent Remove(HTTPRequestLogHandler callback)
        {

            if (callback == null)
                throw new NullReferenceException("callback is null");

            lock (subscribers)
            {
                subscribers.Remove(callback);
            }

            return this;

        }

        #endregion


        #region InvokeAsync(ServerTimestamp, HTTPAPI, Request)

        /// <summary>
        /// Call all subscribers sequentially.
        /// </summary>
        /// <param name="ServerTimestamp">The timestamp of the event.</param>
        /// <param name="HTTPAPI">The sending HTTP API.</param>
        /// <param name="Request">The HTTP request.</param>
        public async Task InvokeAsync(DateTime     ServerTimestamp,
                                      HTTPAPI      HTTPAPI,
                                      HTTPRequest  Request)
        {

            HTTPRequestLogHandler[] _invocationList;

            lock (subscribers)
            {
                _invocationList = subscribers.ToArray();
            }

            foreach (var callback in _invocationList)
                await callback(ServerTimestamp, HTTPAPI, Request).ConfigureAwait(false);

        }

        #endregion

        #region WhenAny    (ServerTimestamp, HTTPAPI, Request,               Timeout = null)

        /// <summary>
        /// Call all subscribers in parallel and wait for any to complete.
        /// </summary>
        /// <param name="ServerTimestamp">The timestamp of the event.</param>
        /// <param name="HTTPAPI">The sending HTTP API.</param>
        /// <param name="Request">The HTTP request.</param>
        /// <param name="Timeout">A timeout for this operation.</param>
        public Task WhenAny(DateTime      ServerTimestamp,
                            HTTPAPI       HTTPAPI,
                            HTTPRequest   Request,
                            TimeSpan?     Timeout = null)
        {

            List<Task> _invocationList;

            lock (subscribers)
            {

                _invocationList = subscribers.
                                        Select(callback => callback(ServerTimestamp, HTTPAPI, Request)).
                                        ToList();

                if (Timeout.HasValue)
                    _invocationList.Add(Task.Delay(Timeout.Value));

            }

            return Task.WhenAny(_invocationList);

        }

        #endregion

        #region WhenFirst  (ServerTimestamp, HTTPAPI, Request, VerifyResult, Timeout = null, DefaultResult = null)

        /// <summary>
        /// Call all subscribers in parallel and wait for all to complete.
        /// </summary>
        /// <typeparam name="T">The type of the results.</typeparam>
        /// <param name="ServerTimestamp">The timestamp of the event.</param>
        /// <param name="HTTPAPI">The sending HTTP API.</param>
        /// <param name="Request">The HTTP request.</param>
        /// <param name="VerifyResult">A delegate to verify and filter results.</param>
        /// <param name="Timeout">A timeout for this operation.</param>
        /// <param name="DefaultResult">A default result in case of errors or a timeout.</param>
        public Task<T> WhenFirst<T>(DateTime           ServerTimestamp,
                                    HTTPAPI            HTTPAPI,
                                    HTTPRequest        Request,
                                    Func<T, Boolean>   VerifyResult,
                                    TimeSpan?          Timeout        = null,
                                    Func<TimeSpan, T>  DefaultResult  = null)
        {

            #region Data

            List<Task>  _invocationList;
            Task        WorkDone;
            Task<T>     Result;
            DateTime    StartTime    = DateTime.UtcNow;
            Task        TimeoutTask  = null;

            #endregion

            lock (subscribers)
            {

                _invocationList = subscribers.
                                        Select(callback => callback(ServerTimestamp, HTTPAPI, Request)).
                                        ToList();

                if (Timeout.HasValue)
                    _invocationList.Add(TimeoutTask = Task.Run(() => System.Threading.Thread.Sleep(Timeout.Value)));

            }

            do
            {

                try
                {

                    WorkDone = Task.WhenAny(_invocationList);

                    _invocationList.Remove(WorkDone);

                    if (WorkDone != TimeoutTask)
                    {

                        Result = WorkDone as Task<T>;

                        if (Result != null &&
                            !EqualityComparer<T>.Default.Equals(Result.Result, default(T)) &&
                            VerifyResult(Result.Result))
                        {
                            return Result;
                        }

                    }

                }
                catch (Exception e)
                {
                    DebugX.LogT(e.Message);
                    WorkDone = null;
                }

            }
            while (!(WorkDone == TimeoutTask || _invocationList.Count == 0));

            return Task.FromResult(DefaultResult(DateTime.UtcNow - StartTime));

        }

        #endregion

        #region WhenAll    (ServerTimestamp, HTTPAPI, Request)

        /// <summary>
        /// Call all subscribers in parallel and wait for all to complete.
        /// </summary>
        /// <param name="ServerTimestamp">The timestamp of the event.</param>
        /// <param name="HTTPAPI">The sending HTTP API.</param>
        /// <param name="Request">The HTTP request.</param>
        public Task WhenAll(DateTime      ServerTimestamp,
                            HTTPAPI       HTTPAPI,
                            HTTPRequest   Request)
        {

            Task[] _invocationList;

            lock (subscribers)
            {
                _invocationList = subscribers.
                                        Select(callback => callback(ServerTimestamp, HTTPAPI, Request)).
                                        ToArray();
            }

            return Task.WhenAll(_invocationList);

        }

        #endregion

    }

    #endregion

    #region (class) HTTPResponseLogEvent

    /// <summary>
    /// An async event notifying about HTTP responses.
    /// </summary>
    public class HTTPResponseLogEvent
    {

        #region Data

        private readonly List<HTTPResponseLogHandler> subscribers;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new async event notifying about HTTP responses.
        /// </summary>
        public HTTPResponseLogEvent()
        {
            subscribers = new List<HTTPResponseLogHandler>();
        }

        #endregion


        #region + / Add

        public static HTTPResponseLogEvent operator + (HTTPResponseLogEvent e, HTTPResponseLogHandler callback)
        {

            if (callback == null)
                throw new NullReferenceException("callback is null");

            if (e == null)
                e = new HTTPResponseLogEvent();

            lock (e.subscribers)
            {
                e.subscribers.Add((timestamp, api, request, response) => callback(timestamp, api, request, response));
            }

            return e;

        }

        public HTTPResponseLogEvent Add(HTTPResponseLogHandler callback)
        {

            if (callback == null)
                throw new NullReferenceException("callback is null");

            lock (subscribers)
            {
                subscribers.Add(callback);
            }

            return this;

        }

        #endregion

        #region - / Remove

        public static HTTPResponseLogEvent operator - (HTTPResponseLogEvent e, HTTPResponseLogHandler callback)
        {

            if (callback == null)
                throw new NullReferenceException("callback is null");

            if (e == null)
                return null;

            lock (e.subscribers)
            {
                e.subscribers.Remove(callback);
            }

            return e;

        }

        public HTTPResponseLogEvent Remove(HTTPResponseLogHandler callback)
        {

            if (callback == null)
                throw new NullReferenceException("callback is null");

            lock (subscribers)
            {
                subscribers.Remove(callback);
            }

            return this;

        }

        #endregion


        #region InvokeAsync(ServerTimestamp, HTTPAPI, Request, Response)

        /// <summary>
        /// Call all subscribers sequentially.
        /// </summary>
        /// <param name="ServerTimestamp">The timestamp of the event.</param>
        /// <param name="HTTPAPI">The sending HTTP API.</param>
        /// <param name="Request">The HTTP request.</param>
        /// <param name="Response">The HTTP response.</param>
        public async Task InvokeAsync(DateTime      ServerTimestamp,
                                      HTTPAPI       HTTPAPI,
                                      HTTPRequest   Request,
                                      HTTPResponse  Response)
        {

            HTTPResponseLogHandler[] _invocationList;

            lock (subscribers)
            {
                _invocationList = subscribers.ToArray();
            }

            foreach (var callback in _invocationList)
                await callback(ServerTimestamp, HTTPAPI, Request, Response).ConfigureAwait(false);

        }

        #endregion

        #region WhenAny    (ServerTimestamp, HTTPAPI, Request, Response,               Timeout = null)

        /// <summary>
        /// Call all subscribers in parallel and wait for any to complete.
        /// </summary>
        /// <param name="ServerTimestamp">The timestamp of the event.</param>
        /// <param name="HTTPAPI">The sending HTTP API.</param>
        /// <param name="Request">The HTTP request.</param>
        /// <param name="Response">The HTTP response.</param>
        /// <param name="Timeout">A timeout for this operation.</param>
        public Task WhenAny(DateTime      ServerTimestamp,
                            HTTPAPI       HTTPAPI,
                            HTTPRequest   Request,
                            HTTPResponse  Response,
                            TimeSpan?     Timeout = null)
        {

            List<Task> _invocationList;

            lock (subscribers)
            {

                _invocationList = subscribers.
                                        Select(callback => callback(ServerTimestamp, HTTPAPI, Request, Response)).
                                        ToList();

                if (Timeout.HasValue)
                    _invocationList.Add(Task.Delay(Timeout.Value));

            }

            return Task.WhenAny(_invocationList);

        }

        #endregion

        #region WhenFirst  (ServerTimestamp, HTTPAPI, Request, Response, VerifyResult, Timeout = null, DefaultResult = null)

        /// <summary>
        /// Call all subscribers in parallel and wait for all to complete.
        /// </summary>
        /// <typeparam name="T">The type of the results.</typeparam>
        /// <param name="ServerTimestamp">The timestamp of the event.</param>
        /// <param name="HTTPAPI">The sending HTTP API.</param>
        /// <param name="Request">The HTTP request.</param>
        /// <param name="Response">The HTTP response.</param>
        /// <param name="VerifyResult">A delegate to verify and filter results.</param>
        /// <param name="Timeout">A timeout for this operation.</param>
        /// <param name="DefaultResult">A default result in case of errors or a timeout.</param>
        public Task<T> WhenFirst<T>(DateTime           ServerTimestamp,
                                    HTTPAPI            HTTPAPI,
                                    HTTPRequest        Request,
                                    HTTPResponse       Response,
                                    Func<T, Boolean>   VerifyResult,
                                    TimeSpan?          Timeout        = null,
                                    Func<TimeSpan, T>  DefaultResult  = null)
        {

            #region Data

            List<Task>  _invocationList;
            Task        WorkDone;
            Task<T>     Result;
            DateTime    StartTime    = DateTime.UtcNow;
            Task        TimeoutTask  = null;

            #endregion

            lock (subscribers)
            {

                _invocationList = subscribers.
                                        Select(callback => callback(ServerTimestamp, HTTPAPI, Request, Response)).
                                        ToList();

                if (Timeout.HasValue)
                    _invocationList.Add(TimeoutTask = Task.Run(() => System.Threading.Thread.Sleep(Timeout.Value)));

            }

            do
            {

                try
                {

                    WorkDone = Task.WhenAny(_invocationList);

                    _invocationList.Remove(WorkDone);

                    if (WorkDone != TimeoutTask)
                    {

                        Result = WorkDone as Task<T>;

                        if (Result != null &&
                            !EqualityComparer<T>.Default.Equals(Result.Result, default(T)) &&
                            VerifyResult(Result.Result))
                        {
                            return Result;
                        }

                    }

                }
                catch (Exception e)
                {
                    DebugX.LogT(e.Message);
                    WorkDone = null;
                }

            }
            while (!(WorkDone == TimeoutTask || _invocationList.Count == 0));

            return Task.FromResult(DefaultResult(DateTime.UtcNow - StartTime));

        }

        #endregion

        #region WhenAll    (ServerTimestamp, HTTPAPI, Request, Response)

        /// <summary>
        /// Call all subscribers in parallel and wait for all to complete.
        /// </summary>
        /// <param name="ServerTimestamp">The timestamp of the event.</param>
        /// <param name="HTTPAPI">The sending HTTP API.</param>
        /// <param name="Request">The HTTP request.</param>
        /// <param name="Response">The HTTP response.</param>
        public Task WhenAll(DateTime      ServerTimestamp,
                            HTTPAPI       HTTPAPI,
                            HTTPRequest   Request,
                            HTTPResponse  Response)
        {

            Task[] _invocationList;

            lock (subscribers)
            {
                _invocationList = subscribers.
                                        Select(callback => callback(ServerTimestamp, HTTPAPI, Request, Response)).
                                        ToArray();
            }

            return Task.WhenAll(_invocationList);

        }

        #endregion

    }

    #endregion

    #region (class) HTTPErrorLogEvent

    /// <summary>
    /// An async event notifying about HTTP errors.
    /// </summary>
    public class HTTPErrorLogEvent
    {

        #region Data

        private readonly List<HTTPErrorLogHandler> subscribers;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new async event notifying about HTTP errors.
        /// </summary>
        public HTTPErrorLogEvent()
        {
            subscribers = new List<HTTPErrorLogHandler>();
        }

        #endregion


        #region + / Add

        public static HTTPErrorLogEvent operator + (HTTPErrorLogEvent e, HTTPErrorLogHandler callback)
        {

            if (callback == null)
                throw new NullReferenceException("callback is null");

            if (e == null)
                e = new HTTPErrorLogEvent();

            lock (e.subscribers)
            {
                e.subscribers.Add(callback);
            }

            return e;

        }

        public HTTPErrorLogEvent Add(HTTPErrorLogHandler callback)
        {

            if (callback == null)
                throw new NullReferenceException("callback is null");

            lock (subscribers)
            {
                subscribers.Add(callback);
            }

            return this;

        }

        #endregion

        #region - / Remove

        public static HTTPErrorLogEvent operator - (HTTPErrorLogEvent e, HTTPErrorLogHandler callback)
        {

            if (callback == null)
                throw new NullReferenceException("callback is null");

            if (e == null)
                return null;

            lock (e.subscribers)
            {
                e.subscribers.Remove(callback);
            }

            return e;

        }

        public HTTPErrorLogEvent Remove(HTTPErrorLogHandler callback)
        {

            if (callback == null)
                throw new NullReferenceException("callback is null");

            lock (subscribers)
            {
                subscribers.Remove(callback);
            }

            return this;

        }

        #endregion


        #region InvokeAsync(ServerTimestamp, HTTPAPI, Request, Response, Error = null, LastException = null)

        /// <summary>
        /// Call all subscribers sequentially.
        /// </summary>
        /// <param name="ServerTimestamp">The timestamp of the event.</param>
        /// <param name="HTTPAPI">The sending HTTP API.</param>
        /// <param name="Request">The HTTP request.</param>
        /// <param name="Response">The HTTP response.</param>
        /// <param name="Error">An error message.</param>
        /// <param name="LastException">The last exception occured.</param>
        public async Task InvokeAsync(DateTime      ServerTimestamp,
                                      HTTPAPI       HTTPAPI,
                                      HTTPRequest   Request,
                                      HTTPResponse  Response,
                                      String        Error          = null,
                                      Exception     LastException  = null)
        {

            HTTPErrorLogHandler[] _invocationList;

            lock (subscribers)
            {
                _invocationList = subscribers.ToArray();
            }

            foreach (var callback in _invocationList)
                await callback(ServerTimestamp, HTTPAPI, Request, Response, Error, LastException).ConfigureAwait(false);

        }

        #endregion

        #region WhenAny    (ServerTimestamp, HTTPAPI, Request, Response, Error = null, LastException = null, Timeout = null)

        /// <summary>
        /// Call all subscribers in parallel and wait for any to complete.
        /// </summary>
        /// <param name="ServerTimestamp">The timestamp of the event.</param>
        /// <param name="HTTPAPI">The sending HTTP API.</param>
        /// <param name="Request">The HTTP request.</param>
        /// <param name="Response">The HTTP response.</param>
        /// <param name="Error">An error message.</param>
        /// <param name="LastException">The last exception occured.</param>
        /// <param name="Timeout">A timeout for this operation.</param>
        public Task WhenAny(DateTime      ServerTimestamp,
                            HTTPAPI       HTTPAPI,
                            HTTPRequest   Request,
                            HTTPResponse  Response,
                            String        Error          = null,
                            Exception     LastException  = null,
                            TimeSpan?     Timeout        = null)
        {

            List<Task> _invocationList;

            lock (subscribers)
            {

                _invocationList = subscribers.
                                        Select(callback => callback(ServerTimestamp, HTTPAPI, Request, Response, Error, LastException)).
                                        ToList();

                if (Timeout.HasValue)
                    _invocationList.Add(Task.Delay(Timeout.Value));

            }

            return Task.WhenAny(_invocationList);

        }

        #endregion

        #region WhenFirst  (ServerTimestamp, HTTPAPI, Request, Response, Error, LastException, VerifyResult, Timeout = null, DefaultResult = null)

        /// <summary>
        /// Call all subscribers in parallel and wait for all to complete.
        /// </summary>
        /// <typeparam name="T">The type of the results.</typeparam>
        /// <param name="ServerTimestamp">The timestamp of the event.</param>
        /// <param name="HTTPAPI">The sending HTTP API.</param>
        /// <param name="Request">The HTTP request.</param>
        /// <param name="Response">The HTTP response.</param>
        /// <param name="Error">An error message.</param>
        /// <param name="LastException">The last exception occured.</param>
        /// <param name="VerifyResult">A delegate to verify and filter results.</param>
        /// <param name="Timeout">A timeout for this operation.</param>
        /// <param name="DefaultResult">A default result in case of errors or a timeout.</param>
        public Task<T> WhenFirst<T>(DateTime           ServerTimestamp,
                                    HTTPAPI            HTTPAPI,
                                    HTTPRequest        Request,
                                    HTTPResponse       Response,
                                    String             Error,
                                    Exception          LastException,
                                    Func<T, Boolean>   VerifyResult,
                                    TimeSpan?          Timeout        = null,
                                    Func<TimeSpan, T>  DefaultResult  = null)
        {

            #region Data

            List<Task>  _invocationList;
            Task        WorkDone;
            Task<T>     Result;
            DateTime    StartTime    = DateTime.UtcNow;
            Task        TimeoutTask  = null;

            #endregion

            lock (subscribers)
            {

                _invocationList = subscribers.
                                        Select(callback => callback(ServerTimestamp, HTTPAPI, Request, Response, Error, LastException)).
                                        ToList();

                if (Timeout.HasValue)
                    _invocationList.Add(TimeoutTask = Task.Run(() => System.Threading.Thread.Sleep(Timeout.Value)));

            }

            do
            {

                try
                {

                    WorkDone = Task.WhenAny(_invocationList);

                    _invocationList.Remove(WorkDone);

                    if (WorkDone != TimeoutTask)
                    {

                        Result = WorkDone as Task<T>;

                        if (Result != null &&
                            !EqualityComparer<T>.Default.Equals(Result.Result, default(T)) &&
                            VerifyResult(Result.Result))
                        {
                            return Result;
                        }

                    }

                }
                catch (Exception e)
                {
                    DebugX.LogT(e.Message);
                    WorkDone = null;
                }

            }
            while (!(WorkDone == TimeoutTask || _invocationList.Count == 0));

            return Task.FromResult(DefaultResult(DateTime.UtcNow - StartTime));

        }

        #endregion

        #region WhenAll    (ServerTimestamp, HTTPAPI, Request, Response, Error = null, LastException = null)

        /// <summary>
        /// Call all subscribers in parallel and wait for all to complete.
        /// </summary>
        /// <param name="ServerTimestamp">The timestamp of the event.</param>
        /// <param name="HTTPAPI">The sending HTTP API.</param>
        /// <param name="Request">The HTTP request.</param>
        /// <param name="Response">The HTTP response.</param>
        /// <param name="Error">An error message.</param>
        /// <param name="LastException">The last exception occured.</param>
        public Task WhenAll(DateTime      ServerTimestamp,
                            HTTPAPI       HTTPAPI,
                            HTTPRequest   Request,
                            HTTPResponse  Response,
                            String        Error          = null,
                            Exception     LastException  = null)
        {

            Task[] _invocationList;

            lock (subscribers)
            {
                _invocationList = subscribers.
                                        Select(callback => callback(ServerTimestamp, HTTPAPI, Request, Response, Error, LastException)).
                                        ToArray();
            }

            return Task.WhenAll(_invocationList);

        }

        #endregion

    }

    #endregion


    /// <summary>
    /// A HTTP API.
    /// </summary>
    public class HTTPAPI
    {

        #region Data

        /// <summary>
        /// Internal non-cryptographic random number generator.
        /// </summary>
        protected static readonly  Random   _Random                 = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// The default HTTP server name.
        /// </summary>
        public  const              String   DefaultHTTPServerName   = "GraphDefined HTTP API v0.8";

        /// <summary>
        /// The default HTTP server port.
        /// </summary>
        public  static readonly    IPPort   DefaultHTTPServerPort   = IPPort.Parse(2002);

        /// <summary>
        /// The default service name.
        /// </summary>
        public  const              String   DefaultServiceName      = "GraphDefined HTTP API";

        /// <summary>
        /// Default logfile name.
        /// </summary>
        public  const              String   DefaultLogfileName      = "HTTPAPI.log";

        #endregion

        #region Properties

        /// <summary>
        /// The HTTP server of the API.
        /// </summary>
        public HTTPServer    HTTPServer     { get; }

        /// <summary>
        /// The HTTP hostname for all URIs within this API.
        /// </summary>
        public HTTPHostname  Hostname       { get; }

        /// <summary>
        /// The URI prefix of this HTTP API.
        /// </summary>
        public HTTPPath       URIPrefix      { get; }

        /// <summary>
        /// The name of the Open Data API service.
        /// </summary>
        public String        ServiceName    { get; }

        /// <summary>
        /// The unqiue identification of this system instance.
        /// </summary>
        public System_Id     SystemId       { get; }

        #endregion

        #region Events

        /// <summary>
        /// An event called whenever a HTTP request came in.
        /// </summary>
        public HTTPRequestLogEvent   RequestLog    = new HTTPRequestLogEvent();

        /// <summary>
        /// An event called whenever a HTTP request could successfully be processed.
        /// </summary>
        public HTTPResponseLogEvent  ResponseLog   = new HTTPResponseLogEvent();

        /// <summary>
        /// An event called whenever a HTTP request resulted in an error.
        /// </summary>
        public HTTPErrorLogEvent     ErrorLog      = new HTTPErrorLogEvent();

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new HTTP API.
        /// </summary>
        /// <param name="HTTPServer">A HTTP server.</param>
        /// <param name="HTTPHostname">A HTTP hostname.</param>
        /// <param name="URIPrefix">An URI prefix.</param>
        /// <param name="ServiceName">A service name.</param>
        public HTTPAPI(HTTPServer     HTTPServer,
                       HTTPHostname?  HTTPHostname   = null,
                       HTTPPath?       URIPrefix      = null,
                       String         ServiceName    = DefaultServiceName)

        {

            this.HTTPServer                   = HTTPServer   ?? throw new ArgumentNullException(nameof(HTTPServer), "HTTPServer!");
            this.Hostname                     = HTTPHostname ?? HTTP.HTTPHostname.Any;
            this.URIPrefix                    = URIPrefix    ?? HTTPPath.Parse("/");

            this.ServiceName                  = ServiceName.IsNotNullOrEmpty() ? ServiceName  : "HTTPAPI";

            this.SystemId                     = System_Id.Parse(Environment.MachineName.Replace("/", "") + "/" + HTTPServer.DefaultHTTPServerPort);

            // Link HTTP events...
            HTTPServer.RequestLog   += (HTTPProcessor, ServerTimestamp, Request)                                 => RequestLog. WhenAll(HTTPProcessor, ServerTimestamp, Request);
            HTTPServer.ResponseLog  += (HTTPProcessor, ServerTimestamp, Request, Response)                       => ResponseLog.WhenAll(HTTPProcessor, ServerTimestamp, Request, Response);
            HTTPServer.ErrorLog     += (HTTPProcessor, ServerTimestamp, Request, Response, Error, LastException) => ErrorLog.   WhenAll(HTTPProcessor, ServerTimestamp, Request, Response, Error, LastException);

        }

        #endregion



        #region HTTP Server Sent Events

        #region AddEventSource(EventIdentification, MaxNumberOfCachedEvents = 500, RetryIntervall = null, LogfileName = null)

        /// <summary>
        /// Add a HTTP Sever Sent Events source.
        /// </summary>
        /// <param name="EventIdentification">The unique identification of the event source.</param>
        /// <param name="MaxNumberOfCachedEvents">Maximum number of cached events.</param>
        /// <param name="RetryIntervall">The retry intervall.</param>
        /// <param name="LogfileName">A delegate to create a filename for storing and reloading events.</param>
        public HTTPEventSource<THelper> AddEventSource<THelper>(HTTPEventSource_Id              EventIdentification,
                                                                UInt32                          MaxNumberOfCachedEvents      = 500,
                                                                TimeSpan?                       RetryIntervall               = null,
                                                                Func<String[], THelper>         CreateHelper                 = null,
                                                                Boolean                         EnableLogging                = true,
                                                                String                          LogfilePrefix                = null,
                                                                Func<String, DateTime, String>  LogfileName                  = null,
                                                                String                          LogfileReloadSearchPattern   = null)

            => HTTPServer.AddEventSource(EventIdentification,
                                         MaxNumberOfCachedEvents,
                                         RetryIntervall,
                                         CreateHelper,
                                         EnableLogging,
                                         LogfilePrefix,
                                         LogfileName,
                                         LogfileReloadSearchPattern);

        #endregion

        #region AddEventSource(EventIdentification, URITemplate, MaxNumberOfCachedEvents = 500, RetryIntervall = null, LogfileName = null, ...)

        /// <summary>
        /// Add a HTTP Sever Sent Events source and a method call back for the given URI template.
        /// </summary>
        /// <param name="EventIdentification">The unique identification of the event source.</param>
        /// <param name="URITemplate">The URI template.</param>
        /// 
        /// <param name="MaxNumberOfCachedEvents">Maximum number of cached events.</param>
        /// <param name="IncludeFilterAtRuntime">Include this events within the HTTP SSE output. Can e.g. be used to filter events by HTTP users.</param>
        /// <param name="RetryIntervall">The retry intervall.</param>
        /// <param name="LogfileName">A delegate to create a filename for storing and reloading events.</param>
        /// <param name="LogfileReloadSearchPattern">The logfile search pattern for reloading events.</param>
        /// 
        /// <param name="Hostname">The HTTP host.</param>
        /// <param name="HttpMethod">The HTTP method.</param>
        /// <param name="HTTPContentType">The HTTP content type.</param>
        /// 
        /// <param name="URIAuthentication">Whether this method needs explicit uri authentication or not.</param>
        /// <param name="HTTPMethodAuthentication">Whether this method needs explicit HTTP method authentication or not.</param>
        /// 
        /// <param name="DefaultErrorHandler">The default error handler.</param>
        public HTTPEventSource<THelper> AddEventSource<THelper>(HTTPEventSource_Id              EventIdentification,
                                                                HTTPPath                         URITemplate,

                                                                UInt32                          MaxNumberOfCachedEvents      = 500,
                                                                Func<HTTPEvent, Boolean>        IncludeFilterAtRuntime       = null,
                                                                TimeSpan?                       RetryIntervall               = null,
                                                                Func<String[], THelper>         CreateHelper                 = null,
                                                                Boolean                         EnableLogging                = true,
                                                                String                          LogfilePrefix                = null,
                                                                Func<String, DateTime, String>  LogfileName                  = null,
                                                                String                          LogfileReloadSearchPattern   = null,

                                                                HTTPHostname?                   Hostname                     = null,
                                                                HTTPMethod?                     HttpMethod                   = null,
                                                                HTTPContentType                 HTTPContentType              = null,

                                                                HTTPAuthentication              URIAuthentication            = null,
                                                                HTTPAuthentication              HTTPMethodAuthentication     = null,

                                                                HTTPDelegate                    DefaultErrorHandler          = null)

            => HTTPServer.AddEventSource(EventIdentification,
                                         URITemplate,

                                         MaxNumberOfCachedEvents,
                                         IncludeFilterAtRuntime,
                                         RetryIntervall,
                                         CreateHelper,
                                         EnableLogging,
                                         LogfilePrefix,
                                         LogfileName,
                                         LogfileReloadSearchPattern,

                                         Hostname,
                                         HttpMethod,
                                         HTTPContentType,

                                         URIAuthentication,
                                         HTTPMethodAuthentication,

                                         DefaultErrorHandler);

        #endregion


        #region Get   (EventSourceIdentification)

        /// <summary>
        /// Return the event source identified by the given event source identification.
        /// </summary>
        /// <param name="EventSourceIdentification">A string to identify an event source.</param>
        public IHTTPEventSource Get<THelper>(HTTPEventSource_Id EventSourceIdentification)

            => HTTPServer.Get(EventSourceIdentification);

        #endregion

        #region TryGet(EventSourceIdentification, out EventSource)

        /// <summary>
        /// Return the event source identified by the given event source identification.
        /// </summary>
        /// <param name="EventSourceIdentification">A string to identify an event source.</param>
        /// <param name="EventSource">The event source.</param>
        public Boolean TryGet(HTTPEventSource_Id EventSourceIdentification, out IHTTPEventSource EventSource)

            => HTTPServer.TryGet(EventSourceIdentification, out EventSource);

        #endregion

        #region EventSources(IncludeEventSource = null)

        /// <summary>
        /// Return a filtered enumeration of all event sources.
        /// </summary>
        /// <param name="IncludeEventSource">An event source filter delegate.</param>
        public IEnumerable<IHTTPEventSource> EventSources(Func<IHTTPEventSource, Boolean> IncludeEventSource = null)

            => HTTPServer.EventSources(IncludeEventSource);

        #endregion

        #endregion



        #region Start()

        public void Start()
        {

            lock (HTTPServer)
            {

                if (!HTTPServer.IsStarted)
                    HTTPServer.Start();

                //SendStarted(this, DateTime.UtcNow);

            }

        }

        #endregion

        #region Shutdown(Message = null, Wait = true)

        public void Shutdown(String Message = null, Boolean Wait = true)
        {

            lock (HTTPServer)
            {

                HTTPServer.Shutdown(Message, Wait);
                //SendCompleted(this, DateTime.UtcNow, Message);

            }

        }

        #endregion

        #region Dispose()

        public void Dispose()
        {

            lock (HTTPServer)
            {
                HTTPServer.Dispose();
            }

        }

        #endregion

    }

}
