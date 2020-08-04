﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Azos.Apps;
using Azos.Conf;
using Azos.Data;

namespace Azos.Serialization.Bix
{
  /// <summary>
  /// Provides "[BI]t-e[X]change" aka "BIX" serialization services for DAG of typed data document
  /// objects which are classes deriving from TypedDoc. Bix is a schema-change/version tolerant
  /// serialization mechanism supporting data doc polymorphism, arrays and lists.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Bix is a binary format conceptually somewhat similar to Protobuf, however it capitalizes on
  /// unified framework thus it does not require serialization-specific metadata/IDL as it uses
  /// existing Azos data documents as-is. The field bindings are done by name where field names are Atoms
  /// (up to 8 char ASCII monikers) represented as var bit encoded ULONGs, consequently short names take a few bytes.
  /// </para>
  /// <para>
  /// Bix utilizes IAmorphousData concept when it can not match the type/name on read, calling "AfterLoad" hook at the end
  /// of deserialization, this way it is possible to transparently upgrade existing/historical data to newer formats
  /// </para>
  /// <para>
  /// Bix performs polymorphic processing of TypeDoc derivatives but does not preserve the references, that is:
  /// if for example a root doc has two field pointing to the same child doc instance, then those would be serialized
  /// twice (and deserialized) as two independent objects. This design decision is done for simplicity and performance as
  /// reference preservation is rarely needed in practice. Should you need a normalized object graph with logical references
  /// preserved,consider shaping your datagram accordingly (e.g. use array indexes as pointers)
  /// </para>
  /// <para>
  /// While it is possible to inter-operate between other formats, Bix is built for high performance,
  /// feature richness and zero maintenance/simplicity as its code base is orders of magnitude smaller
  /// than that of Protobuf and other similar frameworks.
  /// </para>
  /// </remarks>
  public static class Bixer
  {
    public const string CONFIG_AZOS_SERIALIZATION_BIX_SECTION = "azos-serialization-bix";
    public const string CONFIG_ASSEMBLY_SECTION = "assembly";

    private static object s_Lock = new object();
    internal static volatile Dictionary<TargetedType, BixCore> s_Index = new Dictionary<TargetedType, BixCore>();
    private static GuidTypeResolver<TypedDoc, BixAttribute> s_Resolver = new GuidTypeResolver<TypedDoc, BixAttribute>();



    /// <summary>
    /// Returns a resolver which maps Guid/FastGuid to Type objects
    /// </summary>
    public static IGuidTypeResolver GuidTypeResolver => s_Resolver;

    /// <summary>
    /// Registers all IBixCore implementors from a satellite assembly of the calling assembly.
    /// A satellite assembly is the one that is called the same name as the calling one with
    /// an additional suffix appended at the end. If no specific suffix is passed, then ".Bixer" is used as default.
    /// A satellite assembly typically has its file co-located on disk with the specified assembly
    /// </summary>
    /// <example> Given "MyBusiness.dll" satellite is "MyBusiness.Bixer.dll" </example>
    public static void RegisterTypeSerializationSatelliteForThisAssembly(string suffix = null)
    {
      var thisAssembly = Assembly.GetCallingAssembly();
      RegisterTypeSerializationSatelliteFor(thisAssembly);
    }

    /// <summary>
    /// Registers all IBixCore implementors from a satellite assembly of the specified assembly.
    /// A satellite assembly is the one that is called the same name as the specified one with
    /// an additional suffix appended at the end. If no specific suffix is passed, then ".Bixer" is used as default.
    /// A satellite assembly typically has its file co-located on disk with the specified assembly
    /// </summary>
    /// <example> Given "MyBusiness.dll" satellite is "MyBusiness.Bixer.dll" </example>
    public static void RegisterTypeSerializationSatelliteFor(Assembly assembly, string suffix = null)
    {
      const string EXT = ".dll";
      const string BIXER_ASSEMBLY_SUFFIX = ".Bixer";

      var asmFileName = assembly.NonNull(nameof(assembly)).Location.Trim();
      var i = asmFileName.LastIndexOf(EXT);
      if (i != asmFileName.Length - EXT.Length)
        throw new BixException(StringConsts.BIX_SATELLITE_ASSEMBLY_NAME_ERROR.Args(asmFileName));

      asmFileName = asmFileName.Substring(0, i) + suffix.Default(BIXER_ASSEMBLY_SUFFIX);

      try
      {
        var asm = Assembly.LoadFrom(asmFileName);
        RegisterTypeSerializationCores(asm);
      }
      catch (Exception error)
      {
        throw new BixException(StringConsts.BIX_SATELLITE_ASSEMBLY_LOAD_ERROR.Args(asmFileName, error.ToMessageWithType()), error);
      }
    }

    /// <summary>
    /// Registers all BixCore type serialization handlers from the specified assembly so
    /// so they can be globally recognized for serialization of TypedDoc in Bix format
    /// </summary>
    public static void RegisterTypeSerializationCores(Assembly asm)
    {
      var allTypes = asm.NonNull(nameof(asm)).GetTypes();
      var allCores = allTypes.Where(t => t.IsClass && !t.IsAbstract && typeof(BixCore).IsAssignableFrom(t));
      var allDocs = allTypes.Where(t => t.IsClass && !t.IsAbstract && typeof(TypedDoc).IsAssignableFrom(t))
                            .Select(t => (t: t, a: GuidTypeAttribute.TryGetGuidTypeAttribute<TypedDoc, BixAttribute>(t)))
                            .Where( pair => pair.a!=null)
                            .Select( pair => (pair.a.TypeGuid, pair.t) );

      lock(s_Lock)
      {
        var dict = new Dictionary<TargetedType, BixCore>(s_Index);
        var toAdd = new List<(Guid, Type)>(allDocs);

        allCores.ForEach( tc => {
          var core = Activator.CreateInstance(tc) as BixCore;
          var ttp = core.TargetedType;
          dict[ttp] = core;
          toAdd.Add((core.Attribute.TypeGuid, ttp.Type));
        });

        s_Resolver.AddTypes(toAdd);

        System.Threading.Thread.MemoryBarrier();

        s_Index = dict;//atomic
      }
    }

