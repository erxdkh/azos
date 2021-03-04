﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.Linq;

using Azos.Conf;
using Azos.Data.Access.MongoDb.Connector;
using Azos.Instrumentation;
using Azos.Serialization.JSON;
using Azos.Web;

namespace Azos.Security.MinIdp.Instrumentation
{
  /// <summary>
  /// Deletes user
  /// </summary>
  public sealed class DropUser : LongIdCmdBase
  {
    public DropUser(MinIdpMongoDbStore mongo) : base(mongo) { }


    public override ExternalCallResponse Describe()
    => new ExternalCallResponse(ContentType.TEXT,
@"
# Deletes user
```
  DropUser
  {
    realm='realm' //atom
    id=123 //long
  }
```");

    protected override object ExecuteBody()
    {
      var crud = Context.Access((tx) => {
        var cuser = tx.Db[BsonDataModel.GetCollectionName(this.Realm, BsonDataModel.COLLECTION_USER)];

        var cr = cuser.DeleteOne(Query.ID_EQ_Int64(this.Id));
        Aver.IsNull(cr.WriteErrors, cr.WriteErrors?.FirstOrDefault().Message);
        return cr;
      });

      return crud;
    }

  }
}
