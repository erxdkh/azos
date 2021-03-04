/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;

using Azos.Apps;
using Azos.Collections;
using Azos.Instrumentation;

namespace Azos.Pile
{
  /// <summary>
  /// Represents a cache of expiring objects, which are identified by a key and stored in a pile.
  /// Pile allows to store hundreds of millions of objects without overloading the managed GC.
  /// The cache may be local or distributed
  /// </summary>
  public interface ICache
  {
    /// <summary>
    /// Returns whether the cache key:value mappings are local or distributed
    /// </summary>
    LocalityKind Locality { get; }

    /// <summary>
    /// Returns the model of key:value mapping persistence that this cache supports
    /// </summary>
    ObjectPersistence Persistence { get; }

    /// <summary>
    /// Returns the status of the pile where object are stored
    /// </summary>
    IPileStatus PileStatus { get; }


    /// <summary>
    /// Tables that this cache contains
    /// </summary>
    IRegistry<ICacheTable> Tables { get; }

    /// <summary>
    /// Returns existing table by name, if it does not exist creates a new table.
    /// For existing table the types must be identical to the ones used at creation
    /// </summary>
    ICacheTable<TKey> GetOrCreateTable<TKey>(string tableName, IEqualityComparer<TKey> keyComparer = null);

    /// <summary>
    /// Returns existing table by name, if it does not exist creates a new table.
    /// For existing table the types must be identical to the ones used at creation
    /// </summary>
    ICacheTable<TKey> GetOrCreateTable<TKey>(string tableName, out bool createdNew, IEqualityComparer<TKey> keyComparer = null);

    /// <summary>
    /// Returns existing table by name, if it does not exist throws.
    /// The TKey must correspond to existing table
    /// </summary>
    ICacheTable<TKey> GetTable<TKey>(string tableName);


    /// <summary>
    /// Returns how many records are kept in cache
    /// </summary>
    long Count{ get; }


    /// <summary>
    /// Removes all data from all tables stored in the cache
    /// </summary>
    void PurgeAll();
  }


  /// <summary>
  /// Defines table collision modes: Speculative (probability-based - ignores collisions) vs Durable (works slower but handles collisions)
  /// </summary>
  public enum CollisionMode
  {
    /// <summary>
    /// Does not do chaining/rehashing, works faster for caches but may overwrite data with lower priority
    /// </summary>
    Speculative = 0,

    /// <summary>
    /// Guarantees to store all the data and handles all key collisions at the expense of extra processing.
    /// Ignores priorities as they are irrelevant
    /// </summary>
    Durable
  }


  public interface ICacheTable : INamed
  {
    /// <summary>
    /// References cache that this table is under
    /// </summary>
    ICache Cache{ get;}

    /// <summary>
    /// Returns how many records are kept in a table
    /// </summary>
    long Count{get;}

    /// <summary>
    /// Returns how many slots/entries allocated in a table
    /// </summary>
    long Capacity{get;}

    /// <summary>
    /// Returns the percentage of occupied table slots.
    /// When this number exceeds high-water-mark threshold the table is grown,
    /// otherwise if the number falls below the low-water-mark level then the table is shrunk
    /// </summary>
    double LoadFactor{ get;}

    /// <summary>
    /// Cache table options in effect
    /// </summary>
    TableOptions Options { get;}


    /// <summary>
    /// Determines how this instance handles collisions.
    /// This property can not be changed after the table was created from TableOptions.
    /// Changing CollisionMode in TableOptions has no effect on this property after creation
    /// </summary>
    CollisionMode CollisionMode{ get;}

    /// <summary>
    /// Removes all data from table
    /// </summary>
    void Purge();
  }

  /// <summary>
  /// Results of cache table Put() operation: Collision | Inserted | Replaced | Overwritten
  /// </summary>
  public enum PutResult
  {
    /// <summary>
    /// The item could not be put because it collides with existing data
    /// that can not be overwritten because it has higher priority and there is no extra space
    /// </summary>
    Collision = 0,

