﻿using System;

using Azos.Data;

namespace Azos.Security.Tokens
{
  /// <summary> Common base for all tokens stored on a token ring </summary>
  public abstract class RingToken : TypedDoc
  {
    public const string PROTECTED_MSG_TARGET = "protected-msg";

    [Field(backendName: "id", isArow: true)]
    [Field(required: true, key: true, backendName: "_id", description: "Value/ID of the token - a unique primary key. Used only for server-side tokens")]
    public string ID { get; set; }

    [Field(backendName: "tp", isArow: true)]
    [Field(required: true, backendName: "tp", description: "Type ensures that this token can not be deserialized into a different type")]
    public string Type { get; internal set; }

    /// <summary>
    /// Version UTC stamp (Unix seconds time), the system replicates data based on 1 sec resolution.
    /// By design more precise resolution is not needed for practical reasons.
    /// </summary>
    [Field(backendName: "_v", isArow: true)]
    [Field(required: true, backendName: "_v", description: "Version timestamp of this doc (Unix UTC time), used for replication")]
    [Field(targetName: PROTECTED_MSG_TARGET, storeFlag: StoreFlag.None)]
    public long? VersionUtc { get; set; }

    /// <summary> Returns VersionUtc as DateTime of UTC kind </summary>
    public DateTime? VersionUtcTimestamp
    {
      get => VersionUtc?.FromSecondsSinceUnixEpochStart();
      set => VersionUtc = value?.ToSecondsSinceUnixEpochStart();
    }

    /// <summary>
    /// Version Delete flag, used for CRDT idempotent flag
    /// </summary>
    [Field(backendName: "_d", isArow: true)]
    [Field(required: true, backendName: "_d", description: "Version delete flag, used for replication")]
    [Field(targetName: PROTECTED_MSG_TARGET, storeFlag: StoreFlag.None)]
    public bool VersionDeleted { get; set; }

    [Field(backendName: "b", isArow: true)]
    [Field(required: true, backendName: "b", description: "Name of party issuing token")]
    public string IssuedBy { get; set; }

    /// <summary> Expressed in number of seconds since UNIX epoch </summary>
    [Field(backendName: "i", isArow: true)]
    [Field(required: true, backendName: "i", description: "When was the token issued (Unix UTC time)")]
    public long? IssueUtc{  get; set; }

    /// <summary> Returns IssueUtc as DateTime of UTC kind </summary>
    public DateTime? IssueUtcTimestamp
    {
      get => IssueUtc?.FromSecondsSinceUnixEpochStart();
      set => IssueUtc = value?.ToSecondsSinceUnixEpochStart();
    }

    /// <summary> Expressed in number of seconds since UNIX epoch </summary>
    [Field(backendName: "e", isArow: true)]
    [Field(required: true, backendName: "e", description: "When the token becomes invalid (Unix UTC time), as-if non existing")]
    public long? ExpireUtc { get; set; }

    /// <summary> Returns ExpireUtc as DateTime of UTC kind </summary>
    public DateTime? ExpireUtcTimestamp
    {
      get => ExpireUtc?.FromSecondsSinceUnixEpochStart();
      set => ExpireUtc = value?.ToSecondsSinceUnixEpochStart();
    }

    public abstract int TokenByteStrength { get; }
    public abstract int TokenDefaultExpirationSeconds { get; }


    public override ValidState Validate(ValidState state)
    {
      state = base.Validate(state);//ensure base constraints first (e.g. `Type` is required)
      if (state.ShouldStop) return state;

      //Check type signature to ensure that the serialized token does not get deserialized into another one
      //using dynamic formats (e.g. JSON)
      //Type is a required field
      if (Type != GetType().Name)
        state = new ValidState(state, new FieldValidationException(this, nameof(Type), "Token Type Mismatch"));

      return state;
    }

    protected override Exception CheckValueLength(string targetName, Schema.FieldDef fdef, FieldAttribute atr, object value)
    {
      if (fdef.Name==nameof(ID) && !targetName.EqualsOrdSenseCase(PROTECTED_MSG_TARGET))
      {
        var l = value.ToString().Length;
        if (l<TokenByteStrength)
          return new FieldValidationException(this, nameof(ID), "Token ID byte length is {0} bytes, but must be {1} bytes".Args(l, TokenByteStrength));
      }

      return base.CheckValueLength(targetName, fdef, atr, value);
    }
  }
}
