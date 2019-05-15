/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Collections.Generic;
using System.Text;

using Azos.Conf;
using Azos.Serialization.BSON;
using Azos.Data.Access.MongoDb.Connector;
using Azos.Log;

namespace Azos.Sky.Log.Server
{
  /// <summary>
  /// Implements Log Archive store using MongoDB
  /// </summary>
  public sealed class MongoLogArchiveStore : LogArchiveStore
  {
    public const string CONFIG_MONGO_SECTION = "mongo";
    public const string CONFIG_DEFAULT_CHANNEL_ATTR = "default-channel";

    public static readonly Atom DEFAULT_CHANNEL = Atom.Encode("archive");
    public const int DEFAULT_FETCHBY_SIZE = 32;
    public const int MAX_FETCHBY_SIZE = 4 * 1024;

    private static readonly BSONParentKnownTypes KNOWN_TYPES = new BSONParentKnownTypes(typeof(Message));

    public MongoLogArchiveStore(LogReceiverService director, LogArchiveDimensionsMapper mapper, IConfigSectionNode node) : base(director, mapper, node)
    {
      var cstring = ConfigStringBuilder.Build(node, CONFIG_MONGO_SECTION);
      m_Database = App.GetMongoDatabaseFromConnectString( cstring );
      m_DefaultChannel = node.AttrByName(CONFIG_DEFAULT_CHANNEL_ATTR).ValueAsAtom(DEFAULT_CHANNEL);
      m_Serializer = new BSONSerializer(node);
      m_Serializer.PKFieldName = Query._ID;
    }

    protected override void Destructor()
    {
      DisposeAndNull(ref m_Database);
      base.Destructor();
    }

    private BSONSerializer m_Serializer;
    private Database m_Database;
    private Atom m_DefaultChannel;
    private int m_FetchBy = DEFAULT_FETCHBY_SIZE;

    [Config(Default = DEFAULT_FETCHBY_SIZE)]
    public int FetchBy
    {
      get { return m_FetchBy; }
      private set
      {
        m_FetchBy = value < 1 ? 1 : value > MAX_FETCHBY_SIZE ? MAX_FETCHBY_SIZE : value;
      }

    }

    public override object BeginTransaction() { return null; }
    public override void CommitTransaction(object transaction) { }
    public override void RollbackTransaction(object transaction) { }

    public override void Put(Message message, object transaction)
    {
      if (Disposed) return;

      var channel = message.Channel;

      if (channel.IsZero)
        channel = m_DefaultChannel;

      var doc = m_Serializer.Serialize(message, KNOWN_TYPES);

      var map = Mapper.StoreMap(message.ArchiveDimensions);
      if (map != null)
      {
        foreach (var item in map)
          doc.Set(DataDocConverter.String_CLRtoBSON("__" + item.Key, item.Value));
      }

      m_Database[channel.Value].Insert(doc);
    }

    public override bool TryGetByID(Guid id, out Message message, Atom channel)
    {
      var query = new Query(
        @"{ '$query': { {0}: '$$id' } }".Args(m_Serializer.PKFieldName), true,
        new TemplateArg(new BSONBinaryElement("id", new BSONBinary(BSONBinaryType.UUID, id.ToByteArray()))));

      if (channel.IsZero)
        channel = m_DefaultChannel;

      message = new Message();
      var doc = m_Database[channel.Value].FindOne(query);
      if (doc == null) return false;

      m_Serializer.Deserialize(doc, message);
      return true;
    }

    public override IEnumerable<Message> List(Atom channel, string archiveDimensionsFilter, DateTime startDate, DateTime endDate, MessageType? type = null,
      string host = null, string topic = null, Guid? relatedTo = null, int skipCount = 0)
    {
      var map = Mapper.FilterMap(archiveDimensionsFilter);

      var query = buildQuery(channel, map, startDate, endDate, type, host, topic, relatedTo);

      if (channel.IsZero)
        channel = m_DefaultChannel;

      var collection = m_Database[channel.Value];
      var result = new List<Message>();

      using (var cursor = collection.Find(query, skipCount, FetchBy))
        foreach (var doc in cursor)
        {
          var message = new Message();
          m_Serializer.Deserialize(doc, message);
          result.Add(message);
        }

      return result;
    }

    private Query buildQuery(
      Atom channel,
      Dictionary<string, string> archiveDimensionsFilter,
      DateTime startDate, DateTime endDate, MessageType? type = null,
      string host = null, string topic = null,
      Guid? relatedTo = null)
    {
      var args = new List<TemplateArg>();
      var wb = new StringBuilder();

      wb.AppendFormat(@"{0}: {{ '$gte': '$$start_date', '$lt': '$$end_date' }}", Message.BSON_FLD_TIMESTAMP);
      args.Add(new TemplateArg("start_date", startDate));
      args.Add(new TemplateArg("end_date", endDate));

      if (archiveDimensionsFilter != null)
        foreach (var item in archiveDimensionsFilter)
        {
          wb.AppendFormat(", __{0}:'$${0}'", item.Key);
          args.Add(new TemplateArg(item.Key, item.Value));
        }

      if (type.HasValue)
      {
        wb.AppendFormat(", {0}:'$$type'", Message.BSON_FLD_TYPE);
        args.Add(new TemplateArg("type", type.Value));
      }

      if (relatedTo.HasValue)
      {
        wb.AppendFormat(", {0}:'$$related'", Message.BSON_FLD_RELATED_TO);
        args.Add(new TemplateArg("related", relatedTo.Value));
      }

      if (host.IsNotNullOrWhiteSpace())
      {
        wb.AppendFormat(", {0}:'$$host'", Message.BSON_FLD_HOST);
        args.Add(new TemplateArg("host", host));
      }

      if (topic.IsNotNullOrWhiteSpace())
      {
        wb.AppendFormat(", {0}:'$$host'", Message.BSON_FLD_TOPIC);
        args.Add(new TemplateArg("topic", topic));
      }

      var where = "$query: { " + wb.ToString() + "},";
      return new Query(@"{{ {0} $orderby: {{ {1}:1 }} }}".Args(where, Message.BSON_FLD_TIMESTAMP), true, args.ToArray());
    }
  }
}
