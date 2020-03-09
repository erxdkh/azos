﻿using System;

using Azos.Data;
using Azos.IAM.Protocol;

namespace Azos.IAM.Server.Data
{
  /// <summary>
  /// Roles represent named ACL sets of permissions
  /// Roles are addressable by their immutable ID and
  /// are used in other ACLs as a mix-in using "assign{role=role-id}"
  /// Unlike groups/accounts, roles do not link to entities via a separate link table
  /// </summary>
  public class Role : EntityWithRights
  {
    [Field(required: true,
           kind: DataKind.ScreenName,
           minLength: Sizes.ENTITY_ID_MIN,
           maxLength: Sizes.ENTITY_ID_MAX,
           description: "Unique Role ID. Do not change the ID as all links to it will become invalid",
           metadata: "idx{name='main' order=0 unique=true dir=asc}")]
    [Field(typeof(Role), nameof(ID), TMONGO, backendName: "id")]
    public string ID { get; set; }
  }
}
