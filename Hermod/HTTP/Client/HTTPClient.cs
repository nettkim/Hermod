﻿/*
 * Copyright (c) 2010-2016, Achim 'ahzf' Friedland <achim.friedland@graphdefined.com>
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
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using Newtonsoft.Json.Linq;

#endregion

namespace org.GraphDefined.Vanaheimr.Hermod.HTTP
{


    public static class Helpers
    {

        public static Int32 ReadTEBlockLength(this Byte[] TEContent, Int32 Position, Int32 TELength)
        {

            var TEBlockLength = new Byte[TELength];
            Array.Copy(TEContent, Position, TEBlockLength, 0, TELength);

            return Convert.ToInt32(TEBlockLength.ToUTF8String(), // Hex-String
                                   16);

        }


    }


    /// <summary>
    /// A http client.
    /// </summary>
    public class HTTPClient : IDisposable
    {

        #region Data

        private         Socket          TCPSocket;
        private         NetworkStream   TCPStream;
        private         SslStream       TLSStream;
        private         Stream          HTTPStream;

        private static  Regex           IPv4AddressRegExpr     = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");

        /// <summary>
        /// The default HTTP/TCP Port.
        /// </summary>
        public  static  IPPort          DefaultHTTPPort         = IPPort.Parse(80);

        /// <summary>
        /// The default HTTPS user agent.
        /// </summary>
        public  const   String          DefaultUserAgent       = "Vanaheimr Hermod HTTP Client v0.1";

        /// <summary>
        /// The default HTTP user agent.
        /// </summary>
        public  static  TimeSpan        DefaultRequestTimeout  = TimeSpan.FromSeconds(60);

        #endregion

        #region Properties

        /// <summary>
        /// The Hostname to which the HTTPClient connects.
        /// </summary>
        public String           Hostname            { get; }

        /// <summary>
        /// The IP Address to connect to.
        /// </summary>
        public IIPAddress       RemoteIPAddress     { get; private set; }

        /// <summary>
        /// The IP port to connect to.
        /// </summary>
        public IPPort           RemotePort          { get; }

        /// <summary>
        /// The IP socket to connect to.
        /// </summary>
        public IPSocket         RemoteSocket
            => new IPSocket(RemoteIPAddress, RemotePort);

        /// <summary>
        /// The default server name.
        /// </summary>
        public String           UserAgent           { get; }

        /// <summary>
        /// The default server name.
        /// </summary>
        public DNSClient        DNSClient           { get; }

        //      public X509Certificate2 ServerCert { get; set; }

        /// <summary>
        /// A delegate to verify the remote TLS certificate.
        /// </summary>
        public RemoteCertificateValidationCallback RemoteCertificateValidator { get; }

        public LocalCertificateSelectionCallback LocalCertificateSelector { get; }

        public X509Certificate ClientCert           { get; }

        //    public LocalCertificateSelectionCallback ClientCertificateSelector { get; set; }

        public TimeSpan        RequestTimeout       {get; }






        public Int32 Available
            => TCPSocket.Available;

        public Boolean Connected
            => TCPSocket.Connected;

        public LingerOption LingerState {
            get
            {
                return TCPSocket.LingerState;
            }
            set
            {
                TCPSocket.LingerState = value;
            }
        }

        public Boolean NoDelay
        {
            get
            {
                return TCPSocket.NoDelay;
            }
            set
            {
                TCPSocket.NoDelay = value;
            }
        }

        public Byte TTL
        {
            get
            {
                return (Byte) TCPSocket.Ttl;
            }
            set
            {
                TCPSocket.Ttl = value;
            }
        }


        #endregion

        #region Events

        public delegate Task OnDataReadDelegate(TimeSpan Timestamp, UInt64 BytesRead, UInt64? BytesExpected = null);

        public event OnDataReadDelegate OnDataRead;

        #endregion

        #region Constructor(s)

        #region HTTPClient(RemoteIPAddress, ...)

        /// <summary>
        /// Create a new HTTP client using the given optional parameters.
        /// </summary>
        /// <param name="RemoteIPAddress">The remote IP address to connect to.</param>
        /// <param name="RemotePort">The remote IP port to connect to.</param>
        /// <param name="RemoteCertificateValidator">A delegate to verify the remote TLS certificate.</param>
        /// <param name="LocalCertificateSelector">Selects the local certificate used for authentication.</param>
        /// <param name="ClientCert">The TLS client certificate to use.</param>
        /// <param name="UserAgent">The HTTP user agent to use.</param>
        /// <param name="RequestTimeout">An optional default HTTP request timeout.</param>
        /// <param name="DNSClient">An optional DNS client.</param>
        public HTTPClient(IIPAddress                           RemoteIPAddress,
                          IPPort                               RemotePort                   = null,
                          RemoteCertificateValidationCallback  RemoteCertificateValidator   = null,
                          LocalCertificateSelectionCallback    LocalCertificateSelector     = null,
                          X509Certificate                      ClientCert                   = null,
                          String                               UserAgent                    = DefaultUserAgent,
                          TimeSpan?                            RequestTimeout               = null,
                          DNSClient                            DNSClient                    = null)
        {

            this.RemoteIPAddress             = RemoteIPAddress;
            this.Hostname                    = RemoteIPAddress.ToString();
            this.RemotePort                  = RemotePort     ?? DefaultHTTPPort;
            this.RemoteCertificateValidator  = RemoteCertificateValidator;
            this.LocalCertificateSelector    = LocalCertificateSelector;
            this.ClientCert                  = ClientCert;
            this.UserAgent                   = UserAgent      ?? DefaultUserAgent;
            this.RequestTimeout              = RequestTimeout ?? DefaultRequestTimeout;
            this.DNSClient                   = DNSClient      ?? new DNSClient();

        }

        #endregion

        #region HTTPClient(Socket, ...)

        /// <summary>
        /// Create a new HTTP client using the given optional parameters.
        /// </summary>
        /// <param name="RemoteSocket">The remote IP socket to connect to.</param>
        /// <param name="RemoteCertificateValidator">A delegate to verify the remote TLS certificate.</param>
        /// <param name="LocalCertificateSelector">Selects the local certificate used for authentication.</param>
        /// <param name="ClientCert">The TLS client certificate to use.</param>
        /// <param name="UserAgent">The HTTP user agent to use.</param>
        /// <param name="RequestTimeout">An optional default HTTP request timeout.</param>
        /// <param name="DNSClient">An optional DNS client.</param>
        public HTTPClient(IPSocket                             RemoteSocket,
                          RemoteCertificateValidationCallback  RemoteCertificateValidator   = null,
                          LocalCertificateSelectionCallback    LocalCertificateSelector     = null,
                          X509Certificate                      ClientCert                   = null,
                          String                               UserAgent                    = DefaultUserAgent,
                          TimeSpan?                            RequestTimeout               = null,
                          DNSClient                            DNSClient                    = null)

            : this(RemoteSocket.IPAddress,
                   RemoteSocket.Port,
                   RemoteCertificateValidator,
                   LocalCertificateSelector,
                   ClientCert,
                   UserAgent      ?? DefaultUserAgent,
                   RequestTimeout ?? DefaultRequestTimeout,
                   DNSClient      ?? new DNSClient())

        { }

        #endregion

        #region HTTPClient(RemoteHost, ...)

        /// <summary>
        /// Create a new HTTP client using the given optional parameters.
        /// </summary>
        /// <param name="RemoteHost">The remote hostname to connect to.</param>
        /// <param name="RemotePort">The remote IP port to connect to.</param>
        /// <param name="RemoteCertificateValidator">A delegate to verify the remote TLS certificate.</param>
        /// <param name="LocalCertificateSelector">Selects the local certificate used for authentication.</param>
        /// <param name="ClientCert">The TLS client certificate to use.</param>
        /// <param name="UserAgent">The HTTP user agent to use.</param>
        /// <param name="RequestTimeout">An optional default HTTP request timeout.</param>
        /// <param name="DNSClient">An optional DNS client.</param>
        public HTTPClient(String                               RemoteHost,
                          IPPort                               RemotePort                   = null,
                          RemoteCertificateValidationCallback  RemoteCertificateValidator   = null,
                          LocalCertificateSelectionCallback    LocalCertificateSelector     = null,
                          X509Certificate                      ClientCert                   = null,
                          String                               UserAgent                    = DefaultUserAgent,
                          TimeSpan?                            RequestTimeout               = null,
                          DNSClient                            DNSClient                    = null)
        {

            this.Hostname                    = RemoteHost;
            this.RemotePort                  = RemotePort     ?? DefaultHTTPPort;
            this.RemoteCertificateValidator  = RemoteCertificateValidator;
            this.LocalCertificateSelector    = LocalCertificateSelector;
            this.ClientCert                  = ClientCert;
            this.UserAgent                   = UserAgent      ?? DefaultUserAgent;
            this.RequestTimeout              = RequestTimeout ?? DefaultRequestTimeout;
            this.DNSClient                   = DNSClient      ?? new DNSClient();

        }

        #endregion

        #endregion


        #region CreateRequest(HTTPMethod, URI, BuilderAction = null)

        /// <summary>
        /// Create a new HTTP request.
        /// </summary>
        /// <param name="HTTPMethod">A HTTP method.</param>
        /// <param name="URI">An URI.</param>
        /// <param name="BuilderAction">A delegate to configure the new HTTP request builder.</param>
        /// <returns>A new HTTPRequest object.</returns>
        public HTTPRequestBuilder CreateRequest(HTTPMethod                  HTTPMethod,
                                                String                      URI,
                                                Action<HTTPRequestBuilder>  BuilderAction  = null)
        {

            var Builder     = new HTTPRequestBuilder(this) {
                HTTPMethod  = HTTPMethod,
                URI         = URI
            };

            if (BuilderAction != null)
                BuilderAction?.Invoke(Builder);

            return Builder;

        }

        #endregion


        #region Execute(HTTPRequestDelegate, RequestLogDelegate = null, ResponseLogDelegate = null, Timeout = null, CancellationToken = null)

        /// <summary>
        /// Execute the given HTTP request and return its result.
        /// </summary>
        /// <param name="HTTPRequestDelegate">A delegate for producing a HTTP request for a given HTTP client.</param>
        /// <param name="RequestLogDelegate">A delegate for logging the HTTP request.</param>
        /// <param name="ResponseLogDelegate">A delegate for logging the HTTP request/response.</param>
        /// <param name="RequestTimeout">An optional HTTP request timeout.</param>
        /// <param name="CancellationToken">A cancellation token.</param>
        public Task<HTTPResponse> Execute(Func<HTTPClient, HTTPRequest>  HTTPRequestDelegate,
                                          ClientRequestLogHandler        RequestLogDelegate   = null,
                                          ClientResponseLogHandler       ResponseLogDelegate  = null,

                                          CancellationToken?             CancellationToken    = null,
                                          EventTracking_Id               EventTrackingId      = null,
                                          TimeSpan?                      RequestTimeout       = null)

        {

            #region Initial checks

            if (HTTPRequestDelegate == null)
                throw new ArgumentNullException(nameof(HTTPRequestDelegate), "The given delegate must not be null!");

            #endregion

            return Execute(HTTPRequestDelegate(this),
                           RequestLogDelegate,
                           ResponseLogDelegate,

                           CancellationToken,
                           EventTrackingId,
                           RequestTimeout);

        }

        #endregion

        #region Execute(Request, RequestLogDelegate = null, ResponseLogDelegate = null, Timeout = null, CancellationToken = null)

        /// <summary>
        /// Execute the given HTTP request and return its result.
        /// </summary>
        /// <param name="Request">A HTTP request.</param>
        /// <param name="RequestLogDelegate">A delegate for logging the HTTP request.</param>
        /// <param name="ResponseLogDelegate">A delegate for logging the HTTP request/response.</param>
        /// <param name="CancellationToken">A cancellation token.</param>
        /// <param name="RequestTimeout">An optional timeout.</param>
        public Task<HTTPResponse> Execute(HTTPRequest               Request,
                                          ClientRequestLogHandler   RequestLogDelegate   = null,
                                          ClientResponseLogHandler  ResponseLogDelegate  = null,

                                          CancellationToken?        CancellationToken    = null,
                                          EventTracking_Id          EventTrackingId      = null,
                                          TimeSpan?                 RequestTimeout       = null)

        {

            return Task.Run(async () => {

                #region Call the optional HTTP request log delegate

                try
                {

                    if (RequestLogDelegate != null)
                        await Task.WhenAll(RequestLogDelegate.GetInvocationList().
                                           Cast<ClientRequestLogHandler>().
                                           Select(e => e(DateTime.UtcNow,
                                                         this,
                                                         Request))).
                                           ConfigureAwait(false);

                }
                catch (Exception e)
                {
                    e.Log(nameof(HTTPClient) + "." + nameof(RequestLogDelegate));
                }

                #endregion

                HTTPResponse Response = null;

                try
                {

                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

                    #region Data

                    var HTTPHeaderBytes = new Byte[0];
                    var sw = new Stopwatch();

                    if (!RequestTimeout.HasValue)
                        RequestTimeout = Request.Timeout;

                    if (!RequestTimeout.HasValue)
                        RequestTimeout = TimeSpan.FromSeconds(60);

                    #endregion

                    #region Create TCP connection (possibly also do DNS lookups)

                    if (TCPSocket == null)
                    {

                        System.Net.IPEndPoint _FinalIPEndPoint = null;
                   //     IIPAddress _ResolvedRemoteIPAddress = null;

                        if (RemoteIPAddress == null)
                        {

                            if (Hostname.Trim() == "127.0.0.1" || Hostname.Trim() == "localhost")
                                RemoteIPAddress = IPv4Address.Localhost;

                            else if (Hostname.Trim() == "::1" || Hostname.Trim() == "localhost6")
                                RemoteIPAddress = IPv6Address.Localhost;

                            // Hostname is an IPv4 address...
                            else if (IPv4AddressRegExpr.IsMatch(Hostname))
                                RemoteIPAddress = IPv4Address.Parse(Hostname);

                            #region DNS lookup...

                            if (RemoteIPAddress == null)
                            {

                                try
                                {

                                    var IPv4Addresses = await DNSClient.
                                                                  Query<A>(Hostname).
                                                                      ContinueWith(QueryTask => QueryTask.Result.
                                                                                                    Select(ARecord => ARecord.IPv4Address).
                                                                                                    ToArray());

                                    var IPv6Addresses = await DNSClient.
                                                                  Query<AAAA>(Hostname).
                                                                      ContinueWith(QueryTask => QueryTask.Result.
                                                                                                    Select(AAAARecord => AAAARecord.IPv6Address).
                                                                                                    ToArray());

                                    RemoteIPAddress = IPv4Addresses.FirstOrDefault();

                                }
                                catch (Exception e)
                                {
                                    DebugX.Log("HTTP Client DNS lookup failed: " + e.Message);
                                }

                            }

                            #endregion

                        }

                        _FinalIPEndPoint = new System.Net.IPEndPoint(new System.Net.IPAddress(RemoteIPAddress.GetBytes()),
                                                                     RemotePort.ToInt32());

                        sw.Start();

                        //TCPClient = new TcpClient();
                        //TCPClient.Connect(_FinalIPEndPoint);
                        //TCPClient.ReceiveTimeout = (Int32) RequestTimeout.Value.TotalMilliseconds;


                        if (RemoteIPAddress.IsIPv4)
                            TCPSocket = new Socket(AddressFamily.InterNetwork,
                                                  SocketType.   Stream,
                                                  ProtocolType. Tcp);

                        else if (RemoteIPAddress.IsIPv6)
                            TCPSocket = new Socket(AddressFamily.InterNetworkV6,
                                                  SocketType.   Stream,
                                                  ProtocolType. Tcp);

                        TCPSocket.Connect(_FinalIPEndPoint);
                        TCPSocket.ReceiveTimeout = (Int32) RequestTimeout.Value.TotalMilliseconds;

                    }

                    #endregion

                    #region Create (Crypto-)Stream

                    TCPStream = new NetworkStream(TCPSocket, true);
                    TCPStream.ReadTimeout = (Int32) RequestTimeout.Value.TotalMilliseconds;

                    TLSStream = RemoteCertificateValidator != null

                                     ? new SslStream(TCPStream,
                                                     false,
                                                     RemoteCertificateValidator,
                                                     LocalCertificateSelector,
                                                     EncryptionPolicy.RequireEncryption)

                                     : null;

                    if (TLSStream != null)
                        TLSStream.ReadTimeout = (Int32) RequestTimeout.Value.TotalMilliseconds;

                    HTTPStream = null;

                    if (RemoteCertificateValidator != null)
                    {
                        HTTPStream = TLSStream;
                        await TLSStream.AuthenticateAsClientAsync(Hostname);//, new X509CertificateCollection(new X509Certificate[] { ClientCert }), SslProtocols.Default, true);
                    }

                    else
                        HTTPStream = TCPStream;

                    HTTPStream.ReadTimeout = (Int32) RequestTimeout.Value.TotalMilliseconds;

                    #endregion

                    #region Send Request

                    HTTPStream.Write(String.Concat(Request.EntireRequestHeader, "\r\n\r\n").
                                            ToUTF8Bytes());

                    var RequestBodyLength = Request.HTTPBody == null
                                                ? Request.ContentLength.HasValue ? (Int32)Request.ContentLength.Value : 0
                                                : Request.ContentLength.HasValue ? Math.Min((Int32)Request.ContentLength.Value, Request.HTTPBody.Length) : Request.HTTPBody.Length;

                    if (RequestBodyLength > 0)
                        HTTPStream.Write(Request.HTTPBody, 0, RequestBodyLength);

                    var _InternalHTTPStream = new MemoryStream();
                    var _Buffer = new Byte[10485760]; // 10 MBytes, a smaller value leads to read errors!

                    #endregion

                    #region Wait timeout for the server to react!

                    while (!TCPStream.DataAvailable)
                    {

                        if (sw.ElapsedMilliseconds >= RequestTimeout.Value.TotalMilliseconds)
                        {
                            DebugX.Log("Could not read from the TCP stream for " + sw.ElapsedMilliseconds.ToString() + "ms!");
                            throw new Exception("Could not read from the TCP stream for " + sw.ElapsedMilliseconds.ToString() + "ms!");
                        }

                        Thread.Sleep(1);

                    }

                    //Debug.WriteLine("[" + DateTime.Now + "] HTTPClient (" + TCPClient.Client.LocalEndPoint.ToString() + " -> " + RemoteSocket.ToString() + ") got first response after " + sw.ElapsedMilliseconds + "ms!");

                    #endregion

                    #region Read at least the entire HTTP header, and maybe some of the HTTP body...

                    var CurrentDataLength = 0;

                    do
                    {

                        #region When data available, write it to the buffer...

                        while (TCPStream.DataAvailable)
                        {

                            CurrentDataLength = HTTPStream.Read(_Buffer, 0, _Buffer.Length);

                            if (CurrentDataLength > 0)
                            {
                                _InternalHTTPStream.Write(_Buffer, 0, CurrentDataLength);
                                DebugX.Log("Read " + CurrentDataLength + " bytes from HTTP connection (" + TCPSocket.LocalEndPoint + " -> " + RemoteSocket + ") (" + sw.ElapsedMilliseconds + "ms)!");
                            }

                        }

                        #endregion

                        #region Check if the entire HTTP header was already read into the buffer

                        if (_InternalHTTPStream.Length > 4)
                        {

                            var _InternalHTTPStreamBytes = _InternalHTTPStream.ToArray();

                            for (var pos = 3; pos < _InternalHTTPStreamBytes.Length; pos++)
                            {

                                if (_InternalHTTPStreamBytes[pos] == 0x0a &&
                                    _InternalHTTPStreamBytes[pos - 1] == 0x0d &&
                                    _InternalHTTPStreamBytes[pos - 2] == 0x0a &&
                                    _InternalHTTPStreamBytes[pos - 3] == 0x0d)
                                {
                                    Array.Resize(ref HTTPHeaderBytes, pos - 3);
                                    Array.Copy(_InternalHTTPStreamBytes, 0, HTTPHeaderBytes, 0, pos - 3);
                                    break;
                                }

                            }

                            //if (HTTPHeaderBytes.Length > 0)
                            //    DebugX.Log("End of (" + TCPClient.Client.LocalEndPoint.ToString() + " -> " + RemoteSocket.ToString() + ") HTTP header at " + HTTPHeaderBytes.Length + " bytes (" + sw.ElapsedMilliseconds + "ms)!");

                        }

                        #endregion

                        Thread.Sleep(1);

                    }
                    // Note: Delayed parts of the HTTP body may not be read into the buffer
                    //       => Must be read later!
                    while (TCPStream.DataAvailable ||
                           ((sw.ElapsedMilliseconds < HTTPStream.ReadTimeout) && HTTPHeaderBytes.Length == 0));

                    var HTTPBodyStartsAt = HTTPHeaderBytes.Length + 4;

                    //DebugX.Log("Finally read " + _MemoryStream.Length + " bytes of HTTP client (" + TCPClient.Client.LocalEndPoint.ToString() + " -> " + RemoteSocket.ToString() + ") data (" + sw.ElapsedMilliseconds + "ms)!");

                    #endregion

                    #region Copy HTTP header data and create HTTP response

                    if (HTTPHeaderBytes.Length == 0)
                        throw new ApplicationException("[" + DateTime.UtcNow.ToString() + "] Could not find the end of the HTTP protocol header!");

                    Response = HTTPResponse.Parse(HTTPHeaderBytes.ToUTF8String(),
                                                  Request);

                    #endregion

                    #region A single fixed-lenght HTTP request -> read '$Content-Length' bytes...

                    // Copy only the number of bytes given within
                    // the HTTP header element 'Content-Length'!
                    if (Response.ContentLength.HasValue && Response.ContentLength.Value > 0)
                    {

                        _InternalHTTPStream.Seek(HTTPBodyStartsAt, SeekOrigin.Begin);
                        var _AlreadyRead = _InternalHTTPStream.Read(_Buffer, 0, _Buffer.Length);
                        Response.HTTPBodyStream.Write(_Buffer, 0, _AlreadyRead);
                        var _StillToRead = (Int32)Response.ContentLength.Value - _AlreadyRead;

                        do
                        {

                            while (TCPStream.DataAvailable && _StillToRead > 0)
                            {

                                _AlreadyRead = HTTPStream.Read(_Buffer, 0, Math.Min(_Buffer.Length, (Int32)_StillToRead));

                                if (_AlreadyRead > 0)
                                {
                                    Response.HTTPBodyStream.Write(_Buffer, 0, _AlreadyRead);
                                    _StillToRead -= _AlreadyRead;
                                }

                            }

                            OnDataRead?.Invoke(sw.Elapsed,
                                               Response.ContentLength.Value - (UInt64)_StillToRead,
                                               Response.ContentLength.Value);

                            if (_StillToRead <= 0)
                                break;

                            Thread.Sleep(1);

                        }
                        while (sw.ElapsedMilliseconds < HTTPStream.ReadTimeout);

                        Response.ContentStreamToArray();

                    }

                    #endregion

                    #region ...or chunked transport...

                    else if (Response.TransferEncoding == "chunked")
                    {

                        //DebugX.Log("[HTTPClient] Chunked encoding detected");

                        try
                        {

                            // Write the first buffer (without the HTTP header) to the HTTPBodyStream...
                            _InternalHTTPStream.Seek(HTTPBodyStartsAt, SeekOrigin.Begin);
                            Response.NewContentStream();
                            var _ChunkedStream = new MemoryStream();
                            _ChunkedStream.Write(_Buffer, 0, _InternalHTTPStream.Read(_Buffer, 0, _Buffer.Length));
                            var ChunkedDecodingFinished = false;
                            var ChunkedStreamLength = 0UL;

                            do
                            {

                                #region If more (new) data is available -> read it!

                                do
                                {

                                    while (TCPStream.DataAvailable)
                                    {

                                        CurrentDataLength = HTTPStream.Read(_Buffer, 0, _Buffer.Length);

                                        if (CurrentDataLength > 0)
                                            _ChunkedStream.Write(_Buffer, 0, CurrentDataLength);

                                        if (sw.ElapsedMilliseconds > HTTPStream.ReadTimeout)
                                            throw new ApplicationException("HTTPClient timeout!");

                                        Thread.Sleep(1);

                                    }

                                } while ((UInt64)_ChunkedStream.Length == ChunkedStreamLength);

                                ChunkedStreamLength = (UInt64)_ChunkedStream.Length;
                                OnDataRead?.Invoke(sw.Elapsed, ChunkedStreamLength);

                                #endregion

                                var ChunkedBytes = _ChunkedStream.ToArray();
                                var DecodedStream = new MemoryStream();
                                var IsStatus_ReadBlockLength = true;
                                var CurrentPosition = 0;
                                var LastPos = 0;
                                var NumberOfBlocks = 0;

                                do
                                {

                                    if (CurrentPosition > 2 &&
                                        IsStatus_ReadBlockLength &&
                                        ChunkedBytes[CurrentPosition - 1] == '\n' &&
                                        ChunkedBytes[CurrentPosition - 2] == '\r')
                                    {

                                        var BlockLength = ChunkedBytes.ReadTEBlockLength(LastPos,
                                                                                         CurrentPosition - LastPos - 2);

                                        //Debug.WriteLine(DateTime.Now + " Chunked encoded block of length " + BlockLength + " bytes detected");

                                        #region End of stream reached...

                                        if (BlockLength == 0)
                                        {
                                            Response.ContentStreamToArray(DecodedStream);
                                            ChunkedDecodingFinished = true;
                                            break;
                                        }

                                        #endregion

                                        #region ...or read a new block...

                                        if (CurrentPosition + BlockLength <= ChunkedBytes.Length)
                                        {

                                            NumberOfBlocks++;

                                            DecodedStream.Write(ChunkedBytes, CurrentPosition, BlockLength);
                                            CurrentPosition += BlockLength;

                                            if (CurrentPosition < ChunkedBytes.Length &&
                                                ChunkedBytes[CurrentPosition] == 0x0d)
                                            {
                                                CurrentPosition++;
                                            }

                                            if (CurrentPosition < ChunkedBytes.Length - 1 &&
                                                ChunkedBytes[CurrentPosition] == 0x0a)
                                            {
                                                CurrentPosition++;
                                            }

                                            LastPos = CurrentPosition;

                                            IsStatus_ReadBlockLength = false;

                                        }

                                        #endregion

                                        #region ...or start over!

                                        else
                                        {
                                            // Reaching this point means we need to read more
                                            // data from the network stream and decode again!

                                            //Debug.WriteLine(DateTime.Now + " Chunked decoding restarted after reading " + NumberOfBlocks + " blocks!");

                                            break;

                                        }

                                        #endregion

                                    }

                                    else
                                    {
                                        IsStatus_ReadBlockLength = true;
                                        CurrentPosition++;
                                    }

                                } while (CurrentPosition < _ChunkedStream.Length);

                            } while (!ChunkedDecodingFinished);

                        }
                        catch (Exception e)
                        {
                            DebugX.Log("Chunked decoding failed: " + e.Message);
                        }

                    }

                    #endregion

                    #region ...or just connect HTTP stream to network stream!

                    else
                        Response.ContentStreamToArray();

                    #endregion

                    #region Close connection if requested!

                    if (Response.Connection == null ||
                        Response.Connection == "close")
                    {

                        if (TCPSocket != null)
                        {
                            TCPSocket.Close();
                            //TCPClient.Dispose();
                            TCPSocket = null;
                        }

                        HTTPStream = null;

                    }

                    #endregion

                }
                catch (Exception e)
                {

                    #region Create a HTTP response for the exception...

                    while (e.InnerException != null)
                        e = e.InnerException;

                    Response = new HTTPResponseBuilder(Request,
                                                       HTTPStatusCode.BadRequest)
                    {

                        ContentType = HTTPContentType.JSON_UTF8,
                        Content = JSONObject.Create(new JProperty("Message", e.Message),
                                                         new JProperty("StackTrace", e.StackTrace)).
                                                  ToUTF8Bytes()

                    };

                    #endregion

                    if (TCPSocket != null)
                    {
                        TCPSocket.Close();
                        //TCPClient.Dispose();
                        TCPSocket = null;
                    }

                }


                #region Call the optional HTTP response log delegate

                try
                {

                    if (ResponseLogDelegate != null)
                        await Task.WhenAll(ResponseLogDelegate.GetInvocationList().
                                           Cast<ClientResponseLogHandler>().
                                           Select(e => e(DateTime.UtcNow,
                                                         this,
                                                         Request,
                                                         Response))).
                                           ConfigureAwait(false);

                }
                catch (Exception e2)
                {
                    e2.Log(nameof(HTTPClient) + "." + nameof(ResponseLogDelegate));
                }

                #endregion

                return Response;

            });

        }

        #endregion


        #region Close()

        public void Close()
        {

            try
            {
                if (HTTPStream != null)
                {
                    HTTPStream.Close();
                    HTTPStream.Dispose();
                }
            }
            catch (Exception)
            { }

            try
            {
                if (TLSStream != null)
                {
                    TLSStream.Close();
                    TLSStream.Dispose();
                }
            }
            catch (Exception)
            { }

            try
            {
                if (TCPStream != null)
                {
                    TCPStream.Close();
                    TCPStream.Dispose();
                }
            }
            catch (Exception)
            { }

            try
            {
                if (TCPSocket != null)
                {
                    TCPSocket.Close();
                    //TCPClient.Dispose();
                }
            }
            catch (Exception)
            { }

        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns>A string representation of this object.</returns>
        public override String ToString()
        {
            return String.Concat(this.GetType().Name, " ", RemoteIPAddress.ToString(), ":", RemotePort);
        }

        #endregion

    }

}
