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
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Net.Security;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using org.GraphDefined.Vanaheimr.Hermod.DNS;

#endregion

namespace org.GraphDefined.Vanaheimr.Hermod.Sockets.TCP
{

    public static class Ext
    {

        public static void Poll(this Socket socket, SelectMode mode, CancellationToken cancellationToken)
        {

            if (!cancellationToken.CanBeCanceled)
                return;

            if (socket != null)
            {
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                } while (!socket.Poll(1000, mode));
            }

            else
                cancellationToken.ThrowIfCancellationRequested();

        }

    }

    public class TCPClient
    {

        #region Data

        private           A[]                    _CachedIPv4Addresses;
        private           AAAA[]                 _CachedIPv6Addresses;
        private           List<IPSocket>         IPSocketList;
        private           IEnumerator<IPSocket>  IPSocketListEnumerator;
        private           IPSocket               CurrentIPSocket;

#if __MonoCS__
        public const SslProtocols DefaultSslProtocols = SslProtocols.Tls;
#else
        public const SslProtocols DefaultSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
#endif

        #endregion

        #region Properties

        #region RemoteHost

        private String _RemoteHost;

        public String RemoteHost
        {
            get
            {
                return _RemoteHost;
            }
        }

        #endregion

        #region ServiceName

        private String _ServiceName;

        public String ServiceName
        {
            get
            {
                return _ServiceName;
            }
        }

        #endregion

        #region RemotePort

        private IPPort _RemotePort;

        public IPPort RemotePort
        {
            get
            {
                return _RemotePort;
            }
        }

        #endregion


        #region UseIPv4

        private readonly Boolean _UseIPv4;

        public Boolean UseIPv4
        {
            get
            {
                return _UseIPv4;
            }
        }

        #endregion

        #region UseIPv6

        private readonly Boolean _UseIPv6;

        public Boolean UseIPv6
        {
            get
            {
                return _UseIPv6;
            }
        }

        #endregion

        #region PreferIPv6

        private readonly Boolean _PreferIPv6;

        public Boolean PreferIPv6
        {
            get
            {
                return _PreferIPv6;
            }
        }

        #endregion

        #region UseTLS

        private TLSUsage _UseTLS;

        public TLSUsage UseTLS
        {
            get
            {
                return _UseTLS;
            }
        }

        #endregion

        #region ConnectionTimeout

        public TimeSpan _ConnectionTimeout;

        public TimeSpan ConnectionTimeout
        {

            get
            {
                return _ConnectionTimeout;
            }

            set
            {
                _ConnectionTimeout = value;
                // Change DNSClient!
            }

        }

        #endregion

        #region DNSClient

        private readonly DNSClient _DNSClient;

        /// <summary>
        /// The default server name.
        /// </summary>
        public virtual DNSClient DNSClient
        {
            get
            {
                return _DNSClient;
            }
        }

        #endregion

        #region CancellationToken

        public CancellationToken? CancellationToken { get; private set; }

        #endregion


        #region TCPSocket

        private Socket _TCPSocket;

        public Socket TCPSocket
        {
            get
            {
                return _TCPSocket;
            }
        }

        #endregion

        #region Stream

        private Stream _Stream;

        public Stream Stream
        {
            get
            {
                return _Stream;
            }
        }

        #endregion

        #region TCPStream

        private NetworkStream _TCPStream;

        public NetworkStream TCPStream
        {
            get
            {
                return _TCPStream;
            }
        }

        #endregion

        #region TLSStream

        private SslStream _TLSStream;

        public SslStream TLSStream
        {
            get
            {
                return _TLSStream;
            }
        }

        #endregion

        #region TLSClientCertificates

        private readonly X509CertificateCollection _TLSClientCertificates;

        public X509CertificateCollection TLSClientCertificates
        {
            get
            {
                return _TLSClientCertificates;
            }
        }

        #endregion

        #endregion

        #region Events

        #region Connected

        public delegate void TCPConnectedDelegate(Object Sender, String DNSName, IPSocket IPSocket);

        public event TCPConnectedDelegate Connected;

        #endregion

        #region ValidateRemoteCertificate

        public delegate Boolean ValidateRemoteCertificateDelegate(TCPClient Sender, X509Certificate Certificate, X509Chain CertificateChain, SslPolicyErrors PolicyErrors);

        public event ValidateRemoteCertificateDelegate ValidateServerCertificate;

        #endregion

        #endregion

        #region Constructor(s)

        #region TCPClient(DNSName = null, ServiceName = "", ConnectionTimeout = null, DNSClient = null, AutoConnect = false)

        /// <summary>
        /// Create a new TCPClient connecting to a remote service using DNS SRV records.
        /// </summary>
        /// <param name="DNSName">The optional DNS name of the remote service to connect to.</param>
        /// <param name="ServiceName">The optional DNS SRV service name of the remote service to connect to.</param>
        /// <param name="UseIPv4">Whether to use IPv4 as networking protocol.</param>
        /// <param name="UseIPv6">Whether to use IPv6 as networking protocol.</param>
        /// <param name="PreferIPv6">Prefer IPv6 (instead of IPv4) as networking protocol.</param>
        /// <param name="ConnectionTimeout">The timeout connecting to the remote service.</param>
        /// <param name="DNSClient">An optional DNS client used to resolve DNS names.</param>
        /// <param name="AutoConnect">Connect to the TCP service automatically on startup. Default is false.</param>
        public TCPClient(String     DNSName            = "",
                         String     ServiceName        = "",
                         Boolean    UseIPv4            = true,
                         Boolean    UseIPv6            = false,
                         Boolean    PreferIPv6         = false,
                         Boolean    UseTLS             = false,
                         TimeSpan?  ConnectionTimeout  = null,
                         DNSClient  DNSClient          = null,
                         Boolean    AutoConnect        = false)
        {

            this._RemoteHost         = DNSName;
            this._ServiceName        = ServiceName;
            this._UseIPv4            = UseIPv4;
            this._UseIPv6            = UseIPv6;
            this._PreferIPv6         = PreferIPv6;

            this._ConnectionTimeout  = (ConnectionTimeout.HasValue)
                                           ? ConnectionTimeout.Value
                                           : TimeSpan.FromSeconds(60);

            this._DNSClient          = (DNSClient != null)
                                           ? DNSClient
                                           : new DNSClient(SearchForIPv4DNSServers: _UseIPv4,
                                                           SearchForIPv6DNSServers: _UseIPv6);

            if (AutoConnect)
                Connect();

        }

        #endregion

        #region TCPClient(RemoteHost, RemotePort, ...)

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RemoteHost"></param>
        /// <param name="RemotePort"></param>
        /// <param name="UseIPv4">Whether to use IPv4 as networking protocol.</param>
        /// <param name="UseIPv6">Whether to use IPv6 as networking protocol.</param>
        /// <param name="PreferIPv6">Prefer IPv6 (instead of IPv4) as networking protocol.</param>
        /// <param name="UseTLS">Whether Transport Layer Security should be used or not.</param>
        /// <param name="ValidateServerCertificate">A callback for validating the remote server certificate.</param>
        /// <param name="ConnectionTimeout">The timeout connecting to the remote service.</param>
        /// <param name="DNSClient">An optional DNS client used to resolve DNS names.</param>
        /// <param name="AutoConnect">Connect to the TCP service automatically on startup. Default is false.</param>
        /// <param name="CancellationToken"></param>
        public TCPClient(String                             RemoteHost,
                         IPPort                             RemotePort,
                         Boolean                            UseIPv4                     = true,
                         Boolean                            UseIPv6                     = false,
                         Boolean                            PreferIPv6                  = false,
                         TLSUsage                           UseTLS                      = TLSUsage.STARTTLS,
                         ValidateRemoteCertificateDelegate  ValidateServerCertificate   = null,
                         TimeSpan?                          ConnectionTimeout           = null,
                         DNSClient                          DNSClient                   = null,
                         Boolean                            AutoConnect                 = false,
                         CancellationToken?                 CancellationToken           = null)


        {

            this._RemoteHost                = RemoteHost;
            this._RemotePort                = RemotePort;
            this.CancellationToken          = CancellationToken != null ? CancellationToken : new CancellationToken();
            this._UseIPv4                   = UseIPv4;
            this._UseIPv6                   = UseIPv6;
            this._PreferIPv6                = PreferIPv6;
            this._UseTLS                    = UseTLS;
            this.ValidateServerCertificate  = ValidateServerCertificate != null
                                                  ? ValidateServerCertificate
                                                  : (TCPClient, Certificate, CertificateChain, PolicyErrors) => false;

            this._ConnectionTimeout         = (ConnectionTimeout.HasValue)
                                                  ? ConnectionTimeout.Value
                                                  : TimeSpan.FromSeconds(60);

            this._DNSClient                 = (DNSClient == null)
                                                  ? new DNSClient(SearchForIPv6DNSServers: true)
                                                  : DNSClient;

            if (AutoConnect)
                Connect();

        }

        #endregion

        #endregion


        #region (private) QueryDNS()

        private void QueryDNS()
        {

            if      (IPv4Address.TryParse(RemoteHost, out IPv4Address ipv4address))
                IPSocketList = new List<IPSocket>() { new IPSocket(ipv4address, this.RemotePort) };

            else if (IPv6Address.TryParse(RemoteHost, out IPv6Address ipv6address))
                IPSocketList = new List<IPSocket>() { new IPSocket(ipv6address, this.RemotePort) };

            else
            {

                var IPv4Task = _DNSClient.Query<A>(RemoteHost);
                IPv4Task.Wait();
                _CachedIPv4Addresses = IPv4Task.Result.ToArray();

                var IPv6Task = _DNSClient.Query<AAAA>(RemoteHost);
                IPv6Task.Wait();
                _CachedIPv6Addresses = IPv6Task.Result.ToArray();

                IPSocketList            = (_CachedIPv4Addresses.Select(ARecord    => new IPSocket(ARecord.   IPv4Address, this.RemotePort)).Concat(
                                           _CachedIPv6Addresses.Select(AAAARecord => new IPSocket(AAAARecord.IPv6Address, this.RemotePort)))).
                                           ToList();

            }

            IPSocketListEnumerator = IPSocketList.GetEnumerator();

        }

        #endregion

        #region (private) CreateAndConnectTCPSocket(_IPAddress, Port)

        private Socket CreateAndConnectTCPSocket(IIPAddress ipAddress, IPPort Port)
        {

            Socket tcpSocket = null;

            if      (ipAddress is IPv4Address)
                tcpSocket = new Socket(AddressFamily.InterNetwork,   SocketType.Stream, ProtocolType.Tcp);

            else if (ipAddress is IPv6Address)
                tcpSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            else
                throw new Exception("IP address '" + ipAddress.ToString() + "' is invalid!");

            tcpSocket.Connect(System.Net.IPAddress.Parse(ipAddress.ToString()), Port.ToUInt16());

            return tcpSocket;

        }

        #endregion

        #region (private) Reconnect()

        private Boolean Reconnect()
        {

            #region Close previous streams and sockets...

            try
            {

                if (_TCPStream != null)
                {
                    _TCPStream.Close();
                    _TCPStream = null;
                }

            }
            catch (Exception)
            { }

            try
            {

                if (_TLSStream != null)
                {
                    _TLSStream.Close();
                    _TLSStream = null;
                }

            }
            catch (Exception)
            { }

            _Stream = null;

            try
            {

                if (_TCPSocket != null)
                {
                    _TCPSocket.Close();
                    _TCPSocket = null;
                }

            }
            catch (Exception)
            { }

            #endregion

            try
            {

                _TCPSocket = CreateAndConnectTCPSocket(CurrentIPSocket.IPAddress, CurrentIPSocket.Port);
                _TCPStream = new NetworkStream(_TCPSocket, true);
                _Stream    = _TCPStream;

                if (UseTLS == TLSUsage.TLSSocket)
                    EnableTLS();

            }
            catch (Exception e)
            {
                _Stream     = null;
                _TCPStream  = null;
                _TLSStream  = null;
                _TCPSocket  = null;
                return false;
            }

            var ConnectedLocal = Connected;
            if (ConnectedLocal != null)
                ConnectedLocal(this, RemoteHost, CurrentIPSocket);

            return true;

        }

        #endregion

        #region Connect()

        public TCPConnectResult Connect()
        {

            // if already connected => return!

            if (RemoteHost == null &&
                RemoteHost == String.Empty)
            {
                return TCPConnectResult.InvalidDomainName;
            }

            var retry = 0;

            do
            {

                if (IPSocketList == null)
                    QueryDNS();

                if (IPSocketList.Count == 0)
                    return TCPConnectResult.NoIPAddressFound;

                // Get next IP socket in ordered list...
                while (IPSocketListEnumerator.MoveNext())
                {

                    CurrentIPSocket = IPSocketListEnumerator.Current;

                    if (Reconnect())
                        return TCPConnectResult.Ok;

                }

                IPSocketList = null;
                retry++;

            } while (retry < 2);

            return TCPConnectResult.UnknownError;

        }

        #endregion


        protected void EnableTLS()
        {

            try
            {
               _TLSStream  = new SslStream(Stream, false, _ValidateRemoteCertificate);
               _TLSStream.AuthenticateAsClient(RemoteHost, TLSClientCertificates, DefaultSslProtocols, true);
               _Stream     = _TLSStream;
            }

            catch (Exception e)
            {
                Console.WriteLine("EnableTLS() failed!");
            }

        }

        private Boolean _ValidateRemoteCertificate(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {

            var ValidateRemoteCertificateLocal = ValidateServerCertificate;
            if (ValidateRemoteCertificateLocal != null)
                return ValidateRemoteCertificateLocal(this, certificate, chain, errors);

            return false;

        }


        #region Disconnect()

        public TCPDisconnectResult Disconnect()
        {
            return TCPDisconnectResult.Ok;
        }

        #endregion


    }

}