    /// <summary>
    /// The item was inserted into cache table anew
    /// </summary>
    Inserted,

    /// <summary>
    /// The item replaced an existing item with the same key
    /// </summary>
    Replaced,

    /// <summary>
    /// The item was inserted instead of an existing item with lower or equal priority
    /// </summary>
    Overwritten
  }


  /// <summary>
  /// Provides information about the item stored in cache
  /// </summary>
  public interface ICacheEntry<TKey>
  {
    TKey         Key           { get; }
    int          AgeSec        { get; }
    int          Priority      { get; }
    int          MaxAgeSec     { get; }
    DateTime?    ExpirationUTC { get; }

    /// <summary>
    /// Returns value only if enumerator is in materializing mode, obtained by a call to AsEnumerable(withValues: true)
    /// </summary>
    object  Value   { get; }
  }


  public interface ICacheTable<TKey> : ICacheTable
  {
    /// <summary>
    /// Returns equality comparer for keys, or null to use default Equals
    /// </summary>
    IEqualityComparer<TKey> KeyComparer{ get ;}

    /// <summary>
    /// Returns the table as enumerable of entries with optional materialization
    /// of values (which is slower). Materialization is guaranteed to be consistent with the key
    /// </summary>
    /// <param name="withValues">True, to materialize internal PilePointers into CLR objects</param>
    IEnumerable<ICacheEntry<TKey>> AsEnumerable(bool withValues);

    /// <summary>
    /// Returns true if cache has object with the key, optionally filtering-out objects older than ageSec param if it is &gt; zero.
    /// Returns false if there is no object with the specified key or it is older than ageSec limit.
    /// </summary>
    bool ContainsKey(TKey key, int ageSec = 0);


    /// <summary>
    /// Returns the size of stored object if cache has object with the key, optionally filtering-out objects older than ageSec param if it is &gt; zero.
    /// </summary>
    long SizeOfValue(TKey key, int ageSec = 0);

    /// <summary>
    /// Gets cache object by key, optionally filtering-out objects older than ageSec param if it is &gt; zero.
    /// Returns null if there is no object with the specified key or it is older than ageSec limit.
    /// </summary>
    object Get(TKey key, int ageSec = 0);

    /// <summary>
    /// Gets cache object pointer by key, optionally filtering-out objects older than ageSec param if it is &gt; zero.
    /// Returns invalid pointer if there is no object with the specified key or it is older than ageSec limit.
    /// </summary>
    PilePointer GetPointer(TKey key, int ageSec = 0);

    /// <summary>
    /// Puts an object identified by a key into cache returning the result of the put.
    /// For example, put may have added nothing if the table is capped and the space is occupied with data of higher priority
    /// </summary>
    /// <param name="key">A table-wide unique object key</param>
    /// <param name="obj">An object to put</param>
    /// <param name="maxAgeSec">If null then the default maxAgeSec is taken from Options property, otherwise specifies the length of items life in seconds</param>
    /// <param name="priority">The priority of this item. If there is no space in future the items with lower priorities will not evict existing data with higher priorities</param>
    /// <param name="absoluteExpirationUTC">Optional UTC timestamp of object eviction from cache</param>
    /// <returns>The status of put - whether item was inserted/replaced(if key exists)/overwritten or collided with higher-prioritized existing data</returns>
    PutResult Put(TKey key, object obj, int? maxAgeSec = null, int priority = 0, DateTime? absoluteExpirationUTC = null);

