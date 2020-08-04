/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

using Azos.IO;
using Azos.Data;

namespace Azos.Serialization.Arow
{
  /// <summary>
  /// Provides low-level Arow format writing
  /// </summary>
  public static class Writer
  {
    public const int MAX_ARRAY_LENGTH = 1024 * 1024;

    public static readonly Dictionary<Type, string> SER_TYPE_MAP = new Dictionary<Type, string>
    {
      {typeof(bool),       null},
      {typeof(bool[]),     null},
      {typeof(List<bool>), null},

      {typeof(byte),       null},
      {typeof(byte[]),     null},
      {typeof(List<byte>), null},

      {typeof(sbyte),       null},
      {typeof(sbyte[]),     null},
      {typeof(List<sbyte>), null},

      {typeof(short),         null},
      {typeof(short[]),       null},
      {typeof(List<short>),   null},

      {typeof(ushort),         null},
      {typeof(ushort[]),       null},
      {typeof(List<ushort>),   null},

      {typeof(int),         null},
      {typeof(int[]),       null},
      {typeof(List<int>),   null},

      {typeof(uint),       null},
      {typeof(uint[]),     null},
      {typeof(List<uint>), null},

      {typeof(long),         null},
      {typeof(long[]),       null},
      {typeof(List<long>),   null},

      {typeof(ulong),       null},
      {typeof(ulong[]),     null},
      {typeof(List<ulong>), null},

      {typeof(float),         null},
      {typeof(float[]),       null},
      {typeof(List<float>),   null},

      {typeof(double),         null},
      {typeof(double[]),       null},
      {typeof(List<double>),   null},

      {typeof(decimal),         null},
      {typeof(decimal[]),       null},
      {typeof(List<decimal>),   null},

      {typeof(char),         null},
      {typeof(char[]),       null},
      {typeof(List<char>),   null},

      {typeof(string),         null},
      {typeof(string[]),       null},
      {typeof(List<string>),   null},

      {typeof(Financial.Amount),         null},
      {typeof(Financial.Amount[]),       null},
      {typeof(List<Financial.Amount>),   null},

      {typeof(DateTime),       null},
      {typeof(DateTime[]),     null},
      {typeof(List<DateTime>), null},

      {typeof(TimeSpan),       null},
      {typeof(TimeSpan[]),     null},
      {typeof(List<TimeSpan>), null},

      {typeof(Guid),       null},
      {typeof(Guid[]),     null},
      {typeof(List<Guid>), null},

      {typeof(Data.GDID),       null},
      {typeof(Data.GDID[]),     null},
      {typeof(List<Data.GDID>), null},

      {typeof(FID),       null},
      {typeof(FID[]),     null},
      {typeof(List<FID>), null},

      {typeof(Pile.PilePointer),       null},
      {typeof(Pile.PilePointer[]),     null},
      {typeof(List<Pile.PilePointer>), null},

      {typeof(Serialization.JSON.NLSMap),       null},
      {typeof(Serialization.JSON.NLSMap[]),     null},
      {typeof(List<Serialization.JSON.NLSMap>), null},

      {typeof(Atom),       null},
      {typeof(Atom[]),     null},
      {typeof(List<Atom>), null},

    };









    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static void WriteHeader(WritingStreamer streamer)
    {
      streamer.Write((byte)0xC0);
      streamer.Write((byte)0xFE);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static void WriteEORow(WritingStreamer streamer)
    {
      streamer.Write((ulong)0x00);//null name/atom
    }

    public static void WriteNull(WritingStreamer streamer, ulong name)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Null);
    }

    public static void WriteRow(WritingStreamer streamer, ulong name, TypedDoc doc)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Doc);
      ArowSerializer.Serialize(doc, streamer, false);
    }

    public static void WriteRowArray(WritingStreamer streamer, ulong name, IEnumerable<TypedDoc> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.Doc);
      streamer.Write(array.Count());
      foreach(var row in array)
      {
        if (row==null)
          streamer.Write(false);
        else
        {
          streamer.Write(true);
          ArowSerializer.Serialize(row, streamer, false);
        }
      }
    }

    public static void WriteObject(WritingStreamer streamer, ulong name, object obj)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.POD);
      var ser = new Slim.PODSlimSerializer();
      using (var ms = new MemoryStream())
      {
        ser.Serialize(ms, obj);
        streamer.Write(ms.ToArray());
      }
    }

    public static void WriteObjectArray(WritingStreamer streamer, ulong name, IEnumerable<object> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.POD);
      streamer.Write(array.Count());
      var ser = new Slim.PODSlimSerializer();
      foreach(var obj in array)
        using (var ms = new MemoryStream())
        {
          ser.Serialize(ms, obj);
          streamer.Write(ms.ToArray());
        }
    }

    public static void Write(WritingStreamer streamer, ulong name, bool value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Boolean);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<bool> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.Boolean);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, byte value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Byte);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<byte> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.Byte);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, byte[] value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.ByteArray);
      streamer.Write(value);
    }



    public static void Write(WritingStreamer streamer, ulong name, sbyte value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.SByte);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<sbyte> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.SByte);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }



    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<int> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.Int32);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<long> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.Int64);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<double> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.Double);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name,  IEnumerable<float> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.Single);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<decimal> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.Decimal);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, char value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Char);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<char> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.Char);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, string value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.String);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<string> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.String);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, float value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Single);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, double value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Double);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, decimal value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Decimal);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, Financial.Amount value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Amount);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<Financial.Amount> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.Amount);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }


    public static void Write(WritingStreamer streamer, ulong name, int value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Int32);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, uint value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.UInt32);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<uint> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.UInt32);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }


    public static void Write(WritingStreamer streamer, ulong name, long value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Int64);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, ulong value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.UInt64);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<ulong> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.UInt64);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, short value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Int16);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<short> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.Int16);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, ushort value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.UInt16);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<ushort> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.UInt16);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, DateTime value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.DateTime);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<DateTime> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.DateTime);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, TimeSpan value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.TimeSpan);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<TimeSpan> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.TimeSpan);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, Guid value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Guid);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<Guid> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.Guid);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, Data.GDID value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.GDID);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<Data.GDID> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.GDID);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, FID value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.FID);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<FID> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.FID);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, Pile.PilePointer value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.PilePointer);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<Pile.PilePointer> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.PilePointer);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, JSON.NLSMap value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.NLSMap);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<JSON.NLSMap> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.NLSMap);
      streamer.Write(array.Count());
      foreach(var e in array)
        streamer.Write(e);
    }

    public static void Write(WritingStreamer streamer, ulong name, Atom value)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Atom);
      streamer.Write(value);
    }

    public static void Write(WritingStreamer streamer, ulong name, IEnumerable<Atom> array)
    {
      streamer.Write(name);
      streamer.Write((byte)DataType.Array);
      streamer.Write((byte)DataType.Atom);
      streamer.Write(array.Count());
      foreach (var e in array)
        streamer.Write(e);
    }

  }
}
