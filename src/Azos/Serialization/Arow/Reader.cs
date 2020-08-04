/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Azos.IO;
using Azos.Data;

namespace Azos.Serialization.Arow
{
  /// <summary>
  /// Provides low-level Arow format reading
  /// </summary>
  public static class Reader
  {

    public static readonly Dictionary<Type, string> DESER_TYPE_MAP = new Dictionary<Type, string>
    {
      {typeof(byte?),       null},
      {typeof(byte),        null},
      {typeof(byte[]),@"
         if (dt==DataType.Null) doc.{0} = null;
         else if (dt==DataType.ByteArray) doc.{0} = streamer.ReadByteArray();
         else if (dt!=DataType.Array) break;
         else
         {{
           atp = Reader.ReadDataType(streamer);
           if (atp!=DataType.Byte) break;
           doc.{0} = Reader.ReadByteArray(streamer);
         }}
         continue;"
      },

      {typeof(List<byte>),@"
         if (dt==DataType.Null) doc.{0} = null;
         else if (dt==DataType.ByteArray) doc.{0} = new List<byte>(streamer.ReadByteArray());
         else if (dt!=DataType.Array) break;
         else
         {{
           atp = Reader.ReadDataType(streamer);
           if (atp!=DataType.Byte) break;
           doc.{0} = new List<byte>(Reader.ReadByteArray(streamer));
         }}
         continue;"
      },

      //-------------------------------------------------------------------------------------------

      {typeof(bool?),       null},
      {typeof(bool),       null},
      {typeof(bool[]),     null},
      {typeof(List<bool>), null},

      {typeof(sbyte?),       null},
      {typeof(sbyte),       null},
      {typeof(sbyte[]),     null},
      {typeof(List<sbyte>), null},

      {typeof(short?),         null},
      {typeof(short),         null},
      {typeof(short[]),       null},
      {typeof(List<short>),   null},

      {typeof(ushort?),         null},
      {typeof(ushort),         null},
      {typeof(ushort[]),       null},
      {typeof(List<ushort>),   null},

      {typeof(int?),         null},
      {typeof(int),         null},
      {typeof(int[]),       null},
      {typeof(List<int>),   null},

      {typeof(uint?),       null},
      {typeof(uint),       null},
      {typeof(uint[]),     null},
      {typeof(List<uint>), null},

      {typeof(long?),         null},
      {typeof(long),         null},
      {typeof(long[]),       null},
      {typeof(List<long>),   null},

      {typeof(ulong?),       null},
      {typeof(ulong),       null},
      {typeof(ulong[]),     null},
      {typeof(List<ulong>), null},

      {typeof(float?),         null},
      {typeof(float),         null},
      {typeof(float[]),       null},
      {typeof(List<float>),   null},

      {typeof(double?),         null},
      {typeof(double),         null},
      {typeof(double[]),       null},
      {typeof(List<double>),   null},

      {typeof(decimal?),         null},
      {typeof(decimal),         null},
      {typeof(decimal[]),       null},
      {typeof(List<decimal>),   null},

      {typeof(char?),         null},
      {typeof(char),         null},
      {typeof(char[]),       null},
      {typeof(List<char>),   null},

      {typeof(string),        null},
      {typeof(string[]),      null},
      {typeof(List<string>),  null},

      {typeof(Financial.Amount?),         null},
      {typeof(Financial.Amount),         null},
      {typeof(Financial.Amount[]),       null},
      {typeof(List<Financial.Amount>),   null},

      {typeof(DateTime?),       null},
      {typeof(DateTime),       null},
      {typeof(DateTime[]),     null},
      {typeof(List<DateTime>), null},

      {typeof(TimeSpan?),       null},
      {typeof(TimeSpan),       null},
      {typeof(TimeSpan[]),     null},
      {typeof(List<TimeSpan>), null},

      {typeof(Guid?),       null},
      {typeof(Guid),       null},
      {typeof(Guid[]),     null},
      {typeof(List<Guid>), null},

      {typeof(Azos.Data.GDID?),       null},
      {typeof(Azos.Data.GDID),       null},
      {typeof(Azos.Data.GDID[]),     null},
      {typeof(List<Azos.Data.GDID>), null},


      {typeof(FID?),       null},
      {typeof(FID),       null},
      {typeof(FID[]),     null},
      {typeof(List<FID>), null},

      {typeof(Pile.PilePointer?),       null},
      {typeof(Pile.PilePointer),       null},
      {typeof(Pile.PilePointer[]),     null},
      {typeof(List<Pile.PilePointer>), null},

      {typeof(JSON.NLSMap?),      null},
      {typeof(JSON.NLSMap),       null},
      {typeof(JSON.NLSMap[]),     null},
      {typeof(List<JSON.NLSMap>), null},

      {typeof(Atom?),       null},
      {typeof(Atom),       null},
      {typeof(Atom[]),     null},
      {typeof(List<Atom>), null},
    };



