#region Copyright
// 
// DotNetNukeŽ - http://www.dotnetnuke.com
// Copyright (c) 2002-2014
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
// 
// Hotcakes Commerce - https://hotcakes.org
// Copyright (c) 2017
// by Hotcakes Commerce, LLC
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

#endregion

#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Xml;
using DotNetNuke.Entities.Modules;

#endregion

namespace DotNetNuke.Common.Utilities
{
    /// <summary>
    /// The CBO class generates objects.
    /// </summary>
    public partial class CBO
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Obsolete in Hotcakes Commerce 7.3.  Use CreateObject<T>(bool)")]
        public static TObject CreateObject<TObject>()
        {
            return (TObject)CreateObjectInternal(typeof(TObject), false);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Obsolete in Hotcakes Commerce 7.3.  Use CreateObject<T>(bool)")]
        public static object CreateObject(Type objType, bool initialise)
        {
            return CreateObjectInternal(objType, initialise);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Obsolete in Hotcakes Commerce 7.3.  Use FillDictionary<TKey, TValue>(string keyField, IDataReader dr)")]
        public static IDictionary<int, TItem> FillDictionary<TItem>(IDataReader dr) where TItem : IHydratable
        {
            return FillDictionaryFromReader("KeyID", dr, new Dictionary<int, TItem>(), true);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Obsolete in Hotcakes Commerce 7.3.  Use FillDictionary<TKey, TValue>(string keyField, IDataReader dr, IDictionary<TKey, TValue> objDictionary)")]
        public static IDictionary<int, TItem> FillDictionary<TItem>(IDataReader dr, ref IDictionary<int, TItem> objToFill) where TItem : IHydratable
        {
            return FillDictionaryFromReader("KeyID", dr, objToFill, true);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Obsolete in Hotcakes Commerce 7.3.  Replaced by FillObject<T> ")]
        public static object FillObject(IDataReader dr, Type objType)
        {
            return CreateObjectFromReader(objType, dr, true);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Obsolete in Hotcakes Commerce 7.3.  Replaced by FillObject<T> ")]
        public static object FillObject(IDataReader dr, Type objType, bool closeReader)
        {
            return CreateObjectFromReader(objType, dr, closeReader);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Obsolete in Hotcakes Commerce 5.0.  Replaced by GetProperties(Of TObject)() ")]
        public static ArrayList GetPropertyInfo(Type objType)
        {
            var arrProperties = new ArrayList();

            //get cached object mapping for type
            ObjectMappingInfo objMappingInfo = GetObjectMapping(objType);

            arrProperties.AddRange(objMappingInfo.Properties.Values);

            return arrProperties;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Obsolete in Hotcakes Commerce 5.0.  Replaced by SerializeObject(Object) ")]
        public static XmlDocument Serialize(object objObject)
        {
            var document = new XmlDocument();
            SerializeObject(objObject, document);
            return document;
        }
    }
}
