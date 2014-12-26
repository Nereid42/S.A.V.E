using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nereid
{
   namespace SAVE
   {
      class BackupJob
      {
         private readonly BackupSet set;

         public BackupJob(BackupSet set)
         {
            this.set = set;
         }

         public void Backup()
         {
            set.CreateBackup();
         }

         public override String ToString()
         {
            return set.name;
         }
      }
   }
}
