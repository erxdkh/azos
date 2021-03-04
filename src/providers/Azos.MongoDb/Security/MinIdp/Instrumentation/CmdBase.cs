﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using Azos.Conf;
using Azos.Instrumentation;
using Azos.Serialization.JSON;
using Azos.Web;

namespace Azos.Security.MinIdp.Instrumentation
{
  /// <summary>
  /// Base command
  /// </summary>
  [SystemAdministratorPermission(AccessLevel.ADVANCED)]
  public abstract class CmdBase : ExternalCallRequest<MinIdpMongoDbStore>
  {
    public CmdBase(MinIdpMongoDbStore mongo) : base(mongo) { }

    [Config]
    public Atom Realm {  get; set;}

    public sealed override ExternalCallResponse Execute()
    {
      Validate();
      var result = ExecuteBody();
      var response = ToResponse(result);
      return response;
    }

    protected virtual void Validate()
    {
      if (!Realm.IsValid || Realm.IsZero) throw new CallGuardException(GetType().Name, "Realm", "Parameter `$realm` must be a valid non zero Atom"){ Code = -100 };
    }

    protected abstract object ExecuteBody();

    protected virtual ExternalCallResponse ToResponse(object result)
    {
      var response = new {OK = true, data = result};
      var json = response.ToJson(JsonWritingOptions.PrettyPrintASCII);
      return new ExternalCallResponse(ContentType.JSON, json);
    }

  }

  /// <summary>
  /// Base command with ID
  /// </summary>
  public abstract class IdCmdBase : CmdBase
  {
    public IdCmdBase(MinIdpMongoDbStore mongo) : base(mongo) { }

    [Config]
    public string Id { get; set; }


    protected override void Validate()
    {
      base.Validate();
      if (Id.IsNullOrWhiteSpace()) throw new CallGuardException(GetType().Name, "Id",  "Parameter `$id` is not set") { Code = -150 };
      if (Id.Length > BsonDataModel.MAX_ID_LEN) throw new CallGuardException(GetType().Name, "Id", "Length of `$id` is over maximum of {0}".Args(BsonDataModel.MAX_ID_LEN)) { Code = -151 };
    }
  }

  /// <summary>
  /// Base command with long ID
  /// </summary>
  public abstract class LongIdCmdBase : CmdBase
  {
    public LongIdCmdBase(MinIdpMongoDbStore mongo) : base(mongo) { }

    [Config]
    public long Id { get; set; }


    protected override void Validate()
    {
      base.Validate();
      if (Id==0) throw new CallGuardException(GetType().Name, "Id", "Parameter `$id` is not set") { Code = -150 };
    }
  }
}
