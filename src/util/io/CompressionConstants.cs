using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nereid.SevenZip;

namespace Nereid
{
   namespace SAVE
   {
      static class CompressionConstants
      {
         public static readonly object[] properties =
              {
                    (Int32)(1<<23),
                    (Int32)2,
                    (Int32)3,
                    (Int32)0,
                    (Int32)2,
                    (Int32)128,
                    "bt4",
                    true
                };

         public static readonly CoderPropID[] propIDs =
         {
                    CoderPropID.DictionarySize,
                    CoderPropID.PosStateBits,
                    CoderPropID.LitContextBits,
                    CoderPropID.LitPosBits,
                    CoderPropID.Algorithm,
                    CoderPropID.NumFastBytes,
                    CoderPropID.MatchFinder,
                    CoderPropID.EndMarker
                };
      }
   }
}