    [ MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static void ReadHeader(ReadingStreamer streamer)
    {
      if (streamer.ReadByte()==0xC0 && streamer.ReadByte()==0xFE) return;

      throw new ArowException(StringConsts.AROW_HEADER_CORRUPT_ERROR);
    }

    [ MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static ulong ReadName(ReadingStreamer streamer)
    {
      return streamer.ReadULong();
    }

    [ MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static DataType ReadDataType(ReadingStreamer streamer)
    {
      return (DataType)streamer.ReadByte();
    }


    public static TDoc[] ReadRowArray<TDoc>(TypedDoc docScope, ReadingStreamer streamer, string name) where TDoc : TypedDoc, new()
    {
       var len = Reader.ReadArrayLength(streamer);
       var arr = new TDoc[len];
       for(var i=0; i<len; i++)
       {
         var has = streamer.ReadBool();
         if (!has) continue;
         var vrow = new TDoc();
         if (Reader.TryReadRow(docScope, vrow, streamer, name+'_'+i.ToString())) //todo Why extra string instance?
           arr[i] = vrow;
       }
       return arr;
    }

    public static List<TDoc> ReadRowList<TDoc>(TypedDoc docScope, ReadingStreamer streamer, string name) where TDoc : TypedDoc, new()
    {
       var len = Reader.ReadArrayLength(streamer);
       var lst = new List<TDoc>(len);
       for(var i=0; i<len; i++)
       {
         var has = streamer.ReadBool();
         if (!has)
         {
           lst.Add(null);
           continue;
         }
         var vrow = new TDoc();
         if (Reader.TryReadRow(docScope, vrow, streamer, name+'_'+i.ToString())) //todo Why extra string instance?
          lst.Add( vrow );
       }
       return lst;
    }


    public static bool TryReadRow(TypedDoc docScope, TypedDoc newDoc, ReadingStreamer streamer, string name)
    {
       var ok = ArowSerializer.TryDeserialize(newDoc, streamer, false);
       if (ok) return true;

       var map = readRowAsMap(streamer);//unconditionally to advance stream

       var arow = docScope as IAmorphousData;
       if (arow==null) return false;
       if (!arow.AmorphousDataEnabled) return false;
       arow.AmorphousData[name] = map;
       return false;
    }

    public static object ConsumeUnmatched(TypedDoc doc, ReadingStreamer streamer, string name, DataType dt, DataType? atp)
    {
       object value = null;

       if (dt!=DataType.Null)
       {
         if (dt==DataType.Array && !atp.HasValue)
           atp = Reader.ReadDataType(streamer);

         if (atp.HasValue)
         {
           var len = ReadArrayLength(streamer);
           var arr = new object[len];
           for(var i=0; i<arr.Length; i++)
           {
             if (atp.Value==DataType.Doc)
             {
               var has = streamer.ReadBool();
               if (!has) continue;
             }
             arr[i] = readOneAsObject(streamer, atp.Value);
           }
           value = arr;
         }
         else
         {
           value = readOneAsObject(streamer, dt);
         }
       }
       var arow = doc as IAmorphousData;
       if (arow==null) return value;
       if (!arow.AmorphousDataEnabled) return value;
       arow.AmorphousData[name] = value;
       return value;
    }

    private static JSON.JsonDataMap readRowAsMap(ReadingStreamer streamer)
    {
      var result = new JSON.JsonDataMap();
      while(true)
      {
        var name = ReadName(streamer);
        if (name==0) return result;
        var dt = ReadDataType(streamer);
        DataType? atp = null;
        if (dt==DataType.Array)
         atp = ReadDataType(streamer);
        var val = ConsumeUnmatched(null, streamer, null, dt, atp);
        result[CodeGenerator.GetName(name)] = val;
      }
    }

    private static object readOneAsObject(ReadingStreamer streamer, DataType dt)
    {
      switch(dt)
      {
        case DataType.Null: return null;
        case DataType.Doc:  return readRowAsMap(streamer);

        case DataType.Boolean     :  return ReadBoolean     (streamer);
        case DataType.Char        :  return ReadChar        (streamer);
        case DataType.String      :  return ReadString      (streamer);
        case DataType.Single      :  return ReadSingle      (streamer);
        case DataType.Double      :  return ReadDouble      (streamer);
        case DataType.Decimal     :  return ReadDecimal     (streamer);
        case DataType.Amount      :  return ReadAmount      (streamer);
        case DataType.Byte        :  return ReadByte        (streamer);
        case DataType.ByteArray   :  return streamer.ReadByteArray();
        case DataType.SByte       :  return ReadSByte       (streamer);
        case DataType.Int16       :  return ReadInt16       (streamer);
        case DataType.Int32       :  return ReadInt32       (streamer);
        case DataType.Int64       :  return ReadInt64       (streamer);
        case DataType.UInt16      :  return ReadUInt16      (streamer);
        case DataType.UInt32      :  return ReadUInt32      (streamer);
        case DataType.UInt64      :  return ReadUInt64      (streamer);
        case DataType.DateTime    :  return ReadDateTime    (streamer);
        case DataType.TimeSpan    :  return ReadTimeSpan    (streamer);
        case DataType.Guid        :  return ReadGuid        (streamer);
        case DataType.GDID        :  return ReadGDID        (streamer);
        case DataType.FID         :  return ReadFID         (streamer);
        case DataType.PilePointer :  return ReadPilePointer (streamer);
        case DataType.NLSMap      :  return ReadNLSMap      (streamer);
        case DataType.Atom        :  return ReadAtom        (streamer);
        default: throw new ArowException(StringConsts.AROW_DESER_CORRUPT_ERROR);
      }
    }

    [ MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static int ReadArrayLength(ReadingStreamer streamer)
    {
      var len = streamer.ReadInt();
      if (len > Writer.MAX_ARRAY_LENGTH) throw new ArowException(StringConsts.AROW_MAX_ARRAY_LEN_ERROR.Args(len));
      return len;
    }


    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static Boolean                                ReadBoolean      (ReadingStreamer streamer){ return streamer.ReadBool(); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static Char                                   ReadChar         (ReadingStreamer streamer){ return streamer.ReadChar(); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static String                                 ReadString       (ReadingStreamer streamer){ return streamer.ReadString (); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static Single                                 ReadSingle       (ReadingStreamer streamer){ return streamer.ReadFloat  (); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static Double                                 ReadDouble       (ReadingStreamer streamer){ return streamer.ReadDouble (); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static Decimal                                ReadDecimal      (ReadingStreamer streamer){ return streamer.ReadDecimal(); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static Financial.Amount                       ReadAmount       (ReadingStreamer streamer){ return streamer.ReadAmount(); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static Byte                                   ReadByte         (ReadingStreamer streamer){ return streamer.ReadByte     (); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static SByte                                  ReadSByte        (ReadingStreamer streamer){ return streamer.ReadSByte    (); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static Int16                                  ReadInt16        (ReadingStreamer streamer){ return streamer.ReadShort    (); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static Int32                                  ReadInt32        (ReadingStreamer streamer){ return streamer.ReadInt      (); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static Int64                                  ReadInt64        (ReadingStreamer streamer){ return streamer.ReadLong     (); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static UInt16                                 ReadUInt16       (ReadingStreamer streamer){ return streamer.ReadUShort   (); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static UInt32                                 ReadUInt32       (ReadingStreamer streamer){ return streamer.ReadUInt     (); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static UInt64                                 ReadUInt64       (ReadingStreamer streamer){ return streamer.ReadULong    (); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static DateTime                               ReadDateTime     (ReadingStreamer streamer){ return streamer.ReadDateTime (); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static TimeSpan                               ReadTimeSpan     (ReadingStreamer streamer){ return streamer.ReadTimeSpan (); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static Guid                                   ReadGuid         (ReadingStreamer streamer){ return streamer.ReadGuid     (); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static GDID                                   ReadGDID         (ReadingStreamer streamer){ return streamer.ReadGDID(); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static FID                                    ReadFID          (ReadingStreamer streamer){ return streamer.ReadFID();  }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static Pile.PilePointer                       ReadPilePointer  (ReadingStreamer streamer){ return streamer.ReadPilePointer(); }

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    public static JSON.NLSMap                            ReadNLSMap       (ReadingStreamer streamer){ return streamer.ReadNLSMap(); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Atom                                   ReadAtom         (ReadingStreamer streamer) { return streamer.ReadAtom(); }


    public static byte[] ReadByteArray(ReadingStreamer streamer)
    {
      var len = ReadArrayLength(streamer);
      var arr = new byte[len];
      for(var i=0; i<len; i++)
       arr[i] = streamer.ReadByte();  //todo  Why not use streamer.readByteArray?
      return arr;
    }



  }
}
