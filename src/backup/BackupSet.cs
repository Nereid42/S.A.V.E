using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nereid
{
   namespace SAVE
   {
      public class BackupSet
      {
         private const String OK_FILE = "backup.ok";
         private const String RESTORED_FILE = "backup.restored";
         private const String PERSISTENT_FILE = "persistent.sfs";

         public enum STATUS { OK = 1, FAILED = 2, NONE = 4, RESTORING = 5, CORRUPT =5 }

         public String name { get; private set; }
         public String pathSaveGame { get; private set; }
         public STATUS status { get; private set; }
         public DateTime time { get; private set; }

         private volatile String[] backupArray;

         private List<String> backups = new List<String>();

         public BackupSet(String name, String pathSaveGame)
         {
            this.name = name;
            this.pathSaveGame = pathSaveGame;
            this.status = STATUS.NONE;
            this.time = new DateTime(0);
         }

         public override bool Equals(System.Object right)
         {
            if (right == null) return false;
            BackupSet cmp = right as BackupSet;
            if (cmp == null) return false;
            return name.Equals(cmp.name);
         }

         public override int GetHashCode()
         {
            return name.GetHashCode();
         }

         public override string ToString()
         {
            return "backup set " + name + " for " + pathSaveGame;
         }

         public int CompareTo(BackupSet right)
         {
            return name.CompareTo(right.name);
         }

         private DateTime GetBackupTimeForFolder(String folder)
         {
            String yyyy = folder.Substring(0, 4);
            String mm = folder.Substring(4, 2);
            String dd = folder.Substring(6, 2);
            String hh = folder.Substring(9, 2);
            String mi = folder.Substring(11, 2);
            String ss = folder.Substring(13, 2);
            try
            {
               int year = int.Parse(yyyy);
               int month = int.Parse(mm);
               int day = int.Parse(dd);
               int hours = int.Parse(hh);
               int minutes = int.Parse(mi);
               int seconds = int.Parse(ss);
               return new DateTime(year, month, day, hours, minutes, seconds);
            }
            catch(FormatException)
            {
               Log.Error("invalid number format for backup folder "+folder);
               return DateTime.MinValue;
            }
         }


         private String[] GetBackupFolders()
         {
            String backupPath = SAVE.configuration.backupPath + "/" + name;
            Log.Detail("get backup folders in '" + backupPath + "'");
            try
            {
               String[] files = FileOperations.GetDirectories(backupPath);
               // sort it just to be sure...
               Array.Sort<String>(files, delegate(String left, String right)
               {
                  return left.CompareTo(right);
               });
               return files;
            }
            catch
            {
               return new String[0];
            }
         }

         private void SortBackupsByName()
         {
            backups.Sort(delegate(String left, String right)
               {
                  // sort descending
                  return -left.CompareTo(right);
               });
         }

         private bool Successful(String folder)
         {
            return FileOperations.FileExists(folder + "/" + OK_FILE);
         }

         private bool PreRestore(String folder)
         {
            return FileOperations.FileExists(folder + "/" + RESTORED_FILE);
         }

         public void ScanBackups()
         {
            Log.Info("scanning backups for " + name);
            backups.Clear();
            String[] backupFolders = GetBackupFolders();
            Array.Reverse(backupFolders);
            status = STATUS.NONE;
            DateTime time = DateTime.MinValue;
            Log.Detail("found " + backupFolders.Length + " backup folders for " + name);
            for (int i = 0; i < backupFolders.Length; i++)
            {
               String folder = backupFolders[i];
               String backupName = FileOperations.GetFileName(folder);
               Log.Detail("adding backup " + backupName + " to backup set " + name);
               if (Successful(folder))
               {
                  backups.Add(backupName);

                  DateTime t = GetBackupTimeForFolder(backupName);

                  if (t > time)
                  {
                     this.time = t;
                  }

                  if (i == 0)
                  {
                     status = STATUS.OK;
                  }
               }
               else
               {
                  if (i == 0)
                  {
                     status = STATUS.FAILED;
                  }
               }

            }
         }

         private void CreateBackupArray()
         {
            try
            {
               int cnt = backups.Count;
               String[] newArray = new String[cnt];
               int i = 0;
               foreach (String name in backups)
               {
                  newArray[i] = name;
                  i++;
               }
               backupArray = newArray;
            }
            catch(Exception e)
            {
               Log.Exception(e);
               Log.Error("failed to create backup array: " + e.Message);
            }
         }

         public String[] GetBackupsAsArray()
         {
            if (backupArray == null) CreateBackupArray();
            return backupArray;
         }

         public void MarkBackupAsFailed()
         {
            status = STATUS.FAILED;
         }

         private void CompressBackup(String folder)
         {
           Log.Info("compressing backup " + folder);
            if(FileOperations.CompressFolder(folder))
            {
               Log.Info("backup succesfully compressed");
            }
            else
            {
               Log.Error("failed to compress backup "+folder);
               status = STATUS.FAILED;
            }
         }

         public String CreateBackup(bool preRestore = false)
         {
            String backupRootFolder = SAVE.configuration.backupPath+"/"+name;
            Log.Info("creating backup of save game '" + name + "' in '" + backupRootFolder + "'");
            if (!FileOperations.DirectoryExists(backupRootFolder))
            {
               Log.Info("creating root backup folder " + backupRootFolder);
               FileOperations.CreateDirectory(backupRootFolder);
            }

            // files to backup
            String[] gameFiles = FileOperations.GetFiles(pathSaveGame);
            if(gameFiles==null | gameFiles.Length==0)
            {
               Log.Info("no files to backup for backup set "+name);
               status = STATUS.NONE;
               return null;
            }

            DateTime time = DateTime.Now;
            String timestamp = time.Hour.ToString("00") + time.Minute.ToString("00") + time.Second.ToString("00");
            String datestamp = time.Year.ToString("0000") + time.Month.ToString("00") + time.Day.ToString("00");
            String backupFolder = backupRootFolder + "/" + datestamp + "-" + timestamp;
            String backupName = FileOperations.GetFileName(backupFolder);
            if (!FileOperations.DirectoryExists(backupFolder))
            {
               FileOperations.CreateDirectory(backupFolder);
               if (preRestore) FileOperations.CreateFile(backupFolder + "/" + RESTORED_FILE);
            }
            else
            {
               Log.Warning("backup folder '"+backupFolder+"' already existing");
               return backupFolder;
            }

            // backup files
            foreach (String sourceFile in gameFiles)
            {
               Log.Info("creating backup of file " + sourceFile);
               // do not copy the restore marker
               if (sourceFile.Equals(RESTORED_FILE)) continue;
               
               String filename = FileOperations.GetFileName(sourceFile);
               try
               {
                  FileOperations.CopyFile(sourceFile, backupFolder + "/" + filename);
               }
               catch(Exception e)
               {
                  Log.Exception(e);
                  Log.Error("failed to create backup of file " + sourceFile + " in " + backupFolder + ": " + e.Message);
                  status = STATUS.FAILED;
                  return backupFolder;
               }
            }
            // backup subfolders if enabled
            if(SAVE.configuration.recurseBackup)
            {
               foreach (String sourceFolder in FileOperations.GetDirectories(pathSaveGame))
               {
                  Log.Info("creating backup of folder " + sourceFolder);
                  String foldername = FileOperations.GetFileName(sourceFolder);
                  try
                  {
                     FileOperations.CopyDirectory(sourceFolder, backupFolder + "/" + foldername);
                  }
                  catch (Exception e)
                  {
                     Log.Exception(e);
                     Log.Error("failed to create backup of folder " + sourceFolder + " in " + backupFolder + ": " + e.Message);
                     status = STATUS.FAILED;
                     return backupFolder;
                  }
               }
            }
            try
            {
               FileOperations.CreateFile(backupFolder + "/" + OK_FILE);
               status = STATUS.OK;
               this.time = time;
               backups.Add(backupName);
               SortBackupsByName();
               CreateBackupArray();
               // compress backup if enabled
               if(SAVE.configuration.compressBackups)
               {
                  CompressBackup(backupFolder);
               }
               else
               {
                  Log.Detail("backup compression disabled");
               }
               Log.Info("backup successful in " + backupFolder );
            }
            catch
            {
               Log.Error("failed to finish backup in " + backupFolder);
               status = STATUS.FAILED;
               return backupFolder;
            }
            return backupFolder;
         }

         private void DeleteSaveGameFiles()
         {
            Log.Info("deleting save game files");
            // does the folder exists?
            if (FileOperations.DirectoryExists(pathSaveGame))
            {
               foreach (String file in FileOperations.GetFiles(pathSaveGame))
               {
                  try
                  {
                     FileOperations.DeleteFileRetry(file);
                  }
                  catch (Exception e)
                  {
                     Log.Error("failed to delete file '" + file + "'");
                     throw e;
                  }
               }
            }
            else
            {
               Log.Warning("could not delete save game files (folder not found)");
            }
         }

         private void RestoreFilesFromBackup(String backup, bool recurse = false)
         {
            Log.Info("copy game files from backup " + backup);
            Log.Detail("save game path to restore is '"+pathSaveGame+"'");
            //
            DeleteSaveGameFiles();
            //
            // create save game Folder if not existent
            if (!FileOperations.DirectoryExists(pathSaveGame))
            {
               Log.Info("creating save game folder "+pathSaveGame);
               FileOperations.CreateDirectoryRetry(pathSaveGame);
            }
            //
            foreach (String file in FileOperations.GetFiles(backup))
            {
               try
               {
                  String name = FileOperations.GetFileName(file);
                  Log.Detail("restoring file " + name + " from "+file);
                  if (!name.Equals(OK_FILE) && !name.Equals(RESTORED_FILE))
                  {
                     Log.Info("copy file "+name);
                     FileOperations.CopyFile(file, pathSaveGame + "/" + name);
                  }
               }
               catch (Exception e)
               {
                  Log.Exception(e);
                  Log.Error("failed to copy file '" + file + "'");
                  throw e;
               }
            }
            // copy recurse?
            if (recurse)
            {
               Log.Info("recurse restore");
               foreach (String folder in FileOperations.GetDirectories(backup))
                {
                   String name = FileOperations.GetFileName(folder);
                   String target =  pathSaveGame + "/" + name;
                   FileOperations.CopyDirectory(folder, target);
                }
            }
         }

         public void RestoreFrom(String backup)
         {
            Log.Info("restoring game '" + this.name + "' from backup " + backup);
            String backupRootFolder = SAVE.configuration.backupPath + "/" + name;
            try 
            {
               // restore before backup?
               if (SAVE.configuration.backupBeforeRestore)
               {
                  if (FileOperations.DirectoryExists(pathSaveGame))
                  {
                     CreateBackup(true);
                     if(status==STATUS.FAILED)
                     {
                          Log.Error("backup for save game failed; aborting restore");
                          return;
                     }
                  }
                  else
                  {
                     Log.Warning("backup before restore skipped: no save game found to backup");
                  }
               }
               // restore the game
               status = STATUS.RESTORING;
               RestoreFilesFromBackup(backupRootFolder + "/" + backup, SAVE.configuration.recurseBackup);
               // uncompress all compressed files
               if(FileOperations.DecompressFolder(pathSaveGame))
               {
                  status = STATUS.OK;
               }
               else
               {
                  status = STATUS.CORRUPT;
                  Log.Error("failed to decompress files");
               }
            }
            catch (Exception e)
            {
               Log.Exception(e);
               Log.Error("save game is corrupted; restore failed");
               status = STATUS.CORRUPT;
            }
         }

         private void DeleteFolder(String folder)
         {
            Log.Info("deleting folder " + folder);
            try
            {
               FileOperations.DeleteDirectory(folder);
            }
            catch (Exception e)
            {
               Log.Exception(e);
               Log.Error("failed to cleanup folder " + folder);
            }
         }

         public String Latest()
         {
            String[] backupFolders = GetBackupFolders();
            if(backupFolders!=null && backupFolders.Length>0)
            {
               return backupFolders[backupFolders.Length - 1];
            }
            return null;
         }

         public void DeleteBackup(String backup)
         {
            Log.Info("deleting backup '" + backup + "' from backupset '" + name + "'");
            String backupRootFolder = SAVE.configuration.backupPath + "/" + name;
            String backupFolder = backupRootFolder + "/" + backup;
            FileOperations.DeleteDirectory(backupFolder);
            backups.Remove(backup);
            CreateBackupArray();
         }

         public void Delete()
         {
            Log.Info("deleting backupset '" + name + "'");
            String backupRootFolder = SAVE.configuration.backupPath + "/" + name;
            FileOperations.DeleteDirectory(backupRootFolder);
         }

         public void Cleanup()
         {
            Log.Info("cleaning up backup "+name);

            // constraint for cleanup
            int minNumberOfBackups = SAVE.configuration.minNumberOfBackups;
            int maxNumberOfBackups = SAVE.configuration.maxNumberOfBackups;
            int daysToKeepBackups = SAVE.configuration.daysToKeepBackups;

            // no cleanup (keep all backups forever?)
            if ( maxNumberOfBackups==0 && daysToKeepBackups==0)
            {
               return;
            }

            String[] backupFolders = GetBackupFolders();
            // the point in time until backups have to be kept
            DateTime timeOfObsoleteBackups = DateTime.Now.AddDays(-daysToKeepBackups);

            // total number of backups before cleanup
            int totalBackupCount = backupFolders.Length;

            // make sure minNumberOfBackups successful backups are kept
            int backupsToClean = totalBackupCount;
            for (int i = totalBackupCount, cnt = 0; i > 0 && cnt < minNumberOfBackups; i--, backupsToClean--)
            {
               String folder = backupFolders[i-1];
               if(Successful(folder))
               {
                  cnt++;
               }
            }

            // backupsToClean is now set, so that minNumberOfBackups are kept
            for (int i = 0; i < backupsToClean; i++)
            {
               String folder = backupFolders[i];
               String backupName = FileOperations.GetFileName(folder);
               DateTime t = GetBackupTimeForFolder(backupName);
               // backup has to be kept, because of time constraints, if not then backup may be obsolete
               bool backupObsoleteByTime = (t < timeOfObsoleteBackups) && (daysToKeepBackups > 0);
               // backup has to be kept, because of number constraints, if not then backup may be obsolete
               bool backupObsoleteByNumber = (totalBackupCount - i > maxNumberOfBackups) && (maxNumberOfBackups > 0);
               // backups are obsolete, if they are obsolete by number constratins AND time constraints
               if (backupObsoleteByTime || backupObsoleteByNumber)
               {
                  // delete backup, if obsolete
                  DeleteFolder(folder);
                  // remove from backup list
                  backups.Remove(backupName);
               }
            }
            // refresh backup array (for GUI display)
            CreateBackupArray();
         }
      }
   }
}
