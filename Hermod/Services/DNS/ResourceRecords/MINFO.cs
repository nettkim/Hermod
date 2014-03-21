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

    /// <summary>
    /// MINFO - DNS Resource Record
    /// </summary>
    public class MINFO : ADNSResourceRecord
    {

        #region Data

        public const UInt16 TypeId = 14;

        #endregion

        #region Properties

        #region Text

        private readonly String _Text;

        public String Text
        {
            get 
            {
                return _Text;
            }
        }

        #endregion

        #endregion

        #region Constructor

        #region MINFO(Stream)

        public MINFO(Stream Stream)
            : base(Stream, TypeId)
        {
            this._Text  = DNSTools.ExtractName(Stream);
        }

        #endregion

        #region MINFO(Name, Stream)

        public MINFO(String  Name,
                     Stream  Stream)

            : base(Name, TypeId, Stream)

        {
            this._Text  = DNSTools.ExtractName(Stream);
        }

        #endregion

        #region MINFO(Name, Class, TimeToLive, RText)

        public MINFO(String           Name,
                     DNSQueryClasses  Class,
                     TimeSpan         TimeToLive,
                     String           RText)

            : base(Name, TypeId, Class, TimeToLive, RText)

        {
            this._Text  = RText;
        }

        #endregion

        #endregion

    }

}
