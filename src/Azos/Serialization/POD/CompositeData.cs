/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Azos.Serialization.POD
{
    /// <summary>
    /// Represents a composite (non primitive) data stored in Portable Object Document.
    /// This data is obtained from native types either automatically using reflection or
    ///  from types that perform custom serialization using ISerializable interface
    /// </summary>
    [Serializable]
    public abstract class CompositeData
    {
        #region .ctor
            internal CompositeData()
            {

            }


            internal CompositeData(PortableObjectDocument document, object data, int metaTypeIndex = -1)
            {
                if (data==null)
                 throw new InvalidOperationException("No need to allocate CompositeData from NULL data. Write null directly");//todo Refactor execption type,text etc...

                m_Document = document;

                var tp = data.GetType();
                if (tp.IsPrimitive)
                 throw new InvalidOperationException("Can not allocate CompositeData from primitive type: " + tp.FullName);//todo Refactor execption type,text etc...

                var dict = document.m_CompositeDataDict;
                if (dict==null)
                {
                    dict = new Dictionary<object, int>(Collections.ReferenceEqualityComparer<object>.Default);
                    document.m_CompositeDataDict = dict;
                }

                int existingIndex;
                if (dict.TryGetValue(data, out existingIndex))
                {
                    m_ExistingReferenceIndex = existingIndex;
                }
                else
                {
                    document.m_CompositeData.Add(this);
                    dict.Add(data, document.m_CompositeData.Count-1);
                    m_MetaTypeIndex = metaTypeIndex>=0 ? metaTypeIndex :  MetaType.GetExistingOrNewMetaTypeIndex(document, data.GetType());
                }
            }

        #endregion

        #region Fields
            internal PortableObjectDocument m_Document;

            [NonSerialized]
            internal object __CLRObject; //this is set during conversion POD->CLR, this is a transient internal value

            public int? m_ExistingReferenceIndex;

            public int m_MetaTypeIndex;
        #endregion


        #region Properties
            /// <summary>
            /// Returns document instance that contains this type
            /// </summary>
            public PortableObjectDocument Document { get { return m_Document;} }


            /// <summary>
            /// Returns true when this instance contains a reference to existing object
            /// </summary>
            public bool ExistingReference { get { return m_ExistingReferenceIndex.HasValue;}}

            /// <summary>
            /// Returns either an index that is less than 0 or and index to an existing object which is >=0
            /// </summary>
            public int ExistingReferenceIndex { get { return m_ExistingReferenceIndex.HasValue  ? m_ExistingReferenceIndex.Value : -1; }}


            /// <summary>
            /// Returns another composite data that is referenced, if nothing is referenced, returns null
            /// </summary>
            public CompositeData Referenced
            {
                get
                {
                  if (!ExistingReference) return null;

                  return m_Document.m_CompositeData[m_ExistingReferenceIndex.Value];
                }
            }


            /// <summary>
            /// Returns type of this data
            /// </summary>
            public MetaComplexType Type
            {
                get { return m_Document.m_Types[m_MetaTypeIndex] as MetaComplexType;}
            }


        #endregion


    }
}
