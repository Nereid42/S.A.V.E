using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nereid
{
   namespace SAVE
   {
      public class BackupJob
      {
         private readonly BackupSet set;

         public static readonly BackupJob NO_JOB = new BackupJob(null);

         private volatile bool completed;

         public BackupJob(BackupSet set)
         {
            this.set = set;
            this.completed = (set==null); // nothing todo means completed
         }

         public void Backup()
         {
            // is there something todo?
            if(set==null) return;
            // create backup
            set.CreateBackup();
            // mark this backup as done
            completed = true;
            // remove obsolete backups
            set.Cleanup();
         }

         public bool IsCompleted()
         {
            return completed;
         }

         public override String ToString()
         {
            return set.name;
         }
      }
   }
}
