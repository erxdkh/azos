﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Azos.Platform;

namespace Azos.Serialization.JSON
{
  /// <summary>
  /// Defines a custom Json conversion handler which is typically used for custom casting of Json datagrams into
  /// CLR types, or type resolution for polymorphic deserialization.
  /// For example: an CLR field of type  Animal[] gets populated by objects of Cat,Dog, Fish types
  /// as distinguished by a custom pattern match on their Json shapes
  /// </summary>
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
  public abstract class JsonHandlerAttribute : Attribute
  {
    private static FiniteSetLookup<MemberInfo, JsonHandlerAttribute> s_Cache =
      new FiniteSetLookup<MemberInfo, JsonHandlerAttribute>( site => site.GetCustomAttribute<JsonHandlerAttribute>(true) );

    /// <summary>
    /// Tries to find an attribute decorating the target site
    /// </summary>
    public static JsonHandlerAttribute TryFind(MemberInfo site)
    {
      switch(site)
      {
        case null: return null;

        case Type tp when (!tp.IsPrimitive) &&
                          (tp != typeof(string)) &&
                          (tp.IsClass || tp.IsValueType): return s_Cache[tp];

        case PropertyInfo pi: return s_Cache[pi];

        default: return null;
      }
    }


    public enum TypeCastOutcome
    {
      NothingChanged = 0,
      ChangedTargetType = 0x0f,
      ChangedSourceValue = 0xff,
      HandledCast = 0xffff
    }

    /// <summary>
    ///Provides information about the outcome of TypeCastOnRead() operation
    /// </summary>
    public struct TypeCastResult
    {
      public static TypeCastResult NothingChanged => new TypeCastResult();

      public TypeCastResult(TypeCastOutcome outcome, object result, Type toType)
      {
        Outcome = outcome;
        Value = result;
        ToType = toType;
      }

      public TypeCastResult(Type toType)
      {
        Outcome = TypeCastOutcome.ChangedTargetType;
        Value = null;
        ToType = toType;
      }

      public readonly TypeCastOutcome Outcome;
      public readonly object Value;
      public readonly Type ToType;
    }

    /// <summary>
    /// Override to perform a typecast operation on read
    /// </summary>
    /// <param name="v">A value to cast, e.g. int, string or JsonDataMap or JsonDataArray</param>
    /// <param name="toType">Requested CLR type to cast into. The result must be assignment compatible with this type</param>
    /// <param name="fromUI">True when datagram comes from user interface</param>
    /// <param name="options">Controls field name matching</param>
    /// <returns>TypeCastResult which provides value and/or type</returns>
    public abstract TypeCastResult TypeCastOnRead(object v, Type toType, bool fromUI, JsonReader.DocReadOptions options);
  }
}
