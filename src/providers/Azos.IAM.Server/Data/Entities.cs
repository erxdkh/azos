﻿using System;

using Azos.Conf;
using Azos.Data;
using Azos.Serialization.JSON;
using Azos.IAM.Protocol;

namespace Azos.IAM.Server.Data
{
  /// <summary>
  /// Common base type for IAM server data
  /// </summary>
  public abstract class BaseDoc : AmorphousTypedDoc
  {
    public const string TMONGO = "mongo";
  }

  /// <summary>
  /// Represents a period - a span of time. Both dates are required, use min/max date values for open spans
  /// </summary>
  public class TimePeriod : BaseDoc
  {
    [Field(required: true,
           minLength: Sizes.NAME_MIN,
           maxLength: Sizes.NAME_MAX,
           description: "Provides a short name for this period of time, e.g. 'Christmas 2018'")]
    [Field(typeof(TimePeriod), nameof(Name), TMONGO, backendName: "nm")]
    public string Name { get; set; }

    [Field(required: true, description: "Period start timestamp in UTC")]
    [Field(typeof(TimePeriod), nameof(SD), TMONGO, backendName: "sd")]
    public DateTime? SD { get; set; }

    [Field(required: true, description: "Period end timestamp in UTC")]
    [Field(typeof(TimePeriod), nameof(ED), TMONGO, backendName: "ed")]
    public DateTime? ED { get; set; }
  }

  /// <summary>
  /// Provides base for replicating data entities
  /// </summary>
  public abstract class Entity : BaseDoc
  {
    [Field(key: true, required: true, description: "Primary key which identifies this entity")]
    [Field(typeof(Entity), nameof(GDID), TMONGO, backendName: "_id")]
    public GDID GDID{ get; set;}

    [Field(required: true, description: "Version time stamp", metadata: "idx{name='vts' order='0' dir=desc}")]
    [Field(typeof(Entity), nameof(VersionTimestamp), TMONGO, backendName: "_vt")]
    public DateTime? VersionTimestamp { get; set; }

    [Field(required: true, description: "Version status", valueList: ValueLists.ENTITY_VERSION_STATUS_VALUE_LIST)]
    [Field(typeof(Entity), nameof(VersionStatus), TMONGO, backendName: "_vs")]
    public char? VersionStatus {  get; set; }

    [Field(required: true, description: "Actor/User who authored the version")]//not indexed, use Audit for searches instead
    [Field(typeof(Entity), nameof(G_VersionActor), TMONGO, backendName: "_va")]
    public GDID G_VersionActor { get; set; }
  }


  /// <summary>
  /// Provides base for entities with properties
  /// </summary>
  public abstract class EntityWithProperties : Entity
  {
    [Field(
        maxLength: Sizes.DESCRIPTION_MAX,
        description: "Provides an optional description")]
    [Field(typeof(EntityWithProperties), nameof(Description), TMONGO, backendName: "d")]
    public string Description { get; set; }

    [Field(required: false,
           maxLength: Sizes.PROPERTY_COUNT_MAX,
           description: "Properties")]
    [Field(typeof(EntityWithProperties), nameof(PropertyData), TMONGO, backendName: "props")]
    public JsonDataMap PropertyData { get; set; }//JsonDataMap is used because it is supported by all ser frameworks, but we only store strings

    public override ValidState Validate(ValidState state, string scope = null)
    {
      state = base.Validate(state, scope);
      if (state.ShouldStop) return state;

      if (PropertyData==null) return state;

      foreach(var kvp in PropertyData)
      {
        if (kvp.Key.Length > Sizes.PROPERTY_NAME_MAX)
          state = new ValidState(state, new FieldValidationException(this, kvp.Key.TakeLastChars(32, ".."),
                            StringConsts.ENTITY_PROPERTY_NAME_LONG_ERROR.Args(Sizes.PROPERTY_NAME_MAX)));

        if (kvp.Value != null && kvp.Value.AsString().Length > Sizes.PROPERTY_VALUE_MAX)
          state = new ValidState(state, new FieldValidationException(this, kvp.Key.TakeLastChars(32, ".."),
                            StringConsts.ENTITY_PROPERTY_VALUE_LONG_ERROR.Args(Sizes.PROPERTY_VALUE_MAX)));

        if (state.ShouldStop) break;
      }

      return state;
    }

  }

  /// <summary>
  /// Represents entity with access rights
  /// </summary>
  public abstract class EntityWithRights : EntityWithProperties
  {
    private string m_RightsData;

