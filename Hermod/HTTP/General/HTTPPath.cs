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

using org.GraphDefined.Vanaheimr.Illias;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

#endregion

namespace org.GraphDefined.Vanaheimr.Hermod.HTTP
{

    /// <summary>
    /// The path of a HTTP path.
    /// </summary>
    public struct HTTPPath : IEquatable<HTTPPath>,
                             IComparable<HTTPPath>,
                             IComparable
    {

        #region Data

        /// <summary>
        /// The internal identifier.
        /// </summary>
        private readonly String  InternalId;

        /// <summary>
        /// The regular expression for parsing a HTTP URI.
        /// </summary>
        //public static readonly Regex URI_RegEx  = new Regex(@"^https:\/\/.+$",
        //                                                    RegexOptions.IgnorePatternWhitespace);

        #endregion

        #region Properties

        /// <summary>
        /// The HTTP hostname part of the HTTP URI.
        /// </summary>
        //public HTTPHostname  Hostname   { get; }

        /// <summary>
        /// The length of the HTTP path.
        /// </summary>
        public UInt32 Length
            => (UInt32) InternalId.Length;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new HTTP path
        /// </summary>
        /// <param name="URI">The uniform resource identifier.</param>
        private HTTPPath(String URI)
        {

            #region Initial checks

            //if (!URI_RegEx.IsMatch(URI))
            //    throw new ArgumentException("the given URI '" + URI + "' is invalid!", nameof(URI));

            #endregion

            this.InternalId  = URI;
            //this.Hostname    = HTTPHostname.Parse(URI.Substring(URI.IndexOf("://"), URI.IndexOfAny(new Char[] { '/', ':' }, URI.IndexOf("://") + 3 )));

        }

        #endregion


        #region Parse   (Text)

        /// <summary>
        /// Parse the given text representation of a HTTP path.
        /// </summary>
        /// <param name="Text">A text representation of a HTTP path.</param>
        public static HTTPPath Parse(String Text)
        {

            if (TryParse(Text, out HTTPPath HTTPPath))
                return HTTPPath;

            throw new ArgumentException("The given string could not be parsed as a HTTP path!", nameof(Text));

        }

        #endregion

        #region TryParse(Text)

        /// <summary>
        /// Try to parse the given text representation of a HTTP path.
        /// </summary>
        /// <param name="Text">A text representation of a HTTP path.</param>
        public static HTTPPath? TryParse(String Text)
        {

            if (TryParse(Text, out HTTPPath URI))
                return URI;

            return new HTTPPath?();

        }

        #endregion

        #region TryParse(Text, out URI)

        /// <summary>
        /// Try to parse the given text representation of a HTTP path.
        /// </summary>
        /// <param name="Text">A text representation of a HTTP path.</param>
        /// <param name="URI">The parsed HTTP path.</param>
        public static Boolean TryParse(String Text, out HTTPPath URI)
        {

            if (Text != null)
                Text = Text.Trim();

            if (!Text.StartsWith("/"))
                Text = "/" + Text;

            //if (!URI_RegEx.IsMatch(Text))
            //{
                URI = new HTTPPath(Text);
                return true;
            //}

            //URI = default(HTTPPath);
            //return false;

        }

        #endregion


        public Boolean Contains(String Text)
            => InternalId.Contains(Text);


        public HTTPPath Substring(Int32 StartIndex)
            => Parse(InternalId.Substring(StartIndex));

        public HTTPPath Substring(Int32 StartIndex, Int32 EndIndex)
            => Parse(InternalId.Substring(StartIndex, EndIndex));


        public int IndexOf(char value, int startIndex, int count)
            => InternalId.IndexOf(value, startIndex, count);

        public int IndexOf(char value, int startIndex)
            => InternalId.IndexOf(value, startIndex);

        public int IndexOf(String value)
            => InternalId.IndexOf(value);

        public int IndexOf(String value, int startIndex)
            => InternalId.IndexOf(value, startIndex);

        public int IndexOf(String value, int startIndex, int count)
            => InternalId.IndexOf(value, startIndex, count);

        public int IndexOf(String value, StringComparison comparisonType)
            => InternalId.IndexOf(value, comparisonType);

        public int IndexOf(String value, int startIndex, StringComparison comparisonType)
            => InternalId.IndexOf(value, startIndex, comparisonType);

        public int IndexOf(char value)
            => InternalId.IndexOf(value);


        public int LastIndexOf(char value, int startIndex, int count)
            => InternalId.LastIndexOf(value, startIndex, count);

        public int LastIndexOf(char value, int startIndex)
            => InternalId.LastIndexOf(value, startIndex);

        public int LastIndexOf(String value)
            => InternalId.LastIndexOf(value);

        public int LastIndexOf(String value, int startIndex)
            => InternalId.LastIndexOf(value, startIndex);

        public int LastIndexOf(String value, int startIndex, int count)
            => InternalId.LastIndexOf(value, startIndex, count);

        public int LastIndexOf(String value, StringComparison comparisonType)
            => InternalId.LastIndexOf(value, comparisonType);

        public int LastIndexOf(String value, int startIndex, StringComparison comparisonType)
            => InternalId.LastIndexOf(value, startIndex, comparisonType);

        public int LastIndexOf(char value)
            => InternalId.LastIndexOf(value);


        public bool StartsWith(String value, StringComparison comparisonType)
            => InternalId.StartsWith(value, comparisonType);

        public bool StartsWith(String value, bool ignoreCase, CultureInfo culture)
            => InternalId.StartsWith(value, ignoreCase, culture);

        public bool StartsWith(String value)
            => InternalId.StartsWith(value);

        public bool StartsWith(HTTPPath value)
            => InternalId.StartsWith(value.ToString());


        public bool EndsWith(String value)
            => InternalId.EndsWith(value);

        public bool EndsWith(String value, StringComparison comparisonType)
            => InternalId.EndsWith(value, comparisonType);

        public bool EndsWith(String value, bool ignoreCase, CultureInfo culture)
            => InternalId.EndsWith(value, ignoreCase, culture);

        public bool EndsWith(HTTPPath value)
            => InternalId.EndsWith(value.ToString());


        public Boolean IsNullOrEmpty()
        {

            if (String.IsNullOrEmpty(InternalId))
                return true;

            return String.IsNullOrEmpty(InternalId.Trim());

        }

        public Boolean IsNotNullOrEmpty()
            => !IsNullOrEmpty();


        #region Operator overloading

        #region Operator == (HTTPPath1, HTTPPath2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="HTTPPath1">A HTTP path.</param>
        /// <param name="HTTPPath2">Another HTTP path.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (HTTPPath HTTPPath1, HTTPPath HTTPPath2)
        {

            // If both are null, or both are same instance, return true.
            if (Object.ReferenceEquals(HTTPPath1, HTTPPath2))
                return true;

            // If one is null, but not both, return false.
            if (((Object) HTTPPath1 == null) || ((Object) HTTPPath2 == null))
                return false;

            return HTTPPath1.Equals(HTTPPath2);

        }

        #endregion

        #region Operator == (HTTPPath1, Text)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="HTTPPath1">A HTTP path.</param>
        /// <param name="Text">Another HTTP path.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (HTTPPath HTTPPath1, String Text)
        {

            // If both are null, or both are same instance, return true.
            if (Object.ReferenceEquals(HTTPPath1, Text))
                return true;

            // If one is null, but not both, return false.
            if (((Object) HTTPPath1 == null) || ((Object) Text == null))
                return false;

            return HTTPPath1.Equals(Text);

        }

        #endregion

        #region Operator != (HTTPPath1, HTTPPath2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="HTTPPath1">A HTTP path.</param>
        /// <param name="HTTPPath2">Another HTTP path.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (HTTPPath HTTPPath1, HTTPPath HTTPPath2)
            => !(HTTPPath1 == HTTPPath2);

        #endregion

        #region Operator != (HTTPPath1, Text)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="HTTPPath1">A HTTP path.</param>
        /// <param name="Text">Another HTTP path.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (HTTPPath HTTPPath1, String Text)
            => !(HTTPPath1 == Text);

        #endregion

        #region Operator <  (HTTPPath1, HTTPPath2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="HTTPPath1">A HTTP path.</param>
        /// <param name="HTTPPath2">Another HTTP path.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (HTTPPath HTTPPath1, HTTPPath HTTPPath2)
        {

            if ((Object) HTTPPath1 == null)
                throw new ArgumentNullException(nameof(HTTPPath1), "The given HTTPPath1 must not be null!");

            return HTTPPath1.CompareTo(HTTPPath2) < 0;

        }

        #endregion

        #region Operator <= (HTTPPath1, HTTPPath2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="HTTPPath1">A HTTP path.</param>
        /// <param name="HTTPPath2">Another HTTP path.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (HTTPPath HTTPPath1, HTTPPath HTTPPath2)
            => !(HTTPPath1 > HTTPPath2);

        #endregion

        #region Operator >  (HTTPPath1, HTTPPath2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="HTTPPath1">A HTTP path.</param>
        /// <param name="HTTPPath2">Another HTTP path.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (HTTPPath HTTPPath1, HTTPPath HTTPPath2)
        {

            if ((Object) HTTPPath1 == null)
                throw new ArgumentNullException(nameof(HTTPPath1), "The given HTTPPath1 must not be null!");

            return HTTPPath1.CompareTo(HTTPPath2) > 0;

        }

        #endregion

        #region Operator >= (HTTPPath1, HTTPPath2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="HTTPPath1">A HTTP path.</param>
        /// <param name="HTTPPath2">Another HTTP path.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (HTTPPath HTTPPath1, HTTPPath HTTPPath2)
            => !(HTTPPath1 < HTTPPath2);

        #endregion


        #region Operator + (HTTPPath1, Text)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="HTTPPath1">A HTTP path.</param>
        /// <param name="Text">Another HTTP path.</param>
        /// <returns>true|false</returns>
        public static HTTPPath operator + (HTTPPath HTTPPath1, String Text)
            => HTTPPath1.EndsWith("/") && Text.StartsWith("/")
                   ? Parse(HTTPPath1.ToString() + Text.Substring(1))
                   : Parse(HTTPPath1.ToString() + Text);

        #endregion

        #region Operator + (HTTPPath1, HTTPPath2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="HTTPPath1">A HTTP path.</param>
        /// <param name="HTTPPath2">Another HTTP path.</param>
        /// <returns>true|false</returns>
        public static HTTPPath operator + (HTTPPath HTTPPath1, HTTPPath HTTPPath2)
            => HTTPPath1.EndsWith("/") && HTTPPath2.StartsWith("/")
                   ? Parse(HTTPPath1.ToString() + HTTPPath2.Substring(1))
                   : Parse(HTTPPath1.ToString() + HTTPPath2);

        #endregion

        #endregion

        #region IComparable<HTTPPath> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public Int32 CompareTo(Object Object)
        {

            if (Object == null)
                throw new ArgumentNullException("The given object must not be null!");

            if (Object is HTTPPath)
                return CompareTo((HTTPPath) Object);

            if (Object is String)
                return InternalId.CompareTo((String) Object);

            throw new ArgumentException("The given object is neither a HTTP path, nor its text representation!");

        }

        #endregion

        #region CompareTo(HTTPPath)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="HTTPPath">An object to compare with.</param>
        public Int32 CompareTo(HTTPPath HTTPPath)
        {

            if ((Object) HTTPPath == null)
                throw new ArgumentNullException("The given HTTP path must not be null!");

            return InternalId.CompareTo(HTTPPath.InternalId);

        }

        #endregion

        #endregion

        #region IEquatable<HTTPPath> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>true|false</returns>
        public override Boolean Equals(Object Object)
        {

            if (Object == null)
                return false;

            if (Object is HTTPPath)
                return Equals((HTTPPath) Object);

            if (Object is String)
                return InternalId.Equals((String) Object);

            return false;

        }

        #endregion

        #region Equals(HTTPPath)

        /// <summary>
        /// Compares two HTTPPaths for equality.
        /// </summary>
        /// <param name="HTTPPath">A HTTPPath to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(HTTPPath HTTPPath)
        {

            if ((Object) HTTPPath == null || InternalId == null)
                return false;

            return InternalId.Equals(HTTPPath.InternalId);

        }

        #endregion

        #endregion

        #region GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()
            => InternalId.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => InternalId;

        #endregion

    }

}
