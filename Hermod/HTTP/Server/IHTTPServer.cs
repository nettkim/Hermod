﻿using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using org.GraphDefined.Vanaheimr.Styx.Arrows;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.Sockets.TCP;

namespace org.GraphDefined.Vanaheimr.Hermod.HTTP
{

    public interface IHTTPServer : IDisposable
    {

        String                               DefaultServerName             { get; }
        HTTPSecurity                         HTTPSecurity                  { get; }

        DNSClient                            DNSClient                     { get; }

        /// <summary>
        /// The optional delegate to select a SSL/TLS server certificate.
        /// </summary>
        ServerCertificateSelectorDelegate    ServerCertificateSelector     { get; }

        /// <summary>
        /// The optional delegate to verify the SSL/TLS client certificate used for authentication.
        /// </summary>
        RemoteCertificateValidationCallback  ClientCertificateValidator    { get; }

        /// <summary>
        /// The optional delegate to select the SSL/TLS client certificate used for authentication.
        /// </summary>
        LocalCertificateSelectionCallback    ClientCertificateSelector     { get; }

        /// <summary>
        /// The SSL/TLS protocol(s) allowed for this connection.
        /// </summary>
        SslProtocols                         AllowedTLSProtocols           { get; }

        /// <summary>
        /// Is the server already started?
        /// </summary>
        Boolean IsStarted { get; }

        /// <summary>
        /// The current number of attached TCP clients.
        /// </summary>
        UInt64 NumberOfClients { get; }


        event RequestLogHandler RequestLog;
        event AccessLogHandler  AccessLog;
        event ErrorLogHandler   ErrorLog;
        event BoomerangSenderHandler<String, DateTime, HTTPRequest, Task<HTTPResponse>> OnNotification;

        HTTPEventSource AddEventSource(HTTPEventSource_Id              EventIdentification,
                                       UInt32                          MaxNumberOfCachedEvents,
                                       TimeSpan?                       RetryIntervall              = default(TimeSpan?),
                                       Boolean                         EnableLogging               = true,
                                       Func<String, DateTime, String>  LogfileName                 = null);

        HTTPEventSource AddEventSource(HTTPEventSource_Id              EventIdentification,
                                       HTTPURI                         URITemplate,

                                       UInt32                          MaxNumberOfCachedEvents     = 500,
                                       TimeSpan?                       RetryIntervall              = null,
                                       Boolean                         EnableLogging               = false,
                                       String                          LogfilePrefix               = null,
                                       Func<String, DateTime, String>  LogfileName                 = null,
                                       String                          LogfileReloadSearchPattern  = null,

                                       HTTPHostname?                   Hostname                    = null,
                                       HTTPMethod?                     HTTPMethod                  = null,
                                       HTTPContentType                 HTTPContentType             = null,

                                       HTTPAuthentication              URIAuthentication           = null,
                                       HTTPAuthentication              HTTPMethodAuthentication    = null,

                                       HTTPDelegate                    DefaultErrorHandler         = null);

        void AddMethodCallback(HTTPHostname              Hostname,
                               HTTPMethod                HTTPMethod,
                               HTTPURI                   URITemplate,
                               HTTPContentType           HTTPContentType             = null,
                               HTTPAuthentication        URIAuthentication           = null,
                               HTTPAuthentication        HTTPMethodAuthentication    = null,
                               HTTPAuthentication        ContentTypeAuthentication   = null,
                               HTTPRequestDetailLogger   HTTPRequestLogger           = null,
                               HTTPResponseDetailLogger  HTTPResponseLogger          = null,
                               HTTPDelegate              DefaultErrorHandler         = null,
                               HTTPDelegate              HTTPDelegate                = null,
                               URIReplacement            AllowReplacement            = URIReplacement.Fail);

