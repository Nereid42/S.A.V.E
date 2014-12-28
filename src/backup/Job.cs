using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nereid
{
   namespace SAVE
   {
      public class Job
      {
         protected volatile bool completed;

         public bool IsCompleted()
         {
            return completed;
         }
      }
   }
}