    /// <summary>
    /// Puts an object identified by a key into cache returning the result of the put and the PilePointrer at which the value was stored.
    /// For example, put may have added nothing if the table is capped and the space is occupied with data of higher priority
    /// in which case an invalid pointer is returned along with corresponding PutResult
    /// </summary>
    /// <param name="key">A table-wide unique object key</param>
    /// <param name="obj">An object to put</param>
    /// <param name="ptr">Returns a pointer at which the value was put.  Note: the cache assumes ownership of pointer, so once the key is deleted, the pointed-to value is deleted automatically</param>
    /// <param name="maxAgeSec">If null then the default maxAgeSec is taken from Options property, otherwise specifies the length of items life in seconds</param>
    /// <param name="priority">The priority of this item. If there is no space in future the items with lower priorities will not evict existing data with higher priorities</param>
    /// <param name="absoluteExpirationUTC">Optional UTC timestamp of object eviction from cache</param>
    /// <returns>The status of put - whether item was inserted/replaced(if key exists)/overwritten or collided with higher-prioritized existing data</returns>
    PutResult Put(TKey key, object obj, out PilePointer ptr,  int? maxAgeSec = null, int priority = 0, DateTime? absoluteExpirationUTC = null);


    /// <summary>
    /// Puts a pointer identified by a key into cache returning the result of the put.
    /// For example, put may have added nothing if the table is capped and the space is occupied with data of higher priority.
    /// Note: the cache assumes ownership of pointer, so once the key is deleted, the pointed-to value is deleted automatically
    /// </summary>
    /// <param name="key">A table-wide unique object key</param>
    /// <param name="ptr">A valid pointer to put</param>
    /// <param name="maxAgeSec">If null then the default maxAgeSec is taken from Options property, otherwise specifies the length of items life in seconds</param>
    /// <param name="priority">The priority of this item. If there is no space in future the items with lower priorities will not evict existing data with higher priorities</param>
    /// <param name="absoluteExpirationUTC">Optional UTC timestamp of object eviction from cache</param>
    /// <returns>The status of put - whether item was inserted/replaced(if key exists)/overwritten or collided with higher-prioritized existing data</returns>
    PutResult PutPointer(TKey key, PilePointer ptr, int? maxAgeSec = null, int priority = 0, DateTime? absoluteExpirationUTC = null);



    /// <summary>
    /// Removes data by key returning true if found and removed
    /// </summary>
    bool Remove(TKey key);

    /// <summary>
    /// Resets internal object age returning true of object was found and rejuvenated
    /// </summary>
    bool Rejuvenate(TKey key);


    /// <summary>
    /// Atomically tries to get object by key if it exists, otherwise calls a factory method under lock and puts the data with the specified parameters.
    /// 'newPutResult' returns the result of the put after factory method call.
    /// Keep in mind, that even if a factory method created a new object, there may be a case when the value
    /// could not be physically inserted in the cache because of a collision (data with higher priority occupies space and space is capped), so check for
    /// 'newPutResult' value which is null in case of getting an existing item.
    /// Returns object that was gotten or created anew
    /// </summary>
    object GetOrPut(TKey key,
                  Func<ICacheTable<TKey>, TKey, object, object> valueFactory,
                  object factoryContext,
                  out PutResult? newPutResult,
                  int ageSec = 0,
                  int putMaxAgeSec = 0,
                  int putPriority = 0,
                  DateTime? putAbsoluteExpirationUTC = null);
  }

  public interface ICacheImplementation : ICache, IApplicationComponent, IDaemon, IInstrumentable
  {
    /// <summary>
    /// Imposes a limit on maximum number of bytes that a pile can allocate of the system heap.
    /// The default value of 0 means no limit, meaning - the pile will keep allocating objects
    /// until the system allows. If the limit is reached, then the cache will start deleting
    /// older objects to relieve the memory load even if they are not due for expiration yet
    /// </summary>
    long PileMaxMemoryLimit { get; set;}

    /// <summary>
    /// Defines modes of allocation: space/time trade-off
    /// </summary>
    AllocationMode PileAllocMode{ get; set;}


    /// <summary>
    /// Returns table options - used for table creation
    /// </summary>
    Registry<TableOptions> TableOptions { get; }

    /// <summary>
    /// Sets default options for a table which is not found in TableOptions collection.
    /// If this property is null then every table assumes the set of constant values defined in Table class
    /// </summary>
    TableOptions DefaultTableOptions { get; set;}
  }


}
