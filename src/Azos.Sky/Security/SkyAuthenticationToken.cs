/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;

using Azos.Data;
using Azos.Security;
using Azos.Serialization.JSON;

namespace Azos.Sky.Security
{
  /// <summary>
  /// Represents AuthenticationToken in a textual form (e.g. JWT) that can be stored.
  /// SecurityManager implementations in Sky are expected to use string token.Data
  /// </summary>
  public static class SkyAuthenticationTokenSerializer
  {
    public static string Serialize(AuthenticationToken token)
    {
      if (token.Data is string strToken)
        return new { r = token.Realm, d = strToken }.ToJson(JsonWritingOptions.CompactASCII);

      throw new SecurityException(StringConsts.SECURITY_AUTH_TOKEN_SERIALIZATION_ERROR.Args(
                                   nameof(SkyAuthenticationTokenSerializer),
                                   token.Data?.GetType().FullName ?? CoreConsts.NULL_STRING));
    }

    public static AuthenticationToken Deserialize(string token)
    {
      try
      {
        var dataMap = JsonReader.DeserializeDataObject(token) as JsonDataMap;
        var realm = dataMap["r"].AsString();
        var data = dataMap["d"].AsString();

        return new AuthenticationToken(realm, data);
      }
      catch (Exception error)
      {
        throw new SecurityException(StringConsts.SECURITY_AUTH_TOKEN_DESERIALIZATION_ERROR.Args(
                                     nameof(SkyAuthenticationTokenSerializer),
                                     error.ToMessageWithType()), error);
      }
    }
  }
}