        void AddMethodCallback(HTTPHostname              Hostname,
                               HTTPMethod                HTTPMethod,
                               IEnumerable<HTTPURI>      URITemplates,
                               HTTPContentType           HTTPContentType             = null,
                               HTTPAuthentication        URIAuthentication           = null,
                               HTTPAuthentication        HTTPMethodAuthentication    = null,
                               HTTPAuthentication        ContentTypeAuthentication   = null,
                               HTTPRequestDetailLogger   HTTPRequestLogger           = null,
                               HTTPResponseDetailLogger  HTTPResponseLogger          = null,
                               HTTPDelegate              DefaultErrorHandler         = null,
                               HTTPDelegate              HTTPDelegate                = null,
                               URIReplacement            AllowReplacement            = URIReplacement.Fail);

        void AddMethodCallback(HTTPHostname                  Hostname,
                               HTTPMethod                    HTTPMethod,
                               HTTPURI                       URITemplate,
                               IEnumerable<HTTPContentType>  HTTPContentTypes,
                               HTTPAuthentication            URIAuthentication           = null,
                               HTTPAuthentication            HTTPMethodAuthentication    = null,
                               HTTPAuthentication            ContentTypeAuthentication   = null,
                               HTTPRequestDetailLogger       HTTPRequestLogger           = null,
                               HTTPResponseDetailLogger      HTTPResponseLogger          = null,
                               HTTPDelegate                  DefaultErrorHandler         = null,
                               HTTPDelegate                  HTTPDelegate                = null,
                               URIReplacement                AllowReplacement            = URIReplacement.Fail);

        void AddMethodCallback(HTTPHostname                  Hostname,
                               HTTPMethod                    HTTPMethod,
                               IEnumerable<HTTPURI>          URITemplates,
                               IEnumerable<HTTPContentType>  HTTPContentTypes,
                               HTTPAuthentication            URIAuthentication           = null,
                               HTTPAuthentication            HTTPMethodAuthentication    = null,
                               HTTPAuthentication            ContentTypeAuthentication   = null,
                               HTTPRequestDetailLogger       HTTPRequestLogger           = null,
                               HTTPResponseDetailLogger      HTTPResponseLogger          = null,
                               HTTPDelegate                  DefaultErrorHandler         = null,
                               HTTPDelegate                  HTTPDelegate                = null,
                               URIReplacement                AllowReplacement            = URIReplacement.Fail);

        IHTTPServer AttachTCPPort(IPPort Port);
        IHTTPServer AttachTCPPorts(params IPPort[] Ports);
        IHTTPServer AttachTCPSocket(IPSocket Socket);
        IHTTPServer AttachTCPSockets(params IPSocket[] Sockets);
        IHTTPServer DetachTCPPort(IPPort Port);
        IHTTPServer DetachTCPPorts(params IPPort[] Ports);

        Tuple<MethodInfo, IEnumerable<object>> GetErrorHandler(string Host, string URL, HTTPMethod? HTTPMethod = null, HTTPContentType HTTPContentType = null, HTTPStatusCode HTTPStatusCode = null);
        HTTPEventSource GetEventSource(HTTPEventSource_Id EventSourceIdentification);
        IEnumerable<HTTPEventSource> GetEventSources(Func<HTTPEventSource, Boolean> IncludeEventSource = null);
        Task<HTTPResponse> InvokeHandler(HTTPRequest Request);

        void Redirect(HTTPHostname Hostname, HTTPMethod HTTPMethod, HTTPURI URITemplate, HTTPContentType HTTPContentType, HTTPURI URITarget);
        void Redirect(HTTPMethod HTTPMethod, HTTPURI URITemplate, HTTPContentType HTTPContentType, HTTPURI URITarget);

        void AddFilter(HTTPFilter1Delegate Filter);
        void AddFilter(HTTPFilter2Delegate Filter);

        void Rewrite(HTTPRewrite1Delegate Rewrite);
        void Rewrite(HTTPRewrite2Delegate Rewrite);

        bool TryGetEventSource(HTTPEventSource_Id EventSourceIdentification, out HTTPEventSource EventSource);
        void UseEventSource   (HTTPEventSource_Id EventSourceIdentification, Action<HTTPEventSource> Action);
        void UseEventSource<T>(HTTPEventSource_Id EventSourceIdentification, IEnumerable<T> DataSource, Action<HTTPEventSource, T> Action);

        void Start();
        void Start(TimeSpan Delay, Boolean InBackground = true);
        void Shutdown(String Message = null, Boolean Wait = true);

    }

}