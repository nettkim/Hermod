/*
 * Copyright (c) 2013 Achim Friedland <achim.friedland@belectric.com>
 * This file is part of eMI3 OICP <http://www.github.com/BelectricDrive/eMI3_OICP>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
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

#endregion

namespace eu.Vanaheimr.Hermod.Services.DNS
{

    #region (enum) SSHFP_Algorithm

    public enum SSHFP_Algorithm
    {
        reserved  = 0,
        RSA       = 1,
        DSS       = 2,
        ECDSA     = 3
    }

    #endregion

    #region (enum) SSHFP_FingerprintType

    public enum SSHFP_FingerprintType
    {
        reserved  = 0,
        SHA1      = 1,
        SHA256    = 2
    }

    #endregion


    /// <summary>
    /// SSH Fingerprint - DNS Resource Record
    /// </summary>
    public class SSHFP : ADNSResourceRecord
    {

        #region Data

        public const UInt16 TypeId = 44;

        #endregion

        #region Properties

        #region Algorithm

        private readonly SSHFP_Algorithm _Algorithm;

        public SSHFP_Algorithm Algorithm
        {
            get
            {
                return _Algorithm;
            }
        }

        #endregion

        #region Typ

        private readonly SSHFP_FingerprintType _Typ;

        public SSHFP_FingerprintType Typ
        {
            get
            {
                return _Typ;
            }
        }

        #endregion

        #region Fingerprint

        private readonly String _Fingerprint;

        public String Fingerprint
        {
            get
            {
                return _Fingerprint;
            }
        }

        #endregion

        #endregion

        #region Constructor

        #region SSHFP(Stream)

        public SSHFP(Stream  Stream)

            : base(Stream, TypeId)

        {

            this._Algorithm    = (SSHFP_Algorithm)       (Stream.ReadByte() & Byte.MaxValue);
            this._Typ          = (SSHFP_FingerprintType) (Stream.ReadByte() & Byte.MaxValue);
            this._Fingerprint  = DNSTools.ExtractName(Stream);

        }

        #endregion

        #region SSHFP(Name, Stream)

        public SSHFP(String  Name,
                     Stream  Stream)

            : base(Name, TypeId, Stream)

        {

            this._Algorithm    = (SSHFP_Algorithm)       (Stream.ReadByte() & Byte.MaxValue);
            this._Typ          = (SSHFP_FingerprintType) (Stream.ReadByte() & Byte.MaxValue);
            this._Fingerprint  = DNSTools.ExtractName(Stream);

        }

        #endregion

        #region SSHFP(Name, Class, TimeToLive, Algorithm, Typ, Fingerprint)

        public SSHFP(String                 Name,
                     DNSQueryClasses        Class,
                     TimeSpan               TimeToLive,
                     SSHFP_Algorithm        Algorithm,
                     SSHFP_FingerprintType  Typ,
                     String                 Fingerprint)

            : base(Name, TypeId, Class, TimeToLive)

        {

            this._Algorithm    = Algorithm;
            this._Typ          = Typ;
            this._Fingerprint  = Fingerprint;

        }

        #endregion

        #endregion

    }

}
