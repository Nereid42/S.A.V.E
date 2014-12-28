using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Nereid
{
   namespace SAVE
   {
      public class RestoreJob : Job
      {
         private readonly BackupSet set;
         private readonly String name;

         public RestoreJob(BackupSet set, String name)
         {
            this.set = set;
            this.name = name;
         }

         public void Restore()
         {
            set.RestoreFrom(name);
            completed = true;
         }
      }
   }
}