    [NonSerialized]
    private ConfigSectionNode m_Rights;

    /// <summary>
    /// The Audit* family of columns provide the minimum history in case of Audit log deletion/purge
    /// </summary>
    [Field(required: true, description: "Captures the original create date. This value never changes after that")]
    [Field(typeof(EntityWithRights), nameof(AuditCreateDate), TMONGO, backendName: "a_cd")]
    public DateTime? AuditCreateDate           { get; set; }

    /// <summary>
    /// The Audit* family of columns provide the minimum history in case of Audit log deletion/purge
    /// </summary>
    [Field(required: true,
           maxLength: Sizes.ACCOUNT_TITLE_MAX,
           description: "Captures the actor/user account who created this entity. This value never changes after that")]
    [Field(typeof(EntityWithRights), nameof(AuditCreateActorTitle), TMONGO, backendName: "a_ca")]
    public string    AuditCreateActorTitle { get; set; }

    /// <summary>
    /// The Audit* family of columns provide the minimum history in case of Audit log deletion/purge
    /// </summary>
    [Field(required: true, description: "Captures the last modification date")]
    [Field(typeof(EntityWithRights), nameof(AuditLastModifyDate), TMONGO, backendName: "a_lmd")]
    public DateTime? AuditLastModifyDate { get; set; }

    /// <summary>
    /// The Audit* family of columns provide the minimum history in case of Audit log deletion/purge
    /// </summary>
    [Field(required: true,
           maxLength: Sizes.ACCOUNT_TITLE_MAX,
           description: "Captures the actor/user account title who has performed the last modifications")]
    [Field(typeof(EntityWithRights), nameof(AuditLastModifyActorTitle), TMONGO, backendName: "a_lma")]
    public string    AuditLastModifyActorTitle { get; set; }


    [Field(required: true, description: "Contains a list of time periods during which this entity is valid. An entity is considered as invalid/non-existent one outside of these time spans")]
    [Field(typeof(EntityWithRights), nameof(ValidPeriods), TMONGO, backendName: "val")]
    public TimePeriod[] ValidPeriods { get; set; }

    /// <summary>
    /// The locking has different effect on different entities: a locked role just does not get mixed-in as if it never existed.
    /// Locked group disallows any access to any entities directly or indirectly under it.
    /// Locked Account disables all logins, and locked login disables just that login
    /// </summary>
    [Field(description: "Optional temporary lock UTC timestamp, beyond which  an entity is considered to be invalid")]
    [Field(typeof(EntityWithRights), nameof(LockDate), TMONGO, backendName: "lckd")]
    public DateTime? LockDate { get; set; }

    /// <summary>
    /// If this date is specified and the the current UTC is over this date then Lock considers to be auto-lifted
    /// </summary>
    [Field(description: "If this date is specified and the the current UTC is over this date then Lock considers to be auto-lifted")]
    [Field(typeof(EntityWithRights), nameof(LockAutoResetDate), TMONGO, backendName: "lckard")]
    public DateTime? LockAutoResetDate { get; set; }

    [Field(maxLength: Sizes.NOTE_MAX,
           description: "Optional note associated with optional temporary lock timestamp. A note may contain a reason why entity is locked-out")]
    [Field(typeof(EntityWithRights), nameof(LockNote), TMONGO, backendName: "lckn")]
    public string    LockNote { get; set; }


    [Field(required: false,
           maxLength: Sizes.RIGHTS_DATA_MAX,
           description: "Access rights")]
    [Field(typeof(EntityWithRights), nameof(RightsData), TMONGO, backendName: "r")]
    public string RightsData
    {
      get => m_RightsData;
      set
      {
        if (!m_RightsData.EqualsOrdSenseCase(value)) m_Rights = null;
        m_RightsData = value;
      }
    }

    /// <summary>
    /// Provides structured rights representation of this nodes RightData
    /// </summary>
    public ConfigSectionNode Rights
    {
      get
      {
        if (m_Rights==null)
        {
          if (m_RightsData.IsNotNullOrWhiteSpace())
            m_Rights = m_RightsData.AsJSONConfig(handling: ConvertErrorHandling.Throw);
        }
        return m_Rights;
      }
      set
      {
        m_Rights = value;

        if (value==null || !value.Exists)
          m_RightsData = null;
        else
          m_RightsData = value.ToJSONString(JsonWritingOptions.PrettyPrintRowsAsMap);
      }
    }

  }

}
