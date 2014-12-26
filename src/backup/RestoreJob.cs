using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nereid
{
   namespace SAVE
   {
      class RestoreJob
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
         }
      }
   }
}
