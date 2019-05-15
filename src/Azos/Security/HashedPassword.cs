/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Azos.Data;
using Azos.Serialization.JSON;

namespace Azos.Security
{
  /// <summary>
  /// Represents abstraction of a hashed password, the concrete password algorithm provide implementation (i.e. bytebuffer, dictionary, string)
  /// </summary>
  [Azos.Serialization.Slim.SlimSerializationProhibited]
  public sealed class HashedPassword : IJsonWritable, IEnumerable<KeyValuePair<string, object>>
  {
    #region CONSTS
      public const string KEY_ALGO = "algo";
      public const string KEY_FAM = "fam";
    #endregion

    #region Static
      public static HashedPassword FromString(string str)
      {
        if (str.IsNullOrWhiteSpace())
          throw new SecurityException(StringConsts.ARGUMENT_ERROR + "HashedPassword.FromString(str==null|empty)");
        var password = new HashedPassword();
        var json = str.JsonToDataObject() as JsonDataMap;
        if (json == null || json[KEY_ALGO].AsString().IsNullOrWhiteSpace())
          throw new SecurityException(StringConsts.ARGUMENT_ERROR + "HashedPassword.FromString(!map|!algo)");
        password.m_Content = json;
        return password;
      }
    #endregion

    #region .ctor

      private HashedPassword() { }

      public HashedPassword(string algoName, PasswordFamily family)
      {
        m_Content = new JsonDataMap(false);
        m_Content[KEY_ALGO] = algoName;
        m_Content[KEY_FAM] = family;
      }

    #endregion

    #region Field
      private JsonDataMap m_Content;
    #endregion

    #region Properties
      public string AlgoName { get { return m_Content[KEY_ALGO].AsString(); } }
      public PasswordFamily Family { get { return m_Content[KEY_FAM].AsEnum(PasswordFamily.Unspecified); } }

      public object this[string key]
      {
        get { return m_Content[key]; }
        set
        {
          if (key.EqualsOrdIgnoreCase(KEY_ALGO))
            throw new SecurityException(GetType().Name + ".this[algo].readonly");
          if (key.EqualsOrdIgnoreCase(KEY_FAM))
            throw new SecurityException(GetType().Name + ".this[fam].readonly");

          m_Content[key] = value;
        }
      }
    #endregion

    #region Public
      public void Add(string key, object value) { m_Content.Add(key, value); }

      public override string ToString() { return m_Content.ToJson(); }

      public void WriteAsJson(TextWriter wri, int nestingLevel, JsonWritingOptions options = null)
      {
        JsonWriter.WriteMap(wri, m_Content, nestingLevel, options);
      }

      public IEnumerator<KeyValuePair<string, object>> GetEnumerator() { return m_Content.GetEnumerator(); }
      IEnumerator IEnumerable.GetEnumerator() { return m_Content.GetEnumerator(); }
    #endregion
  }
}
