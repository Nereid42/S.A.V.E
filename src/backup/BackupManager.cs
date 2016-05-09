using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Nereid
{
   namespace SAVE
   {
      public class BackupManager : IEnumerable<BackupSet>
      {
         private const int MILLIS_RESTORE_WAIT = 2000;
         public static String SAVE_ROOT = KSPUtil.ApplicationRootPath+"saves";

         private const String SAVE_GAME_TRAINING = "training";
         private const String SAVE_GAME_SCENARIOS = "scenarios";
         private const String SAVE_GAME_DESTRUCTIBLES = "scenarios";

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

         private bool BuildInSaveGame(String name)
         {
            return name.Equals(SAVE_GAME_TRAINING) || name.Equals(SAVE_GAME_SCENARIOS) || name.Equals(SAVE_GAME_SCENARIOS);
         }

         private void AddBackup(String name, String folder)
         {
            if (!BuildInSaveGame(name))
            {
               BackupSet set = new BackupSet(name, folder);
               backupSets.Add(set);
               set.ScanBackups();
            }
            else
            {
               Log.Detail("save game " + name + " is build in and ignored");
            }
         }

         public void ScanSavegames()
         {
            Log.Info("scanning save games");
            try
            {
               // scan save games
               foreach (String folder in Directory.GetDirectories(SAVE_ROOT))
               {
                  Log.Info("save game found: "+folder);
                  String name = Path.GetFileName(folder);

                  if (GetBackupSetForName(name)==null)
                  {
                     Log.Detail("adding backup set " + name);
                     AddBackup(name, folder);
                  }
               }
               // scan backups (if save game folder was deleted)
               foreach (String folder in Directory.GetDirectories(SAVE.configuration.backupPath))
               {
                  String name = Path.GetFileName(folder);
                  BackupSet set = GetBackupSetForName(name);
                  if(set==null)
                  {
                     Log.Detail("adding backup set "+name+" (save game was deleted)");
                     AddBackup(name, SAVE_ROOT+"/"+name);
                  }
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
            //
            if (SAVE.configuration.disabled)
            {
               Log.Info("backup disabled");
               return;
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
                  job = BackupGame(set);
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_IN_10_MINUTES:
                  if (elapsed.TotalMinutes >= 10)
                  {
                     job = BackupGame(set);
                  }
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_IN_30_MINUTES:
                  if (elapsed.TotalMinutes >= 30)
                  {
                     job = BackupGame(set);
                  }
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_PER_HOUR:
                  if(elapsed.TotalHours>=1)
                  {
                     job = BackupGame(set);
                  }
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_IN_2_HOURS:
                  if (elapsed.TotalHours >= 2)
                  {
                     job = BackupGame(set);
                  }
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_IN_4_HOURS:
                  if (elapsed.TotalHours >= 4)
                  {
                     job = BackupGame(set);
                  }
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_PER_DAY:
                  if (elapsed.TotalDays >= 1)
                  {
                     job = BackupGame(set);
                  }
                  break;
               case Configuration.BACKUP_INTERVAL.ONCE_PER_WEEK:
                  if (elapsed.TotalDays >= 7)
                  {
                     job = BackupGame(set);
                  }
                  break;
               case Configuration.BACKUP_INTERVAL.CUSTOM:
                  if (elapsed.Minutes >= SAVE.configuration.customBackupInterval)
                  {
                     job = BackupGame(set);
                  }
                  break;
               case Configuration.BACKUP_INTERVAL.ON_QUIT:
                  Log.Detail("backups are done every quit");
                  break;
               default:
                  Log.Error("invalid backup interval ignored; backup is done each save");
                  job = BackupGame(set);
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
               BackupGame(set, true);
               cnt++;
            }
            return cnt;
         }

         public BackupJob BackupGame(BackupSet set, bool forceAsynchronous = false)
         {
            Log.Info("adding backup job for " + set.name+" ("+backupQueue.Size()+" backups in queue)");
            allBackupsCompleted = false;
            BackupJob job = new BackupJob(set);
            if (SAVE.configuration.asynchronous || forceAsynchronous)
            {
               Log.Info("adding backup job for " + set.name + " (" + backupQueue.Size() + " backups in queue)");
               backupQueue.Enqueue(job);
            }
            else
            {
               Log.Info("synchronous backup to backup set '" + set.name);
               // wait for asynchronous backups to complete
               while (backupQueue.Size() > 0) Thread.Sleep(100);
               // do backup
               job.Backup();
               // done
               allBackupsCompleted = true;
            }
            return job;
         }

         public BackupJob BackupGame(String game)
         {
            BackupSet set = GetBackupSetForName(game);
            if(set==null)
            {
               set = new BackupSet(game, SAVE_ROOT + "/" + game);
               backupSets.Add(set);
               SortBackupSets();
               CreateBackupSetNameArray();
            }
            return BackupGame(set);
         }

         public void CloneGame(String game, String into)
         {
            Log.Info("cloning game from backup of '"+game+"' into '"+into+"'");
            BackupSet set = GetBackupSetForName(game);
            if (set != null)
            {
               String from = set.Latest();
               String to = SAVE_ROOT+"/"+into;
               Log.Info("cloning game from '" + from + "' into '" + into + "'");
               if (FileOperations.DirectoryExists(from))
               {
                  if (FileOperations.DirectoryExists(to))
                  {
                     Log.Error("cloning of game failed: target folder exists");
                     return;
                  }
                  FileOperations.CopyDirectory(from, to);
                  ScanSavegames();
               }
               else
               {
                  Log.Error("cloning of game failed: no backup folder to clone");
               }
            }
            else
            {
               Log.Error("cloning of game failed: no backup set '" + game + "' found");
            }
         }

         public void CloneBackup(String game, String into)
         {
            Log.Info("cloning backup of '" + game + "' into '" + into + "'");
            game = FileOperations.GetFileName(game);
            BackupSet set = GetBackupSetForName(game);
            if (set != null)
            {
               String from = SAVE.configuration.backupPath + "/" + game;
               String to = SAVE.configuration.backupPath + "/" + into;
               Log.Info("cloning backup from '" + from + "' into '" + into + "'");
               if (FileOperations.DirectoryExists(from))
               {
                  if (FileOperations.DirectoryExists(to))
                  {
                     Log.Error("cloning of backup failed: target folder exists");
                     return;
                  }
                  FileOperations.CopyDirectory(from, to);
                  BackupSet clone = GetBackupSetForName(into);
                  if(clone!=null)
                  {
                     clone.ScanBackups();
                  }
               }
               else
               {
                  Log.Error("cloning of backup failed: no backup folder to clone");
               }
            }
            else
            {
               Log.Error("cloning of backup failed: no backup set '" + game + "' found");
            }
         }

         public bool RestoreGame(String game, String from)
         {
            BackupSet set = GetBackupSetForName(game);
            if (set != null)
            {
               restoreCompleted = false;
               restoredGame = game;
               RestoreJob job = new RestoreJob(set, from);
               Log.Warning("restoring game " + game);
               if (SAVE.configuration.asynchronous)
               {
                  Log.Info("asynchronous restore from backup set '" + game + "' backup '" + from + "'");
                  restoreQueue.Enqueue(job);
               }
               else
               {
                  Log.Info("synchronous restore from backup set '" + game + "' backup '" + from + "'");
                  // wait for asynchronous restores to complete
                  while (restoreQueue.Size() > 0) Thread.Sleep(100);
                  // do restore
                  job.Restore();
                  // done
                  restoreCompleted = true;
               }
               return true;
            }
            else
            {
               Log.Warning("no backup set '" + game + "' found");
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
