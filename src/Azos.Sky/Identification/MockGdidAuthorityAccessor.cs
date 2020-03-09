﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Azos.Collections;
using Azos.Scripting;
using Azos.Sky.Contracts;

namespace Azos.Sky.Identification
{
  /// <summary>
  /// Implements a GDID generation authority accessor which generates GDIDs locally in memory.
  /// This is used exclusively for testing since the generated gdids are not really unique
  /// </summary>
  public sealed class MockGdidAuthorityAccessor : IGdidAuthorityAccessor
  {
    private NamedInterlocked m_Data = new NamedInterlocked();


    public Task<GdidBlock> AllocateBlockAsync(string scopeName, string sequenceName, int blockSize, ulong? vicinity = 1152921504606846975)
    {
      const int MAX_BLOCK=12000;

      var key = scopeName+"::"+sequenceName;
      if (blockSize>MAX_BLOCK) blockSize = MAX_BLOCK;

"FETCHED!!!!!!!!!!!!!!!!!!!!!!!".See();

      var start = m_Data.AddLong(key, blockSize);

      return Task.FromResult( new GdidBlock
      {
        ScopeName = scopeName,
        SequenceName = sequenceName,
        Authority = 1,
        AuthorityHost = "/localhost",
        Era = 0,
        StartCounterInclusive = (ulong)(start - blockSize),
        BlockSize = blockSize,
        ServerUTCTime = Ambient.UTCNow
      });
    }
  }
}
