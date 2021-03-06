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
using System.Xml.Linq;
using System.Threading;
using System.Net.Security;
using System.Threading.Tasks;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json.Linq;

#endregion

namespace org.GraphDefined.Vanaheimr.Hermod.JSON
{

    public delegate T CustomJSONParserDelegate<T>(JObject JSON, T Data);

    public delegate JObject CustomJSONSerializerDelegate<T>(T ResponseBuilder, JObject JSON);

    /// <summary>
    /// A specialized HTTP client for JSON transport.
    /// </summary>
    public class JSONClient : HTTPClient
    {

        #region Data

        /// <summary>
        /// The default HTTP/JSON user agent.
        /// </summary>
        public new const String DefaultUserAgent  = "GraphDefined HTTP/JSON Client";

        #endregion

        #region Properties

        /// <summary>
        /// The URI-prefix of the HTTP/JSON service.
        /// </summary>
        public HTTPPath  URIPrefix   { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new specialized HTTP client for the JavaScript Object Notation (JSON).
        /// </summary>
        /// <param name="Hostname">The hostname of the remote HTTP/JSON service.</param>
        /// <param name="VirtualHostname">The HTTP virtual host to use.</param>
        /// <param name="URIPrefix">The URI-prefix of the HTTP/JSON service.</param>
        /// <param name="HTTPSPort">The HTTP port of the remote HTTP/JSON service.</param>
        /// <param name="RemoteCertificateValidator">A delegate to verify the remote TLS certificate.</param>
        /// <param name="ClientCertificateSelector">A delegate to select a TLS client certificate.</param>
        /// <param name="UserAgent">The HTTP user agent to use.</param>
        /// <param name="RequestTimeout">An optional default HTTP request timeout.</param>
        /// <param name="DNSClient">An optional DNS client.</param>
        public JSONClient(HTTPHostname                         Hostname,
                          HTTPPath                              URIPrefix,
                          HTTPHostname?                        VirtualHostname              = null,
                          IPPort?                              HTTPSPort                    = null,
                          RemoteCertificateValidationCallback  RemoteCertificateValidator   = null,
                          LocalCertificateSelectionCallback    ClientCertificateSelector    = null,
                          String                               UserAgent                    = DefaultUserAgent,
                          TimeSpan?                            RequestTimeout               = null,
                          DNSClient                            DNSClient                    = null)

            : base(Hostname,
                   HTTPSPort      ?? IPPort.HTTPS,
                   VirtualHostname,
                   RemoteCertificateValidator,
                   ClientCertificateSelector,
                   null,
                   UserAgent      ?? DefaultUserAgent,
                   RequestTimeout ?? DefaultRequestTimeout,
                   DNSClient)

        {

            this.URIPrefix = URIPrefix.IsNotNullOrEmpty() ? URIPrefix : HTTPPath.Parse("/");

        }

        #endregion


        #region Query(JSONRequest, OnSuccess, OnJSONFault, OnHTTPError, OnException, RequestTimeout = null)

        /// <summary>
        /// Create a new JSON request task.
        /// </summary>
        /// <typeparam name="T">The type of the return data structure.</typeparam>
        /// <param name="JSONRequest">The JSON request.</param>
        /// <param name="OnSuccess">The delegate to call for every successful result.</param>
        /// <param name="OnJSONFault">The delegate to call whenever a JSON fault occured.</param>
        /// <param name="OnHTTPError">The delegate to call whenever a HTTP error occured.</param>
        /// <param name="OnException">The delegate to call whenever an exception occured.</param>
        /// <param name="RequestTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        /// <returns>The data structured after it had been processed by the OnSuccess delegate, or a fault.</returns>
        public async Task<HTTPResponse<T>>

            Query<T>(JObject                                                         JSONRequest,
                     Func<HTTPResponse<JObject>,                   HTTPResponse<T>>  OnSuccess,
                     Func<DateTime, Object, HTTPResponse<JObject>, HTTPResponse<T>>  OnJSONFault,
                     Func<DateTime, Object, HTTPResponse,          HTTPResponse<T>>  OnHTTPError,
                     Func<DateTime, Object, Exception,             HTTPResponse<T>>  OnException,
                     Action<HTTPRequest.Builder>                                     HTTPRequestBuilder    = null,
                     ClientRequestLogHandler                                         RequestLogDelegate    = null,
                     ClientResponseLogHandler                                        ResponseLogDelegate   = null,

                     CancellationToken?                                              CancellationToken     = null,
                     EventTracking_Id                                                EventTrackingId       = null,
                     TimeSpan?                                                       RequestTimeout        = null,
                     Byte                                                            NumberOfRetry         = 0)

        {

            #region Initial checks

            if (JSONRequest == null)
                throw new ArgumentNullException(nameof(JSONRequest),  "The JSON request must not be null!");

            if (OnSuccess   == null)
                throw new ArgumentNullException(nameof(OnSuccess),    "The 'OnSuccess'-delegate must not be null!");

            if (OnJSONFault == null)
                throw new ArgumentNullException(nameof(OnJSONFault),  "The 'OnJSONFault'-delegate must not be null!");

            if (OnHTTPError == null)
                throw new ArgumentNullException(nameof(OnHTTPError),  "The 'OnHTTPError'-delegate must not be null!");

            if (OnException == null)
                throw new ArgumentNullException(nameof(OnException),  "The 'OnException'-delegate must not be null!");

            #endregion

            var _RequestBuilder = this.POST(URIPrefix);
            _RequestBuilder.Host               = VirtualHostname ?? Hostname;
            _RequestBuilder.Content            = JSONRequest.ToUTF8Bytes();
            _RequestBuilder.ContentType        = HTTPContentType.JSON_UTF8;
            _RequestBuilder.UserAgent          = UserAgent;
            _RequestBuilder.FakeURIPrefix      = "https://" + (VirtualHostname ?? Hostname);

            HTTPRequestBuilder?.Invoke(_RequestBuilder);

            var HttpResponse = await Execute(_RequestBuilder,
                                             RequestLogDelegate,
                                             ResponseLogDelegate,
                                             CancellationToken.HasValue  ? CancellationToken.Value : new CancellationTokenSource().Token,
                                             EventTrackingId,
                                             RequestTimeout ?? DefaultRequestTimeout,
                                             NumberOfRetry);


            if (HttpResponse                 != null              &&
                HttpResponse.HTTPStatusCode  == HTTPStatusCode.OK &&
                HttpResponse.HTTPBody        != null              &&
                HttpResponse.HTTPBody.Length > 0)
            {

                try
                {

            //        var OnHTTPErrorLocal = OnHTTPError;
            //    if (OnHTTPErrorLocal != null)
            //        return OnHTTPErrorLocal(DateTime.UtcNow, this, HttpResponseTask?.Result);

            //    return new HTTPResponse<JObject>(HttpResponseTask?.Result,
            //                                     new JObject(new JProperty("HTTPError", "")),
            //                                     IsFault: true) as HTTPResponse<T>;

            //}

            //try
            //{

                    var JSON = JObject.Parse(HttpResponse.HTTPBody.ToUTF8String());

                    var OnSuccessLocal = OnSuccess;
                    if (OnSuccessLocal != null)
                        return OnSuccessLocal(new HTTPResponse<JObject>(HttpResponse, JSON));

                    //var OnSOAPFaultLocal = OnSOAPFault;
                    //if (OnSOAPFaultLocal != null)
                    //    return OnSOAPFaultLocal(DateTime.UtcNow, this, new HTTPResponse<XElement>(HttpResponseTask.Result, SOAPXML));

                    return new HTTPResponse<JObject>(HttpResponse,
                                                     new JObject(new JProperty("fault", "")),
                                                     IsFault: true) as HTTPResponse<T>;


                } catch (Exception e)
                {

                    OnException?.Invoke(DateTime.UtcNow, this, e);

                    //var OnFaultLocal = OnSOAPFault;
                    //if (OnFaultLocal != null)
                    //    return OnFaultLocal(new HTTPResponse<XElement>(HttpResponseTask.Result, e));

                    return new HTTPResponse<JObject>(HttpResponse,
                                                     new JObject(new JProperty("exception", e.Message)),
                                                     IsFault: true) as HTTPResponse<T>;

                }

            }

            else
            {

                DebugX.LogT("HTTPRepose is null! (" + _RequestBuilder.URI + ")");

                var OnHTTPErrorLocal = OnHTTPError;
                if (OnHTTPErrorLocal != null)
                    return OnHTTPErrorLocal(DateTime.UtcNow, this, HttpResponse);

                return new HTTPResponse<JObject>(HttpResponse,
                                                 new JObject(
                                                     new JProperty("HTTPError", true)
                                                 ),
                                                 IsFault: true) as HTTPResponse<T>;

            }

        }

        #endregion

    }

}
