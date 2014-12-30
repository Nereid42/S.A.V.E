using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

namespace Nereid
{
   namespace SAVE
   {
      public class BackupManager : IEnumerable<BackupSet>
      {
         private const int MILLIS_RESTORE_WAIT = 2000;
         private static String SAVE_ROOT = KSPUtil.ApplicationRootPath+"saves";

         private List<BackupSet> backupSets = new List<BackupSet>();

         // array of names for display in GUI
         private String[] games;

         // Thread for doing backups...
         private Thread backupThread;
         // Thread for doing restores...
         private Thread restoreThread;
         // backup job queue
         private readonly BlockingQueue<BackupJob> backupQueue = new BlockingQueue<BackupJob>();
         // restore job queue
         private readonly BlockingQueue<RestoreJob> restoreQueue = new BlockingQueue<RestoreJob>();
         //
         private volatile bool stopRequested = false;

         private volatile bool allBackupsCompleted = true;
         private volatile bool restoreCompleted = true;
         private volatile String restoredGame;

         public BackupManager()
         {
            Log.Info("new instance of backup manager (save root is "+SAVE_ROOT+")");
            this.backupThread = new Thread(new ThreadStart(this.BackupWork));
            this.restoreThread = new Thread(new ThreadStart(this.RestoreWork));
         }

         public void Start()
         {
            Log.Info("starting backup/restore threads");
            backupThread.Start();
            restoreThread.Start();
         }

         public void Stop()
         {
            Log.Info("stopping backup/restore threads");
            stopRequested = true;
         }

         public void BackupWork()
         {
            Log.Info("backup thread running");
            while(!stopRequested)
            {
               BackupJob job = backupQueue.Dequeue();
               Log.Info("executing backup job " + job);
               job.Backup();
               allBackupsCompleted = backupQueue.Size() == 0;
            }
            Log.Info("backup thread terminated");
         }

         public void RestoreWork()
         {
            Log.Info("restore thread running");
            while (!stopRequested)
            {
               RestoreJob job = restoreQueue.Dequeue();
               Log.Info("executing restore job " + job);
               job.Restore();
               // wait at least 2 seconds;
               Thread.Sleep(MILLIS_RESTORE_WAIT);
               restoreCompleted = restoreQueue.Size() == 0;
            }
            Log.Info("restore thread terminated");
         }

         public void ScanSavegames()
         {
            Log.Info("scanning save games");
            try
            {
               foreach (String folder in Directory.GetDirectories(SAVE_ROOT))
               {
                  Log.Info("save game found: "+folder);

                  String name = Path.GetFileName(folder);

                  BackupSet set = new BackupSet(name, folder);

                  backupSets.Add(set);
                  set.ScanBackups();
               }
               SortBackupSets();
               CreateBackupSetNameArray();
            }
            catch (System.Exception e)
            {
               Log.Error("failed to scan for save games: "+e.Message);
            }
         }

         private void SortBackupSets()
         {
            backupSets.Sort(delegate(BackupSet left, BackupSet right)
            {
               return left.name.CompareTo(right.name);
            });
         }

         public BackupSet GetBackupSetForName(String name)
         {
            foreach (BackupSet set in backupSets)
            {
               if (set.name.Equals(name)) return set;
            }
            Log.Warning("no backup set '"+name+"' found");
            return null;
         }


         public void CallbackGameSaved(Game game)
         {
            String name = HighLogic.SaveFolder;
            BackupSet set = GetBackupSetForName(name);
            if(set==null)
            {
               set = new BackupSet(name, SAVE_ROOT + "/" + name);
               backupSets.Add(set);
               SortBackupSets();
               CreateBackupSetNameArray();
            }
            TimeSpan elapsed = DateTime.Now - set.time;
            // 
            if (elapsed.Seconds <= 0)
            {
               Log.Info("backup already done");
               return;
            }


            Configuration.BACKUP_INTERVAL interval = SAVE.configuration.backupInterval;
            BackupJob job = BackupJob.NO_JOB;
            switch (interval)
            {
               case Configuration.BACKUP_INTERVAL.EACH_SAVE:
                  job = BackupGameInBackground(set);
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_IN_10_MINUTES:
                  if (elapsed.Minutes >= 10)
                  {
                     job = BackupGameInBackground(set);
                  }
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_IN_30_MINUTES:
                  if (elapsed.Minutes >= 30)
                  {
                     job = BackupGameInBackground(set);
                  }
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_PER_HOUR:
                  if(elapsed.Hours>=1)
                  {
                     job = BackupGameInBackground(set);
                  }
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_PER_DAY:
                  if (elapsed.Days >= 1)
                  {
                     job = BackupGameInBackground(set);
                  }
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_PER_WEEK:
                  if (elapsed.Days >= 7)
                  {
                     job = BackupGameInBackground(set);
                  }
                  break;
            }
            // wait for job to complete, to avoid concurrency problems 
            WaitUntilBackupJobCompleted(job);

         }

         private void WaitUntilBackupJobCompleted(BackupJob job)
         {
            while (!job.IsCompleted() && !stopRequested)
            {
               Thread.Sleep(100);
            }
         }

         public int BackupAll()
         {
            Log.Info("creating backup of all save games");
            int cnt = 0;
            allBackupsCompleted = false;
            foreach (BackupSet set in backupSets)
            {
               BackupGameInBackground(set);
               cnt++;
            }
            return cnt;
         }

         public BackupJob BackupGameInBackground(BackupSet set)
         {
            Log.Info("adding backup job for " + set.name+" ("+backupQueue.Size()+" backups in queue)");
            allBackupsCompleted = false;
            BackupJob job = new BackupJob(set);
            backupQueue.Enqueue(job);
            return job;
         }

         public BackupJob BackupGame(String name)
         {
            BackupSet set = GetBackupSetForName(name);
            if(set==null)
            {
               set = new BackupSet(name, SAVE_ROOT + "/" + name);
               backupSets.Add(set);
               SortBackupSets();
               CreateBackupSetNameArray();
            }
            return BackupGameInBackground(set);
         }

         public bool RestoreGameInBackground(String name, String from)
         {
            if(!restoreCompleted)
            {
               Log.Warning("restore not complete!");
               return false;
            }
            BackupSet set = GetBackupSetForName(name);
            if (set != null)
            {
               restoreCompleted = false;
               restoredGame = name;
               Log.Warning("restoring game "+name);
               RestoreJob job = new RestoreJob(set, from);
               //restoreQueue.Enqueue(job);
               job.Restore();
               return true;
            }
            else
            {
               Log.Warning("no backup set '" + name + "' found");
               return false;
            }
         }

         public String GetRestoredGame()
         {
            if (restoredGame == null) return "none";
            return restoredGame;
         }


         public int Queuedbackups()
         {
            return backupQueue.Size();
         }

         private void CreateBackupSetNameArray()
         {
            games = new String[backupSets.Count];
            int i = 0;
            foreach(BackupSet set in backupSets)
            {
               games[i] = set.name;
               i++;
            }
         }

         public String[] GetBackupSetNameArray()
         {
            if (games == null) CreateBackupSetNameArray();
            return games;
         }

         public bool BackupsCompleted()
         {
            return backupQueue.Size() == 0 && allBackupsCompleted;
         }

         public bool RestoreCompleted()
         {
            return restoreCompleted;
         }


         public System.Collections.IEnumerator GetEnumerator()
         {
            return backupSets.GetEnumerator();
         }

         IEnumerator<BackupSet> IEnumerable<BackupSet>.GetEnumerator()
         {
            return backupSets.GetEnumerator();
         }
      }
   }
}
