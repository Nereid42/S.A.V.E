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
         private static String SAVE_ROOT = KSPUtil.ApplicationRootPath+"saves";

         private List<BackupSet> backupSets = new List<BackupSet>();

         // array of names for display in GUI
         private String[] games;

         // Thread for doing backups...
         private Thread backupThread;
         // Thread for doing resores...
         private Thread restoreThread;
         // backup job queue
         private readonly BlockingQueue<BackupJob> backupQueue = new BlockingQueue<BackupJob>();
         // restore job queue
         private readonly BlockingQueue<RestoreJob> restoreQueue = new BlockingQueue<RestoreJob>();
         //
         private volatile bool stopRequested = false;

         private volatile bool backupsFinished = true;
         private volatile bool restoreFinished = true;

         public BackupManager()
         {
            Log.Info("new instance of backup manager (save root is "+SAVE_ROOT+")");
            this.backupThread = new Thread(BackupWork);
            this.restoreThread = new Thread(RestoreWork);
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
               backupsFinished = backupQueue.Size() == 0;
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
               restoreFinished = restoreQueue.Size() == 0;
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
                  Log.Test(set.ToString());
               }
               CreateBackupSetNameArray();
            }
            catch (System.Exception e)
            {
               Log.Error("failed to scan for save games: "+e.Message);
            }
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

            Log.Info("BackupManager::OnGameSave");
            String name = HighLogic.SaveFolder;
            BackupSet set = GetBackupSetForName(name);
            if(set==null)
            {
               set = new BackupSet(name, SAVE_ROOT + "/" + name);
               backupSets.Add(set);
               backupSets.Sort(delegate(BackupSet left, BackupSet right)
                  {
                     return left.name.CompareTo(right.name);
                  });
               CreateBackupSetNameArray();
            }

            TimeSpan elapsed = DateTime.Now - set.time;

            Configuration.BACKUP_INTERVAL interval = SAVE.configuration.backupInterval;
            Log.Test("backup interval: " + interval);
            switch (interval)
            {
               case Configuration.BACKUP_INTERVAL.EACH_SAVE:
                  Log.Test("backup each save");
                  BackupGame(set);
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_PER_HOUR:
                  if(elapsed.Hours>=1)
                  {
                     BackupGame(set);
                  }
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_PER_DAY:
                  if (elapsed.Days >= 1)
                  {
                     BackupGame(set);
                  }
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_PER_WEEK:
                  if (elapsed.Days >= 7)
                  {
                     BackupGame(set);
                  }
                  break;
            }
         }

         public int BackupAll()
         {
            Log.Info("creating backup of all save games");
            int cnt = 0;
            backupsFinished = false;
            foreach (BackupSet set in backupSets)
            {
               BackupGame(set);
               cnt++;
            }
            return cnt;
         }

         public void BackupGame(BackupSet set)
         {
            Log.Info("adding backup job for " + set.name+" ("+backupQueue.Size()+" backups in queue)");
            backupsFinished = false;
            BackupJob job = new BackupJob(set);
            backupQueue.Enqueue(job);
         }

         public void BackupGame(String name)
         {
            BackupSet set = GetBackupSetForName(name);
            if (set != null)
            {
               BackupGame(set);
            }
            else
            {
               Log.Warning("no backup set '" + name + "' found");
            }
         }

         public void RestoreGame(String name, String from)
         {
            if(!restoreFinished)
            {
               Log.Warning("restore not complete!");
               return;
            }
            BackupSet set = GetBackupSetForName(name);
            if (set != null)
            {
               restoreFinished = false;
               backupQueue.Enqueue(new BackupJob(set));
            }
            else
            {
               Log.Warning("no backup set '" + name + "' found");
            }
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

         public bool BackupsFinished()
         {
            return backupQueue.Size() == 0 && backupsFinished;
         }

         public bool RestoreFinished()
         {
            return restoreFinished;
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