    /// <summary>
    /// Registers BixCore so it can be globally recognized for serialization of TypedDoc in Bix format
    /// </summary>
    public static bool RegisterTypeSerializationCore(BixCore core)
    {
      var ttp = core.NonNull(nameof(core)).TargetedType;

      lock (s_Lock)
      {
        if (s_Index.TryGetValue(ttp, out var existing) && object.ReferenceEquals(existing, core)) return false;
        var dict = new Dictionary<TargetedType, BixCore>(s_Index);
        dict[ttp] = core;
        s_Resolver.AddTypes((core.Attribute.TypeGuid, ttp.Type).ToEnumerable());

        System.Threading.Thread.MemoryBarrier();

        s_Index = dict;//atomic
        return true;
      }
    }

    /// <summary>
    /// Registers assemblies from config section: r{ assembly{name='assembly1.dll'} assembly{name='assembly2.dll'} }
    /// </summary>
    public static void RegisterFromConfiguration(IConfigSectionNode conf)
    {
      if (conf==null || !conf.Exists) return;

      foreach (var nasm in conf.Children.Where(n => n.IsSameName(CONFIG_ASSEMBLY_SECTION)))
      {
        var asmName = nasm.AttrByName(Configuration.CONFIG_NAME_ATTR).Value;
        if (asmName.IsNullOrWhiteSpace()) continue;

        try
        {
          var asm = Assembly.LoadFrom(asmName);
          RegisterTypeSerializationCores(asm);
        }
        catch (Exception error)
        {
          throw new BixException(StringConsts.BIX_CONFIGURED_ASSEMBLY_LOAD_ERROR.Args(asmName, error.ToMessageWithType(), CONFIG_AZOS_SERIALIZATION_BIX_SECTION), error);
        }
      }
    }


    /// <summary>
    /// Serializes any object polymorphically
    /// </summary>
    public static void SerializeAny(BixWriter writer, object root, BixContext ctx = null)
    {
      if (!writer.IsAssigned) throw new BixException(StringConsts.ARGUMENT_ERROR+"{0}.!Assigned".Args(nameof(BixWriter)));

      if (ctx == null) ctx = BixContext.ObtainDefault();

      //1 Header
      if (ctx.HasHeader) Writer.WriteHeader(writer, ctx);

      //2 Payload
      Writer.WriteAny(writer, root, ctx);

      ctx.DisposeDefault();
    }

    /// <summary>
    /// Serializes data document
    /// </summary>
    public static void Serialize(BixWriter writer, TypedDoc root, BixContext ctx = null)
    {
      if (!writer.IsAssigned) throw new BixException(StringConsts.ARGUMENT_ERROR + "{0}.!Assigned".Args(nameof(BixWriter)));

      if (ctx == null) ctx = BixContext.ObtainDefault();

      //1 Header
      if (ctx.HasHeader) Writer.WriteHeader(writer, ctx);

      //2 Payload
      Writer.WriteDoc(writer, root, ctx, isRoot: true);

      ctx.DisposeDefault();
    }

    public static object DeserializeAny(BixReader reader, BixContext ctx = null)
    {
      return null;
    }

    public static T Deserialize<T>(BixReader reader, BixContext ctx = null) where T : TypedDoc
    {
      var (got, ok) = TryDeserializeRootOrInternal<T>(true, reader, ctx);

      if (!ok)
        throw new BixException(StringConsts.BIX_TYPE_NOT_SUPPORTED_ERROR.Args(typeof(T).FullName));

      return got;
    }

    internal static (T doc, bool ok) TryDeserializeRootOrInternal<T>(bool root, BixReader reader, BixContext ctx = null) where T : TypedDoc
    {
      if (ctx == null) ctx = BixContext.ObtainDefault();

      throw new NotImplementedException();

      ////1 Header
      //if (ctx.HasHeader) Reader.ReadHeader(reader);

      //TargetedType ttp;
      //if ((root && ctx.PolymorphicRoot) || (!root && ctx.PolymorphicFields))
      //{
      //  //read type identity from stream
      //  ttp = new TargetedType();//todo
      //}
      //else
      //{
      //  //take type identity from t
      //  ttp = new TargetedType(ctx.TargetName, typeof(T));
      //}

      //if (!s_Index.TryGetValue(ttp, out var core) || !(core is BixCore<T> coreT))
      //  return (null, false);

      //T doc = SerializationUtils.MakeNewObjectInstance(ttp.Type) as T;

      ////2 Body
      //coreT.Deserialize(doc, reader);

      //if (doc is IAmorphousData ad && ad.AmorphousDataEnabled)
      //{
      //  ad.AfterLoad(ctx.TargetName);
      //}

      //ctx.DisposeDefault();

      //return (doc, true);
    }

  }
}
