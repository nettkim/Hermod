﻿/*
 * Copyright (c) 2010-2017, Achim 'ahzf' Friedland <achim.friedland@graphdefined.com>
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
using System.Collections.Generic;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.BouncyCastle;
using Org.BouncyCastle.Bcpg;

#endregion

namespace org.GraphDefined.Vanaheimr.Hermod.Mail
{

    /// <summary>
    /// An e-mail builder.
    /// </summary>
    public abstract class AbstractEMailBuilder : AbstractEMail
    {

        #region Data

        protected static readonly String[] TextLineSplitter = new String[] { "\r\n", "\r", "\n" };

        protected readonly List<EMailBodypart> _Attachments;

        #endregion

        #region Properties

        //ToDo: "resentSender", "resentDate", "resentMessageId", "Resent-From", 
        //      "Resent-Reply-To", "Resent-To", "Resent-Cc", "Resent-Bcc",

        #region From

        private EMailAddress _From;

        /// <summary>
        /// The sender of this e-mail.
        /// </summary>
        public EMailAddress From
        {

            get
            {

                if (_From != null)
                    return _From;

                var _FromString = MailHeaders.
                                      Where(kvp => kvp.Key.ToLower() == "from").
                                      FirstOrDefault();

                if (_FromString.Key != null)
                {
                    _From = EMailAddress.Parse(_FromString.Value);
                    return _From;
                }

                return null;

            }

            set
            {

                if (value != null)
                {

                    _From = value;

                    this.SetEMailHeader("From", value.ToString());

                }

            }

        }

        #endregion

        #region To

        private EMailAddressListBuilder _To;

        /// <summary>
        /// The receivers of this e-mail.
        /// </summary>
        public EMailAddressListBuilder To
        {

            get
            {

                if (_To != null)
                    return _To;

                var _ToString = MailHeaders.
                                      Where(kvp => kvp.Key.ToLower() == "to").
                                      FirstOrDefault();

                if (_ToString.Key != null)
                {
                    _To = EMailAddressListBuilder.Parse(_ToString.Value);
                    return _To;
                }

                return null;

            }

            set
            {

                if (value != null)
                {

                    _To = value;

                    this.SetEMailHeader("To", value.ToString());

                }

            }

        }

        #endregion

        #region ReplyTo

        private readonly EMailAddressListBuilder _ReplyTo;

        /// <summary>
        /// The receivers of any reply on this e-mail.
        /// </summary>
        public EMailAddressListBuilder ReplyTo
        {

            get
            {
                return _ReplyTo;
            }

            set
            {
                if (value != null)
                    _ReplyTo.Add(value);
            }

        }

        #endregion

        #region Cc

        private readonly EMailAddressListBuilder _Cc;

        /// <summary>
        /// Additional receivers of this e-mail.
        /// </summary>
        public EMailAddressListBuilder Cc
        {

            get
            {
                return _Cc;
            }

            set
            {
                if (value != null)
                    _Cc.Add(value);
            }

        }

        #endregion

        #region Bcc

        private readonly EMailAddressListBuilder _Bcc;

        /// <summary>
        /// Additional but hidden receivers of this e-mail.
        /// </summary>
        public EMailAddressListBuilder Bcc
        {

            get
            {
                return _Bcc;
            }

            set
            {
                if (value != null)
                    _Bcc.Add(value);
            }

        }

        #endregion

        #region Subject

        private String _Subject;

        /// <summary>
        /// The subject of this e-mail.
        /// </summary>
        public String Subject
        {

            get
            {

                if (_Subject != null)
                    return _Subject;

                var _String = MailHeaders.
                                      Where(kvp => kvp.Key.ToLower() == "subject").
                                      FirstOrDefault();

                if (_String.Key != null)
                {
                    _Subject = _String.Value;
                    return _Subject;
                }

                return "";

            }

            set
            {

                if (value != null && value != String.Empty && value.Trim() != "")
                {
                    _Subject = value.Trim();
                    this.SetEMailHeader("Subject", value.ToString());
                }

            }

        }

        #endregion

        #region Date

        private Nullable<DateTime> _Date;

        /// <summary>
        /// The sending timestamp of this e-mail.
        /// </summary>
        public DateTime Date
        {

            get
            {

                if (_Date.HasValue)
                    return _Date.Value;

                var _String = MailHeaders.
                                      Where(kvp => kvp.Key.ToLower() == "date").
                                      FirstOrDefault();

                if (_String.Key != null)
                {
                    _Date = DateTime.Parse(_String.Value);
                    return _Date.Value;
                }

                return DateTime.MinValue;

            }

            set
            {
                this.SetEMailHeader("Date", value.ToString("ddd, d MMM yyyy HH:mm:ss zzz"));
            }

        }

        #endregion

        #region MessageId

        private MessageId _MessageId;

        /// <summary>
        /// The unique message identification of the e-mail.
        /// </summary>
        public MessageId MessageId
        {

            get
            {
                return _MessageId;
            }

            set
            {
                if (value != null)
                    _MessageId = value;
            }

        }

        #endregion

        private readonly MessageId _Reference;

        private readonly List<MessageId> _References;

        #region SecurityLevel

        /// <summary>
        /// The security level of the e-mail.
        /// </summary>
        public EMailSecurity SecurityLevel { get; set; }

        #endregion

        #region SymmetricKeyAlgorithm

        /// <summary>
        /// The symmetric key algorithm to use.
        /// </summary>
        public SymmetricKeyAlgorithms SymmetricKeyAlgorithm { get; set; }

        #endregion

        #region HashAlgorithm

        /// <summary>
        /// The hash algorithm to use.
        /// </summary>
        public HashAlgorithms HashAlgorithm { get; set; }

        #endregion

        #region CompressionAlgorithm

        /// <summary>
        /// The compression algorithm to use.
        /// </summary>
        public CompressionAlgorithms CompressionAlgorithm { get; set; }

        #endregion

        #region Passphrase

        public String Passphrase
        {
            get;
            set;
        }

        #endregion


        #region Body

        private EMailBodypart _Body;

        /// <summary>
        /// The e-mail body.
        /// </summary>
        public EMailBodypart Body
        {

            get
            {
                return _Body;
            }

            set
            {
                if (value != null)
                    _Body = value;
            }

        }

        #endregion


        #region AsImmutable

        /// <summary>
        /// Convert this e-mail builder to an immutable e-mail.
        /// </summary>
        public EMail AsImmutable
        {
            get
            {

                if (From.Address.Value.IsNullOrEmpty() ||
                    To.Count()                     < 1 ||
                    Subject.IsNullOrEmpty())

                    throw new Exception("Invalid email!");

                return new EMail(this);

            }
        }

        #endregion

        #endregion

        #region Constructor(s)

        #region AbstractEMailBuilder()

        /// <summary>
        /// Create a new e-mail builder.
        /// </summary>
        public AbstractEMailBuilder()
            : base()
        {

            this._To                    = EMailAddressListBuilder.Empty;
            this._ReplyTo               = EMailAddressListBuilder.Empty;
            this._Cc                    = EMailAddressListBuilder.Empty;
            this._Bcc                   = EMailAddressListBuilder.Empty;
            this._Subject               = "";
            this. Date                  = DateTime.Now;
            this._References            = new List<MessageId>();
            this._Attachments           = new List<EMailBodypart>();

            this.SecurityLevel          = EMailSecurity.auto;
            this.SymmetricKeyAlgorithm  = SymmetricKeyAlgorithms.Aes256;
            this.HashAlgorithm          = HashAlgorithms.Sha512;
            this.CompressionAlgorithm   = CompressionAlgorithms.Zip;

            _MailHeaders.Add(new KeyValuePair<String, String>("MIME-Version", "1.0"));

        }

        #endregion

        #region AbstractEMailBuilder(EMail)

        /// <summary>
        /// Parse the e-mail from the given e-mail.
        /// </summary>
        /// <param name="EMail">An e-mail.</param>
        public AbstractEMailBuilder(EMail EMail)
            : base(EMail)
        {

            this._Body = new EMailBodypart(EMail.ToText);

        }

        #endregion

        #region AbstractEMailBuilder(MailText)

        /// <summary>
        /// Parse the e-mail from the given text lines.
        /// </summary>
        /// <param name="MailText">The E-Mail as an enumeration of strings.</param>
        public AbstractEMailBuilder(IEnumerable<String> MailText)
            : base(MailText)
        {

            this._Body = new EMailBodypart(MailText);

        }

        #endregion

        #endregion


        #region AddAttachment(EMailBodypart)

        /// <summary>
        /// Add an attachment to this e-mail.
        /// </summary>
        /// <param name="EMailBodypart">An attachment.</param>
        public T AddAttachment<T>(EMailBodypart EMailBodypart)
            where T : AbstractEMailBuilder
        {

            _Attachments.Add(EMailBodypart);

            return (T) this;

        }

        #endregion


        #region (protected, abstract) EncodeBodyparts()

        /// <summary>
        /// Encode all nested e-mail body parts.
        /// </summary>
        protected abstract EMailBodypart _EncodeBodyparts();

        #endregion

        #region (internal) EncodeBodyparts()

        /// <summary>
        /// Encode this and all nested e-mail body parts.
        /// </summary>
        internal AbstractEMailBuilder EncodeBodyparts()
        {

            var SignTheMail     = false;
            var EncryptTheMail  = false;

            EMailBodypart BodypartToBeSecured = null;

            #region Add attachments, if available...

            if (_Attachments       == null ||
                _Attachments.Count == 0)
                BodypartToBeSecured  = _EncodeBodyparts();

            else
                BodypartToBeSecured  = new EMailBodypart(ContentTypeBuilder:       AEMail => new MailContentType(AEMail, MailContentTypes.multipart_mixed) { CharSet = "utf-8" }.GenerateMIMEBoundary(),
                                                         ContentTransferEncoding:  "8bit",
                                                         Content:                  new String[] { "This is a multi-part message in MIME format." },
                                                         NestedBodyparts:          new EMailBodypart[] { _EncodeBodyparts() }.
                                                                                       Concat(_Attachments));

            #endregion


            #region Check security settings

            switch (SecurityLevel)
            {

                case EMailSecurity.autosign:

                    if (From.SecretKeyRing != null &&
                        From.SecretKeyRing.Any()   &&
                        Passphrase.IsNotNullOrEmpty())
                        SignTheMail = true;

                    break;


                case EMailSecurity.sign:

                    if (From.SecretKeyRing == null ||
                       !From.SecretKeyRing.Any()   ||
                        Passphrase.IsNullOrEmpty())
                        throw new ApplicationException("Can not sign the e-mail!");

                    SignTheMail = true;

                    break;


                case EMailSecurity.auto:

                    if (From.SecretKeyRing != null &&
                        From.SecretKeyRing.Any()   &&
                        Passphrase.IsNotNullOrEmpty())
                        SignTheMail = true;

                    if (SignTheMail &&
                        (!To.Any() | To.Any(v => v.PublicKeyRing != null && v.PublicKeyRing.Any() )) &&
                        (!Cc.Any() | Cc.Any(v => v.PublicKeyRing != null && v.PublicKeyRing.Any() )))
                        EncryptTheMail = true;

                    break;


                case EMailSecurity.encrypt:

                    if (From.SecretKeyRing == null  ||
                       !From.SecretKeyRing.Any()    ||
                        Passphrase.IsNullOrEmpty()  ||
                        To.Any(v => v.PublicKeyRing == null || !v.PublicKeyRing.Any() ) ||
                        Cc.Any(v => v.PublicKeyRing == null || !v.PublicKeyRing.Any() ))
                        throw new ApplicationException("Can not sign and encrypt the e-mail!");

                    EncryptTheMail = true;

                    break;

            }

            #endregion

            #region Sign the e-mail

            if (SignTheMail & !EncryptTheMail)
            {

                var DataToBeSigned      = BodypartToBeSecured.

                                              // Include headers of this MIME body
                                              // https://tools.ietf.org/html/rfc1847 Security Multiparts for MIME:
                                              ToText().

                                              // Any trailing whitespace MUST then be removed from the signed material
                                              Select(line => line.TrimEnd()).

                                              // Canonical text format with <CR><LF> line endings
                                              // https://tools.ietf.org/html/rfc3156 5. OpenPGP signed data
                                              Aggregate((a, b) => a + "\r\n" + b)

                                              //ToDo: Apply Content-Transfer-Encoding
                                              ;

                // MIME Security with OpenPGP (rfc3156, https://tools.ietf.org/html/rfc3156)
                // OpenPGP Message Format     (rfc4880, https://tools.ietf.org/html/rfc4880)
                _Body = new EMailBodypart(ContentTypeBuilder:          AMail => new MailContentType(AMail, MailContentTypes.multipart_signed) {
                                                                           MicAlg    = "pgp-sha512",
                                                                           Protocol  = "application/pgp-signature",
                                                                           CharSet   = "utf-8",
                                                                       },
                                          ContentTransferEncoding:     "8bit",
                                          NestedBodyparts:             new EMailBodypart[] {

                                                                           BodypartToBeSecured,

                                                                           new EMailBodypart(ContentTypeBuilder:       AMail => new MailContentType(AMail, MailContentTypes.application_pgp__signature) { CharSet = "utf-8" },
                                                                                         //    ContentTransferEncoding:  "8bit",
                                                                                             ContentDescription:       "OpenPGP digital signature",
                                                                                             ContentDisposition:       ContentDispositions.attachment.ToString() + "; filename=\"signature.asc\"",
                                                                                             Content:                  new String[] {

                                                                                                                           OpenPGP.CreateSignature(new MemoryStream(DataToBeSigned.ToUTF8Bytes()),
                                                                                                                                                   From.SecretKeyRing.First(),
                                                                                                                                                   Passphrase,
                                                                                                                                                   HashAlgorithm: HashAlgorithm).

                                                                                                                                   WriteTo(new MemoryStream(), CloseOutputStream: false).
                                                                                                                                       ToUTF8String()

                                                                                                                       })

                                                                       }
                                         );

            }

            #endregion

            #region Encrypt the e-mail

            else if (SignTheMail & EncryptTheMail)
            {

                var Plaintext   = BodypartToBeSecured.ToText().Aggregate((a, b) => a + "\r\n" + b).ToUTF8Bytes();
                var Ciphertext  = new MemoryStream();

                OpenPGP.EncryptSignAndZip(InputStream:            new MemoryStream(Plaintext),
                                          Length:                 (UInt64) Plaintext.Length,
                                          SecretKey:              From.SecretKeyRing.First(),
                                          Passphrase:             Passphrase,
                                          PublicKey:              To.First().PublicKeyRing.First(),
                                          OutputStream:           Ciphertext,
                                          SymmetricKeyAlgorithm:  SymmetricKeyAlgorithm,
                                          HashAlgorithm:          HashAlgorithm,
                                          CompressionAlgorithm:   CompressionAlgorithm,
                                          ArmoredOutput:          true,
                                          Filename:               "encrypted.asc",
                                          LastModificationTime:   DateTime.UtcNow);

                // MIME Security with OpenPGP (rfc3156, https://tools.ietf.org/html/rfc3156)
                // OpenPGP Message Format     (rfc4880, https://tools.ietf.org/html/rfc4880)
                _Body = new EMailBodypart(ContentTypeBuilder:          AMail => new MailContentType(AMail, MailContentTypes.multipart_encrypted) {
                                                                           Protocol = "application/pgp-encrypted",
                                                                           CharSet  = "utf-8",
                                                                       },
                                          ContentTransferEncoding:     "8bit",
                                          NestedBodyparts:             new EMailBodypart[] {

                                                                           new EMailBodypart(ContentTypeBuilder:   AMail => new MailContentType(AMail, MailContentTypes.application_pgp__encrypted) { CharSet = "utf-8" },
                                                                                             ContentDescription:   "PGP/MIME version identification",
                                                                                             ContentDisposition:   ContentDispositions.attachment.ToString() + "; filename=\"signature.asc\"",
                                                                                             Content:              new String[] { "Version: 1" }),

                                                                           new EMailBodypart(ContentTypeBuilder:   AMail => new MailContentType(AMail, MailContentTypes.application_octet__stream) { CharSet = "utf-8" },
                                                                                             ContentDescription:   "OpenPGP encrypted message",
                                                                                             ContentDisposition:   ContentDispositions.inline.ToString() + "; filename=\"encrypted.asc\"",
                                                                                             Content:              new String[] { Ciphertext.ToArray().ToUTF8String() }),

                                                                       }
                                         );

            }

            #endregion


            else
                this._Body = BodypartToBeSecured;

            return this;

        }

        #endregion

    }

}
