/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;

using Azos.Instrumentation;
using Azos.Serialization.Arow;
using Azos.Serialization.BSON;

namespace Azos.Pile.Instrumentation
{
  /// <summary>
  /// Provides base for cache long gauges
  /// </summary>
  [Serializable]
  public abstract class CacheLongGauge : PileLongGauge
  {
    protected CacheLongGauge(string src, long value) : base(src, value) { }
  }

  /// <summary>
  /// Provides base for cache double gauges
  /// </summary>
  [Serializable]
  public abstract class CacheDoubleGauge : PileDoubleGauge
  {
    protected CacheDoubleGauge(string src, double value) : base(src, value) { }
  }

  /// <summary>
  /// Provides base for cache events
  /// </summary>
  [Serializable]
  public abstract class CacheEvent : Event
  {
    protected CacheEvent(string src) : base(src) { }
  }

  /// <summary>
  /// Provides table count in the cache instance
  /// </summary>
  [Serializable]
  [Arow("5E4EF192-9C9C-4879-8771-2C49ACF7995F")]
  [BSONSerializable("1C1E2248-0665-425B-8788-33A604BDB9C8")]
  public class CacheTableCount : CacheLongGauge
  {
    internal CacheTableCount(string src, long value) : base(src, value) { }

    public override string Description { get { return "Provides table count in the cache instance"; } }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_TABLE; } }

    protected override Datum MakeAggregateInstance() { return new CacheTableCount(this.Source, 0); }
  }

  /// <summary>
  /// Provides object count in the cache instance
  /// </summary>
  [Serializable]
  [Arow("33EDF299-C205-43C4-A495-CC0DC4AD0C58")]
  [BSONSerializable("70E4120E-6EC8-46C0-B60D-59D11F4ED903")]
  public class CacheCount : CacheLongGauge
  {
    internal CacheCount(string src, long value) : base(src, value) { }

    public override string Description { get { return "Provides object count in the cache instance"; } }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_OBJECT; } }

    protected override Datum MakeAggregateInstance() { return new CacheCount(this.Source, 0); }
  }

  /// <summary>
  /// Provides entry/slot count in the cache instance
  /// </summary>
  [Serializable]
  [Arow("D9A9A5FC-F088-4B65-A0F3-1FC341CFA846")]
  [BSONSerializable("CEE6A3C2-B876-4171-8B10-A70456D2F95A")]
  public class CacheCapacity : CacheLongGauge
  {
    internal CacheCapacity(string src, long value) : base(src, value) { }

    public override string Description { get { return "Provides entry/slot count in the cache instance"; } }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_OBJECT; } }

    protected override Datum MakeAggregateInstance() { return new CacheCapacity(this.Source, 0); }
  }

  /// <summary>
  /// Provides load factor percentage
  /// </summary>
  [Serializable]
  [Arow("7C6A96C0-69BD-451A-803E-D4EA95F64B66")]
  [BSONSerializable("8FF1C20B-3A23-4CE7-92F6-116C366FB59D")]
  public class CacheLoadFactor : CacheDoubleGauge
  {
    internal CacheLoadFactor(string src, double value) : base(src, value) { }

    public override string Description { get { return "Provides object count in the cache instance"; } }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_PERCENT; } }

    protected override Datum MakeAggregateInstance() { return new CacheLoadFactor(this.Source, 0d); }
  }

  /// <summary>
  /// How many times put resulted in new object insertion in cache with or without overwriting the existing item
  /// </summary>
  [Serializable]
  [Arow("682F1B8C-BFD8-4DC4-81BB-ABC8420F1DB1")]
  [BSONSerializable("F98541C5-9C2C-487D-8B2F-B8916CA914D3")]
  public class CachePut : CacheLongGauge
  {
    internal CachePut(string src, long value) : base(src, value) { }

    public override string Description { get { return "How many times put resulted in new object insertion in cache with or without overwriting the existing item"; } }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_TIME; } }

    protected override Datum MakeAggregateInstance() { return new CachePut(this.Source, 0); }
  }

  /// <summary>
  /// How many times put could not insert new object in cache because there was no room and existing data could not be overwritten
  ///  due to higher priority
  /// </summary>
  [Serializable]
  [Arow("ABA17C2C-BBCE-42EF-B662-254ACDC57BAF")]
  [BSONSerializable("5541FEF0-FCB2-4C6E-9FCB-1BC7FE00441C")]
  public class CachePutCollision : CacheLongGauge
  {
    internal CachePutCollision(string src, long value) : base(src, value) { }

    public override string Description
    {
      get { return "How many times put could not insert new object in cache because there was no room and existing data could not be overwritten due to higher priority"; }
    }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_TIME; } }

    protected override Datum MakeAggregateInstance() { return new CachePutCollision(this.Source, 0); }
  }

  /// <summary>
  /// How many times put inserted new object in cache by overwriting existing value with lower priority
  /// </summary>
  [Serializable]
  [Arow("FB086220-7F84-4639-B0BB-F315374BD28F")]
  [BSONSerializable("85B3755A-3C0D-4079-8E8C-01B10A3F0613")]
  public class CachePutOverwrite : CacheLongGauge
  {
    internal CachePutOverwrite(string src, long value) : base(src, value) { }

    public override string Description
    {
      get { return "How many times put inserted new object in cache by overwriting existing value with lower priority"; }
    }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_TIME; } }

    protected override Datum MakeAggregateInstance() { return new CachePutOverwrite(this.Source, 0); }
  }

  /// <summary>
  /// How many times put replaced existing object in cache
  /// </summary>
  [Serializable]
  [Arow("3F3ECFCD-5905-4007-901B-21819C352BA5")]
  [BSONSerializable("E73361E0-DCD3-4557-A44B-E89F5EB9733A")]
  public class CachePutReplace : CacheLongGauge
  {
    internal CachePutReplace(string src, long value) : base(src, value) { }

    public override string Description
    {
      get { return "How many times put replaced existing object in cache"; }
    }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_TIME; } }

    protected override Datum MakeAggregateInstance() { return new CachePutReplace(this.Source, 0); }
  }

  /// <summary>
  /// How many times key was found and object removed
  /// </summary>
  [Serializable]
  [Arow("D9CC2154-2476-4D91-AEBD-4233A3203B47")]
  [BSONSerializable("17A730D2-E409-4DDB-8979-E8DE913D72E2")]
  public class CacheRemoveHit : CacheLongGauge
  {
    internal CacheRemoveHit(string src, long value) : base(src, value) { }

    public override string Description
    {
      get { return "How many times key was found and object removed"; }
    }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_TIME; } }

    protected override Datum MakeAggregateInstance() { return new CacheRemoveHit(this.Source, 0); }
  }

  /// <summary>
  /// How many times key was not found and object not removed
  /// </summary>
  [Serializable]
  [Arow("9734117D-E43A-4B1E-98F5-340C2454AC62")]
  [BSONSerializable("88DE0E76-475C-41DE-961F-E735737B1F00")]
  public class CacheRemoveMiss : CacheLongGauge
  {
    internal CacheRemoveMiss(string src, long value) : base(src, value) { }

    public override string Description
    {
      get { return " How many times key was not found and object not removed"; }
    }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_TIME; } }

    protected override Datum MakeAggregateInstance() { return new CacheRemoveMiss(this.Source, 0); }
  }


  /// <summary>
  /// How many entries/objects were removed by sweep
  /// </summary>
  [Serializable]
  [Arow("2E0B1DA5-0F92-4E04-ACBF-F1D58AF43ABC")]
  [BSONSerializable("DCDEE4A4-BE23-4B2E-BE9C-9D3F970ECDC9")]
  public class CacheSweep : CacheLongGauge
  {
    internal CacheSweep(string src, long value) : base(src, value) { }

    public override string Description { get { return " How many slots/objects were removed by sweep"; } }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_OBJECT; } }

    protected override Datum MakeAggregateInstance() { return new CacheSweep(this.Source, 0); }
  }

  /// <summary>
  /// How long the sweeping took (examination + removal of expired)
  /// </summary>
  [Serializable]
  [Arow("0B16D43F-6BE9-4EA2-AECA-7B853C7B6512")]
  [BSONSerializable("FD0C832E-0FC5-46A3-BBA3-5689E8004A34")]
  public class CacheSweepDuration : CacheLongGauge
  {
    internal CacheSweepDuration(string src, long value) : base(src, value) { }

    public override string Description { get { return "How long the sweeping took (examination + removal of expired)"; } }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_MSEC; } }

    protected override Datum MakeAggregateInstance() { return new CacheSweepDuration(this.Source, 0); }
  }


  /// <summary>
  /// Cache table was swept
  /// </summary>
  [Serializable]
  [Arow("DAF6F0E2-2999-4CE7-958D-59DF6F37BA77")]
  [BSONSerializable("6360EF29-C14D-4CCD-81A1-433F3F533A0B")]
  public class CacheTableSwept : CacheEvent
  {
    protected CacheTableSwept(string src) : base(src) { }

    public static void Happened(IInstrumentation inst, string tableName)
    {
      if (inst!=null && inst.Enabled)
        inst.Record(new CacheTableSwept(tableName));
    }

    public override string Description { get { return "Cache table was swept"; } }

    protected override Datum MakeAggregateInstance() { return new CacheTableSwept(this.Source); }
  }


  /// <summary>
  /// How many times key entry was found and its age reset to zero
  /// </summary>
  [Serializable]
  [Arow("F63939D9-E518-4A90-A9C8-E407AB70F921")]
  [BSONSerializable("A322FCA5-C07C-499D-AF9C-96EC7E7857A6")]
  public class CacheRejuvenateHit : CacheLongGauge
  {
    internal CacheRejuvenateHit(string src, long value) : base(src, value) { }

    public override string Description
    {
      get { return "How many times key entry was found and its age reset to zero"; }
    }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_TIME; } }

    protected override Datum MakeAggregateInstance() { return new CacheRejuvenateHit(this.Source, 0); }
  }

  /// <summary>
  /// How many times key entry was not found for resetting its age
  /// </summary>
  [Serializable]
  [Arow("778DB876-2D3D-4E52-AF06-46AE1F39CB91")]
  [BSONSerializable("19BC6E0B-AFC8-4EE5-A077-1F5C414C5035")]
  public class CacheRejuvenateMiss : CacheLongGauge
  {
    internal CacheRejuvenateMiss(string src, long value) : base(src, value) { }

    public override string Description
    {
      get { return "How many times key entry was not found for resetting its age"; }
    }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_TIME; } }

    protected override Datum MakeAggregateInstance() { return new CacheRejuvenateMiss(this.Source, 0); }
  }


  /// <summary>
  /// How many times cached object was found and gotten by its key
  /// </summary>
  [Serializable]
  [Arow("D0D8347B-E47D-42D9-8B2D-724EFB826230")]
  [BSONSerializable("87AFAACD-6526-4E54-811C-4AC616FEE408")]
  public class CacheGetHit : CacheLongGauge
  {
    internal CacheGetHit(string src, long value) : base(src, value) { }

    public override string Description
    {
      get { return "How many times cached object was found and gotten by its key"; }
    }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_TIME; } }

    protected override Datum MakeAggregateInstance() { return new CacheGetHit(this.Source, 0); }
  }

  /// <summary>
  /// How many times cached object was tried to be gotten but not found by its key
  /// </summary>
  [Serializable]
  [Arow("00E0CDB0-E16E-4AA6-A3DE-D2BB6C11DB63")]
  [BSONSerializable("24D12137-96AD-4428-B8B8-A6B58EE10EE9")]
  public class CacheGetMiss : CacheLongGauge
  {
    internal CacheGetMiss(string src, long value) : base(src, value) { }

    public override string Description
    {
      get { return "How many times cached object was tried to be gotten but not found by its key"; }
    }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_TIME; } }

    protected override Datum MakeAggregateInstance() { return new CacheGetMiss(this.Source, 0); }
  }


  /// <summary>
  /// How many times cache has to increase its capacity
  /// </summary>
  [Serializable]
  [Arow("649CC1A6-F2B1-474C-AA41-BCC9A9ACA56E")]
  [BSONSerializable("EB07D59D-67FB-4AF8-8311-4299F66B5749")]
  public class CacheGrew : CacheLongGauge
  {
    internal CacheGrew(string src, long value) : base(src, value) { }

    public override string Description
    {
      get { return "How many times cache has to increase its capacity"; }
    }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_TIME; } }

    protected override Datum MakeAggregateInstance() { return new CacheGrew(this.Source, 0); }
  }

  /// <summary>
  /// How many times cache has to decrease its capacity
  /// </summary>
  [Serializable]
  [Arow("2F0C118B-A94D-430C-B1EC-CED55C0927FA")]
  [BSONSerializable("F9474DA5-9E5F-463E-AAD3-8F9A2629B2BD")]
  public class CacheShrunk : CacheLongGauge
  {
    internal CacheShrunk(string src, long value) : base(src, value) { }

    public override string Description
    {
      get { return "How many times cache has to decrease its capacity"; }
    }

    public override string ValueUnitName { get { return CoreConsts.UNIT_NAME_TIME; } }

    protected override Datum MakeAggregateInstance() { return new CacheShrunk(this.Source, 0); }
  }
}
