/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Azos.Data
{
    /// <summary>
    /// Provides extension methods for converting string values to different scalar types
    /// </summary>
    public static class StringValueConversion
    {

         /// <summary>
         /// Used by env var macros evaluator do not remove
         /// </summary>
         public static string AsStringWhenNullOrEmpty(this string val, string dflt = "")
         {
              return val.AsString(dflt);
         }

         /// <summary>
         /// Used by env var macros evaluator do not remove
         /// </summary>
         public static string AsString(this string val, string dflt = "")
         {
              if (string.IsNullOrEmpty(val))
                return dflt;
              else
                return val;
         }


         public static readonly char[] ARRAY_SPLIT_CHARS = new []{',', ';'};

         public static byte[] AsByteArray(this string val, byte[] dflt = null)
         {
              if (val==null) return dflt;
              try
              {
                var result = new List<byte>();
                var segs = val.Split(ARRAY_SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries);
                foreach(var seg in segs)
                {
                  //byte arrays defaults to prefix-less hex
                  byte b;
                  if (byte.TryParse(seg, NumberStyles.HexNumber, null, out b))
                  {
                    result.Add(b);
                    continue;
                  }
                  result.Add( seg.AsByte(handling: ConvertErrorHandling.Throw)) ;
                }

                return result.ToArray();
              }
              catch
              {
                return dflt;
              }
         }

         public static int[] AsIntArray(this string val, int[] dflt = null)
         {
              if (val==null) return dflt;
              try
              {
                var result = new List<int>();
                var segs = val.Split(ARRAY_SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries);
                foreach(var seg in segs)
                 result.Add( seg.AsInt(handling: ConvertErrorHandling.Throw));

                return result.ToArray();
              }
              catch
              {
                return dflt;
              }
         }

         public static long[] AsLongArray(this string val, long[] dflt = null)
         {
              if (val==null) return dflt;
              try
              {
                var result = new List<long>();
                var segs = val.Split(ARRAY_SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries);
                foreach(var seg in segs)
                 result.Add( seg.AsLong(handling: ConvertErrorHandling.Throw));

                return result.ToArray();
              }
              catch
              {
                return dflt;
              }
         }


         public static float[] AsFloatArray(this string val, float[] dflt = null)
         {
              if (val==null) return dflt;
              try
              {
                var result = new List<float>();
                var segs = val.Split(ARRAY_SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries);
                foreach(var seg in segs)
                 result.Add( seg.AsFloat(handling: ConvertErrorHandling.Throw));

                return result.ToArray();
              }
              catch
              {
                return dflt;
              }
         }


         public static double[] AsDoubleArray(this string val, double[] dflt = null)
         {
              if (val==null) return dflt;
              try
              {
                var result = new List<double>();
                var segs = val.Split(ARRAY_SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries);
                foreach(var seg in segs)
                 result.Add( seg.AsDouble(handling: ConvertErrorHandling.Throw));

                return result.ToArray();
              }
              catch
              {
                return dflt;
              }
         }

         public static decimal[] AsDecimalArray(this string val, decimal[] dflt = null)
         {
              if (val==null) return dflt;
              try
              {
                var result = new List<decimal>();
                var segs = val.Split(ARRAY_SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries);
                foreach(var seg in segs)
                 result.Add( seg.AsDecimal(handling: ConvertErrorHandling.Throw));

                return result.ToArray();
              }
              catch
              {
                return dflt;
              }
         }


         public static GDID AsGDID(this string val, GDID? dflt = null)
         {
              if (dflt.HasValue)
                return ObjectValueConversion.AsGDID(val, dflt.Value);
              else
                return ObjectValueConversion.AsGDID(val);
         }

         public static GDID? AsNullableGDID(this string val, GDID? dflt = null)
         {
              return ObjectValueConversion.AsNullableGDID(val, dflt);
         }

         public static GDIDSymbol AsGDIDSymbol(this string val, GDIDSymbol? dflt = null)
         {
              if (dflt.HasValue)
                return ObjectValueConversion.AsGDIDSymbol(val, dflt.Value);
              else
                return ObjectValueConversion.AsGDIDSymbol(val);
         }

         public static GDIDSymbol? AsNullableGDIDSymbol(this string val, GDIDSymbol? dflt = null)
         {
              return ObjectValueConversion.AsNullableGDIDSymbol(val, dflt);
         }


         public static byte AsByte(this string val, byte dflt = 0)
         {
              return ObjectValueConversion.AsByte(val, dflt);
         }

         public static byte? AsNullableByte(this string val, byte? dflt = 0)
         {
              return ObjectValueConversion.AsNullableByte(val, dflt);
         }

         public static sbyte AsSByte(this string val, sbyte dflt = 0)
         {
              return ObjectValueConversion.AsSByte(val, dflt);
         }

         public static sbyte? AsNullableSByte(this string val, sbyte? dflt = 0)
         {
              return ObjectValueConversion.AsNullableSByte(val, dflt);
         }


         public static short AsShort(this string val, short dflt = 0)
         {
              return ObjectValueConversion.AsShort(val, dflt);
         }

         public static short? AsNullableShort(this string val, short? dflt = 0)
         {
              return ObjectValueConversion.AsNullableShort(val, dflt);
         }


         public static ushort AsUShort(this string val, ushort dflt = 0)
         {
              return ObjectValueConversion.AsUShort(val, dflt);
         }

         public static ushort? AsNullableUShort(this string val, ushort? dflt = 0)
         {
              return ObjectValueConversion.AsNullableUShort(val, dflt);
         }


         public static int AsInt(this string val, int dflt = 0)
         {
              return ObjectValueConversion.AsInt(val, dflt);
         }

         public static int? AsNullableInt(this string val, int? dflt = 0)
         {
              return ObjectValueConversion.AsNullableInt(val, dflt);
         }

         public static uint AsUInt(this string val, uint dflt = 0)
         {
              return ObjectValueConversion.AsUInt(val, dflt);
         }

         public static uint? AsNullableUInt(this string val, uint? dflt = 0)
         {
              return ObjectValueConversion.AsNullableUInt(val, dflt);
         }


         public static long AsLong(this string val, long dflt = 0)
         {
              return ObjectValueConversion.AsLong(val, dflt);
         }

         public static long? AsNullableLong(this string val, long? dflt = 0)
         {
             return ObjectValueConversion.AsNullableLong(val, dflt);
         }

         public static ulong AsULong(this string val, ulong dflt = 0)
         {
              return ObjectValueConversion.AsULong(val, dflt);
         }

         public static ulong? AsNullableULong(this string val, ulong? dflt = 0)
         {
             return ObjectValueConversion.AsNullableULong(val, dflt);
         }


         public static double AsDouble(this string val, double dflt = 0d)
         {
             return ObjectValueConversion.AsDouble(val, dflt);
         }

         public static double? AsNullableDouble(this string val, double? dflt = 0d)
         {
             return ObjectValueConversion.AsNullableDouble(val, dflt);
         }


         public static float AsFloat(this string val, float dflt = 0f)
         {
             return ObjectValueConversion.AsFloat(val, dflt);
         }

         public static float? AsNullableFloat(this string val, float? dflt = 0f)
         {
            return ObjectValueConversion.AsNullableFloat(val, dflt);
         }


         public static decimal AsDecimal(this string val, decimal dflt = 0m)
         {
            return ObjectValueConversion.AsDecimal(val, dflt);
         }


         public static decimal? AsNullableDecimal(this string val, decimal? dflt = 0m)
         {
            return ObjectValueConversion.AsNullableDecimal(val, dflt);
         }


         public static bool AsBool(this string val, bool dflt = false)
         {
            return ObjectValueConversion.AsBool(val, dflt);
         }

         public static bool? AsNullableBool(this string val, bool? dflt = false)
         {
            return ObjectValueConversion.AsNullableBool(val, dflt);
         }


         public static Guid AsGUID(this string val, Guid dflt)
         {
            return ObjectValueConversion.AsGUID(val, dflt);
         }

         public static Guid? AsNullableGUID(this string val, Guid? dflt = null)
         {
            return ObjectValueConversion.AsNullableGUID(val, dflt);
         }

         public static DateTime AsDateTimeOrThrow(this string val)
         {
            return ObjectValueConversion.AsDateTime(val);
         }

         public static DateTime AsDateTime(this string val, DateTime dflt)
         {
            return ObjectValueConversion.AsDateTime(val, dflt);
         }

         public static DateTime AsDateTimeFormat(this string val, DateTime dflt, string fmt, DateTimeStyles fmtStyles = DateTimeStyles.None)
         {
            DateTime result;
            return DateTime.TryParseExact(val, fmt, null, fmtStyles, out result) ? result : dflt;
         }

         public static DateTime? AsNullableDateTime(this string val, DateTime? dflt = null)
         {
            return ObjectValueConversion.AsNullableDateTime(val, dflt);
         }


         public static TimeSpan AsTimeSpanOrThrow(this string val)
         {
            return ObjectValueConversion.AsTimeSpan(val, TimeSpan.FromSeconds(0), ConvertErrorHandling.Throw);
         }

         public static TimeSpan AsTimeSpan(this string val, TimeSpan dflt)
         {
            return ObjectValueConversion.AsTimeSpan(val, dflt);
         }

         public static TimeSpan? AsNullableTimeSpan(this string val, TimeSpan? dflt = null)
         {
            return ObjectValueConversion.AsNullableTimeSpan(val, dflt);
         }

         public static TEnum AsEnum<TEnum>(this string val, TEnum dflt = default(TEnum)) where TEnum : struct
         {
            return ObjectValueConversion.AsEnum<TEnum>(val, dflt);
         }

         public static TEnum? AsNullableEnum<TEnum>(this string val, TEnum? dflt = null) where TEnum : struct
         {
            return ObjectValueConversion.AsNullableEnum<TEnum>(val, dflt);
         }

         public static Uri AsUri(this string val, Uri dflt = null)
         {
            return ObjectValueConversion.AsUri(val, dflt);
         }

         public static Atom AsAtom(this string val)
         {
           return ObjectValueConversion.AsAtom(val);
         }

         public static Atom AsAtom(this string val, Atom dflt)
         {
           return ObjectValueConversion.AsAtom(val, dflt);
         }

         public static Atom? AsNullableAtom(this string val, Atom? dflt = null)
         {
           return ObjectValueConversion.AsNullableAtom(val, dflt);
         }


              private static Dictionary<Type, Func<string, bool, object>> s_CONV = new Dictionary<Type,Func<string,bool,object>>
              {
                   {typeof(object)   , (val, strict) => val },
                   {typeof(string)   , (val, strict) => val },
                   {typeof(char)     , (val, strict) => val.IsNullOrEmpty() ? (char)0 : val.Length==1 ? val[0] : throw new AzosException("(char)`{0}`".Args(val)) },
                   {typeof(int)      , (val, strict) => strict ? int.Parse(val)   : AsInt(val)  },
                   {typeof(uint)     , (val, strict) => strict ? uint.Parse(val)  : AsUInt(val)  },
                   {typeof(long)     , (val, strict) => strict ? long.Parse(val)  : AsLong(val) },
                   {typeof(ulong)    , (val, strict) => strict ? ulong.Parse(val) : AsULong(val) },
                   {typeof(short)    , (val, strict) => strict ? short.Parse(val) : AsShort(val)},
                   {typeof(ushort)   , (val, strict) => strict ? ushort.Parse(val): AsUShort(val)},
                   {typeof(byte)     , (val, strict) => strict ? byte.Parse(val)  : AsByte(val) },
                   {typeof(sbyte)    , (val, strict) => strict ? sbyte.Parse(val) : AsSByte(val) },
                   {typeof(bool)     , (val, strict) => strict ? bool.Parse(val)  : AsBool(val) },
                   {typeof(float)    , (val, strict) => strict ? float.Parse(val)    : AsFloat(val) },
                   {typeof(double)   , (val, strict) => strict ? double.Parse(val)   : AsDouble(val) },
                   {typeof(decimal)  , (val, strict) => strict ? decimal.Parse(val)  : AsDecimal(val) },
                   {typeof(TimeSpan) , (val, strict) => strict ? TimeSpan.Parse(val) : AsTimeSpanOrThrow(val) },
                   {typeof(DateTime) , (val, strict) => strict ? DateTime.Parse(val) : AsDateTimeOrThrow(val) },
                   {typeof(Atom)     , (val, strict) => strict ? AsAtom(val) : AsAtom(val, Atom.ZERO) },
                   {typeof(GDID)     , (val, strict) => strict ? GDID.Parse(val) : AsGDID(val) },
                   {typeof(GDIDSymbol),
                                         (val, strict) =>
                                         {
                                           if (strict)
                                           {
                                             var gdid = GDID.Parse(val);
                                             return new GDIDSymbol(gdid, val);
                                           }
                                           return  AsGDIDSymbol(val);
                                         }},
                   {typeof(Guid)      , (val, strict)  => strict ? Guid.Parse(val) : AsGUID(val, Guid.Empty) },

                   {typeof(byte[])    , (val, strict)  => AsByteArray(val)},
                   {typeof(int[])     , (val, strict)  => AsIntArray(val)},
                   {typeof(char[])    , (val, strict)  => val==null ? null : val.ToCharArray()},
                   {typeof(long[])    , (val, strict)  => AsLongArray(val)},
                   {typeof(float[])   , (val, strict)  => AsFloatArray(val)},
                   {typeof(double[])  , (val, strict)  => AsDoubleArray(val)},
                   {typeof(decimal[]) , (val, strict)  => AsDecimalArray(val)},

                   {typeof(char?)     ,(val, strict) => val.IsNullOrEmpty() ? (char?)0 : val.Length==1 ? val[0] : throw new AzosException("(char?)`{0}`".Args(val)) },
                   {typeof(int?),      (val, strict) => string.IsNullOrWhiteSpace(val) ? (int?)null      : (strict ? int.Parse(val)   : AsInt(val)) },
                   {typeof(uint?),     (val, strict) => string.IsNullOrWhiteSpace(val) ? (uint?)null     : (strict ? uint.Parse(val)  : AsUInt(val)) },
                   {typeof(long?),     (val, strict) => string.IsNullOrWhiteSpace(val) ? (long?)null     : (strict ? long.Parse(val)  : AsLong(val)) },
                   {typeof(ulong?),    (val, strict) => string.IsNullOrWhiteSpace(val) ? (ulong?)null    : (strict ? ulong.Parse(val) : AsULong(val)) },
                   {typeof(short?),    (val, strict) => string.IsNullOrWhiteSpace(val) ? (short?)null    : (strict ? short.Parse(val) : AsShort(val)) },
                   {typeof(ushort?),   (val, strict) => string.IsNullOrWhiteSpace(val) ? (ushort?)null   : (strict ? ushort.Parse(val): AsUShort(val)) },
                   {typeof(byte?),     (val, strict) => string.IsNullOrWhiteSpace(val) ? (byte?)null     : (strict ? byte.Parse(val)  : AsByte(val)) },
                   {typeof(sbyte?),    (val, strict) => string.IsNullOrWhiteSpace(val) ? (sbyte?)null    : (strict ? sbyte.Parse(val) : AsSByte(val)) },
                   {typeof(bool?),     (val, strict) => string.IsNullOrWhiteSpace(val) ? (bool?)null     : (strict ? bool.Parse(val)  : AsBool(val)) },
                   {typeof(float?),    (val, strict) => string.IsNullOrWhiteSpace(val) ? (float?)null    : (strict ? float.Parse(val)    : AsFloat(val)) },
                   {typeof(double?),   (val, strict) => string.IsNullOrWhiteSpace(val) ? (double?)null   : (strict ? double.Parse(val)   : AsDouble(val)) },
                   {typeof(decimal?),  (val, strict) => string.IsNullOrWhiteSpace(val) ? (decimal?)null  : (strict ? decimal.Parse(val)  : AsDecimal(val)) },
                   {typeof(TimeSpan?), (val, strict) => string.IsNullOrWhiteSpace(val) ? (TimeSpan?)null : (strict ? TimeSpan.Parse(val) : AsNullableTimeSpan(val)) },
                   {typeof(DateTime?), (val, strict) => string.IsNullOrWhiteSpace(val) ? (DateTime?)null : (strict ? DateTime.Parse(val) : AsNullableDateTime(val)) },
                   {typeof(Atom?),     (val, strict) => string.IsNullOrWhiteSpace(val) ? (Atom?)null     : (strict ? AsAtom(val) : AsAtom(val, Atom.ZERO)) },
                   {typeof(GDID?),     (val, strict) => string.IsNullOrWhiteSpace(val) ? (GDID?)null     : (strict ? GDID.Parse(val) : AsGDID(val)) },
                   {typeof(GDIDSymbol?),
                                       (val, strict) =>
                                       {
                                         if (string.IsNullOrWhiteSpace(val)) return (GDIDSymbol?)null;
                                         if (strict)
                                         {
                                           var gdid = GDID.Parse(val);
                                           return new GDIDSymbol(gdid, val);
                                         }
                                         return AsGDIDSymbol(val);
                                        }},
                   {typeof(Guid?),     (val, strict) => string.IsNullOrWhiteSpace(val) ? (Guid?)null     : (strict ? Guid.Parse(val) : AsGUID(val, Guid.Empty)) },
                   {typeof(Uri),       (val, strict) => string.IsNullOrWhiteSpace(val) ? (Uri)null       : (strict ? new Uri(val) : AsUri(val)) }
              };




              /// <summary>
              /// Tries to get a string value as specified type.
              /// When 'strict=false', tries to do some inference like return "true" for numbers that dont equal to zero etc.
              /// When 'strict=true' throws an exception if deterministic conversion is not possible
              /// </summary>
              public static object AsType(this string val, Type tp, bool strict = true)
              {
                try
                {
                    Func<string, bool, object> func = null;
                    if (s_CONV.TryGetValue(tp, out func)) return func(val, strict);

                    if (tp.IsEnum)
                    {
                      return Enum.Parse(tp, val, true);
                    }

                    if (tp.IsGenericType && tp.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                          var v = val;
                          if (string.IsNullOrWhiteSpace(v)) return null;


                          var gargs = tp.GetGenericArguments();
                          if (gargs.Length==1)
                          {
                                var gtp = gargs[0];
                                if (gtp.IsEnum)
                                {
                                    return Enum.Parse(gtp, v, true);
                                }
                          }
                    }

                }
                catch(Exception error)
                {
                  throw new AzosException(string.Format(StringConsts.STRING_VALUE_COULD_NOT_BE_GOTTEN_AS_TYPE_ERROR,
                                                        val ?? CoreConsts.NULL_STRING, tp.FullName), error);
                }

                throw new AzosException(string.Format(StringConsts.STRING_VALUE_COULD_NOT_BE_GOTTEN_AS_TYPE_ERROR,
                                                        val ?? CoreConsts.NULL_STRING, tp.FullName));
              }


    }
}
