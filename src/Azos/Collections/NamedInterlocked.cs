/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Azos.Financial;

namespace Azos.Collections
{
  /// <summary>
  /// Provides functionality similar to the Interlocked class executed over a named slot.
  /// All operations are THREAD-SAFE for calling concurrently.
  /// The name comparison is that of Registry's which is OrdinalIgnoreCase.
  /// This class was designed to better organize named counters incremented from different threads,
  /// i.e. this is needed to keep a count of calls to remote host identified by their names.
  /// This class is NOT designed for frequent additions/deletions of named slots, nor was it designed
  /// to keep millions of slots. Use it in cases when there are thousands at most slots and new slots
  /// appear infrequently. You must delete unneeded slots
  /// </summary>
  public sealed class NamedInterlocked
  {
    private class slot : INamed
    {
      public slot(string name) { Name = name; }

      public string Name { get; }
      public long Long;
    }

    private Registry<slot> m_Data = new Registry<slot>();

    /// <summary>
    /// Returns the current number of named slots in the instance
    /// </summary>
    public int Count
    {
      get { return m_Data.Count;}
    }


    /// <summary>
    /// Enumerates all slot names. This operation is thread-safe, and returns a snapshot of the instance taken at the time of the first call
    /// </summary>
    public IEnumerable<string> AllNames
    {
      get { return m_Data.Names; }
    }

    /// <summary>
    /// Enumerates all named integers. This operation is thread-safe, and returns a snapshot of the instance taken at the time of the first call.
    /// If exchange is specified, atomically flips the value of every slot
    /// </summary>
    public IEnumerable<KeyValuePair<string, long>> SnapshotAllLongs(long? exchange = null)
    {
      if (exchange.HasValue)
        return m_Data.Select(s => new KeyValuePair<string, long>(s.Name, Interlocked.Exchange(ref s.Long, exchange.Value)));

      return m_Data.Select(s => new KeyValuePair<string, long>(s.Name, s.Long));
    }

    /// <summary>
    /// Enumerates all named amounts. This operation is thread-safe, and returns a snapshot of the instance taken at the time of the first call
    /// If exchange is specified, atomically flips the value of every slot
    /// </summary>
    public IEnumerable<KeyValuePair<string, Amount>> SnapshotAllAmounts(decimal? exchange = null)
    {
      if (exchange.HasValue)
      {
        var xchg = convertDecimalToLong(exchange.Value);
        return m_Data.Select(s => new KeyValuePair<string, Amount>(s.Name,  new Amount(s.Name, convertLongToDecimal(Interlocked.Exchange(ref s.Long, xchg)))) );
      }

      return m_Data.Select(s => new KeyValuePair<string, Amount>(s.Name, new Amount(s.Name, convertLongToDecimal(s.Long))));
    }

    /// <summary>
    /// Records a snapshot of all longs converted into TDatum. The TDatum must have a public .ctor(string, long) or runtime exception is thrown
    /// (this is because C# does not have a contract/constraint for parameterized constructors)
    /// </summary>
    public void SnapshotAllLongsInto<TDatum>(Instrumentation.IInstrumentation instrumentation, long? exchange = null) where TDatum : Instrumentation.Datum
    {
      if (instrumentation==null)
        throw new AzosException(StringConsts.ARGUMENT_ERROR+"{0}.{1}(instr==null)".Args(nameof(NamedInterlocked), nameof(SnapshotAllLongsInto)));

      foreach(var slot in SnapshotAllLongs(exchange))
      {
        var datum = (Instrumentation.Datum)Activator.CreateInstance(typeof(TDatum), slot.Key, slot.Value);
        instrumentation.Record( datum );
      }
    }


    /// <summary>
    /// Deletes all state for all slots
    /// </summary>
    public void Clear()
    {
      m_Data.Clear();
    }

    /// <summary>
    /// Deletes all state for the named slot returning true if the slot was found and removed
    /// </summary>
    public bool Clear(string name)
    {
      if (name==null) throw new AzosException(StringConsts.ARGUMENT_ERROR+GetType().FullName+".Clear(name==null)");
      return m_Data.Unregister( name );
    }

    /// <summary>
    /// Returns true when the instance contains the named slot
    /// </summary>
    public bool Exists(string name)
    {
      if (name==null) throw new AzosException(StringConsts.ARGUMENT_ERROR+GetType().FullName+".Exists(name==null)");
      return m_Data.ContainsName( name );
    }

    public long IncrementLong(string name)
    {
      var slot = getSlot(name);
      return Interlocked.Increment(ref slot.Long);
    }

    public long DecrementLong(string name)
    {
      var slot = getSlot(name);
      return Interlocked.Decrement(ref slot.Long);
    }

    public long AddLong(string name, long arg)
    {
      var slot = getSlot(name);
      return Interlocked.Add(ref slot.Long, arg);
    }

    public Amount Add(Amount arg)
    {
      var slot = getSlot(arg.ISO.Value);
      return new Amount(arg.ISO, Interlocked.Add(ref slot.Long, convertDecimalToLong(arg.Value)));
    }

    /// <summary>
    /// Adds slot snapshot from another source.
    /// Pass exchange to flip the existing slots to the exchanged value
    /// </summary>
    public void Add(NamedInterlocked source, long? exchangeSource = null)
    {
      if (source == null) return;
      var longs = source.SnapshotAllLongs(exchangeSource);
      foreach (var item in longs)
        AddLong(item.Key, item.Value);
    }

    /// <summary>
    ///  Returns a 64-bit value, loaded as an atomic operation even on a 32bit platform
    /// </summary>
    public long ReadAtomicLong(string name)
    {
      var slot = getSlot(name);
      return Interlocked.Read(ref slot.Long);
    }

    /// <summary>
    /// Captures the current value of a named long value.
    /// If slot does not exist, creates it and captures the value (which may be non-zero even if the slot was just created)
    /// </summary>
    public long VolatileReadLong(string name)
    {
      var slot = getSlot(name);
      return Thread.VolatileRead( ref slot.Long );
    }

    /// <summary>
    /// Captures the current value of a named long value and atomically sets it to the passed value
    /// </summary>
    public long ExchangeLong(string name, long value)
    {
      var slot = getSlot(name);
      return Interlocked.Exchange(ref slot.Long, value);
    }

    private slot getSlot(string name, [System.Runtime.CompilerServices.CallerMemberName]string opName = "")
    {
      if (name==null) throw new AzosException(StringConsts.ARGUMENT_ERROR + GetType().FullName + opName);
      return m_Data.GetOrRegister(name, (_) => new slot(name) , this);
    }

    private static long convertDecimalToLong(decimal val)
    {
      return (long)(val * 100);
    }

    private static decimal convertLongToDecimal(long val)
    {
      return val / 100M;
    }
  }

}
