﻿using System;

namespace AGSUnpacker.Lib
{
  public class AGSCursorInfo
  {
    public Int32 picture;
    public Int16 hotspot_x;
    public Int16 hotspot_y;
    public Int16 view;
    public string name;
    public byte flags;

    // extension data
    public int animdelay;

    public AGSCursorInfo()
    {
      picture = 0;
      hotspot_x = 0;
      hotspot_y = 0;
      view = 0;
      name = string.Empty;
      flags = 0;

      animdelay = 5;
    }

    public void LoadFromStream(AGSAlignedStream ar)
    {
      picture = ar.ReadInt32();
      hotspot_x = ar.ReadInt16();
      hotspot_y = ar.ReadInt16();
      view = ar.ReadInt16();
      name = ar.ReadFixedString(10);
      
      //NOTE(adm244): structure is aligned at 4-byte boundary,
      // read with a padding and discard it
      flags = (byte)ar.ReadInt32();
    }
  }
}
