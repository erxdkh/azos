/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;

using Azos.Apps;


namespace Azos.Serialization.Arow
{
  /// <summary>
  /// Designate data document types that support Arow serialization -
  /// types that generate Arow serialization/deserialization method cores
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
  public sealed class ArowAttribute : GuidTypeAttribute
  {
    public ArowAttribute(string typeGuid) : base(typeGuid){ }
  }
}
