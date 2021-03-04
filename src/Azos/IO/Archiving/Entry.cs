﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;

namespace Azos.IO.Archiving
{
  /// <summary>
  /// Provides data for raw entry on a page.
  /// Warning: The Raw array segment delimits page buffer which may be recycled in parallel use-case,
  /// therefore never retain entries beyond page ownership as their pointed-to raw data gets lost
  /// </summary>
  public struct Entry
  {
    public enum Status
    {
      BadHeader = -101,
      InvalidLength = -100,

      Unassigned = 0,

      Valid = 1,
      EOF = 2
    }

    public Entry(int address, Status state)
    {
      State = state;
      Address = address;
      Raw = new ArraySegment<byte>();
    }

    public Entry(int address, ArraySegment<byte> raw)
    {
      State = Status.Valid;
      Address = address;
      Raw = raw;
    }


    public readonly Status State;
    public readonly int Address;
    public ArraySegment<byte> Raw;
  }
}
