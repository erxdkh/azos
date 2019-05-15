/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;

using Azos.Wave.Mvc;

namespace WaveTestSite.Controllers
{

  public class InheritB : InheritBase
  {

    [Action(Name ="exo")]
    public new string EchoNew(string msg)
    {
      return "B:EchoNew "+msg;
    }

    [Action(Name ="exov")]
    public override string EchoVirtual(string msg)
    {
      return "B:EchoVirtual "+msg;
    }
  }


}
