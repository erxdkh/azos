/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Collections.Generic;
using System.Linq;
using Azos.Log;
using Azos.Log.Sinks;

using Azos.Sky.Chronicle;

namespace Azos.Sky.Log
{
  /// <summary>
  /// Sends log messages to log chronicle service using ILogChronicle
  /// </summary>
  public sealed class ChronicleSink : Sink
  {
    public const int BATCH_TRIM = 1024;

    public ChronicleSink(ISinkOwner owner) : base(owner) { }
    public ChronicleSink(ISinkOwner owner, string name, int order) : base(owner, name, order) { }

    //The dependency may NOT be available at time of construction of this object
    //because this boots before other framework services, hence - service location
    ILogChronicleLogic m_Chronicle;
    ILogChronicleLogic Chronicle
    {
      get
      {
        if (m_Chronicle == null)
          m_Chronicle = App.ModuleRoot.Get<ILogChronicleLogic>();

        return m_Chronicle;
      }
    }


    private List<Message> m_ToSend = new List<Message>();


    protected internal override void DoSend(Message entry)
    {
      if (entry==null) return;

      m_ToSend.Add(entry);
    }

    protected internal override void DoPulse()
    {
      base.DoPulse();

      if (m_ToSend.Count==0) return;

      var toSend = m_ToSend.ToArray();

      if (m_ToSend.Count > BATCH_TRIM)
        m_ToSend = new List<Message>();
      else
        m_ToSend.Clear();

      foreach(var slice in toSend.BatchBy(0xff))
      {
        var batch = new LogBatch
        {
          Data = slice.ToArray()
        };

        Chronicle.WriteAsync(batch)
                 .GetAwaiter()
                 .GetResult();
      }
    }

  }
}
