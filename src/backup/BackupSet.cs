﻿using System;
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
         private List<String> prerestore = new List<String>();

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

         public void ScanBackups()
         {
            backups.Clear();
            prerestore.Clear();
            String[] backupFolders = GetBackupFolders();
            Array.Reverse(backupFolders);
            status = STATUS.NONE;
            DateTime time = DateTime.MinValue;
            for (int i = 0; i < backupFolders.Length; i++)
            {
               String folder = backupFolders[i];
               String backupName = FileOperations.GetFileName(folder);
               if (Successful(folder))
               {
                  if (!backupName.EndsWith("-R"))
                  {
                     backups.Add(backupName);
                  }
                  else
                  {
                     prerestore.Add(backupName);
                  }

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
               Log.Error("failed to create backup array: "+e.Message);
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


         public String CreateBackup(bool preRestore = false)
         {
            String backupRootFolder = SAVE.configuration.backupPath+"/"+name;
            Log.Info("creating backup of save game '" + name + "' in '" + backupRootFolder + "'");
            if (!FileOperations.DirectoryExists(backupRootFolder))
            {
               Log.Info("creating root backup folder " + backupRootFolder);
               FileOperations.CreateDirectory(backupRootFolder);
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
            foreach (String sourceFile in FileOperations.GetFiles(pathSaveGame))
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
                  Log.Error("failed to create backup of file " + sourceFile + " in " + backupFolder+": "+e.Message);
                  status = STATUS.FAILED;
                  return backupFolder;
               }
            }
            // backup subfolders if enabled
            if(SAVE.configuration.recurseBackup)
            {
               foreach (String sourceFolder in FileOperations.GetDirectories(pathSaveGame))
               {
                  Log.Info("creating backup of foldeer " + sourceFolder);
                  String foldername = FileOperations.GetFileName(sourceFolder);
                  try
                  {
                     FileOperations.CopyDirectory(sourceFolder, backupFolder + "/" + foldername);
                  }
                  catch (Exception e)
                  {
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
               Log.Info("backup successful in " + backupFolder );
            }
            catch
            {
               Log.Error("failed to finish backup in " + backupFolder);
               if(preRestore)
               {
                  Log.Warning("failed to create pre restore backup");
                  status = STATUS.CORRUPT;
               }
               else
               {
                  status = STATUS.FAILED;
               }
            }
            return backupFolder;
         }

         private void DeleteSaveGameFiles()
         {
            Log.Info("deleting save game files");
            foreach (String file in FileOperations.GetFiles(pathSaveGame))
            {
               try
               {
                  FileOperations.DeleteFile(file);
               }
               catch(Exception e)
               {
                  Log.Error("failed to delete file '"+file+"'");
                  throw e;
               }
            }
         }

         private void RestoreFilesFromBackup(String backup, bool recurse = false)
         {
            Log.Info("copy game files from backup " + backup);
            foreach (String file in FileOperations.GetFiles(backup))
            {
               try
               {
                  String name = FileOperations.GetFileName(file);
                  if (!name.Equals(OK_FILE) && !name.Equals(RESTORED_FILE))
                  {
                     Log.Info("copy file "+name);
                     FileOperations.CopyFile(file, pathSaveGame + "/" + name);
                  }
               }
               catch (Exception e)
               {
                  Log.Error("failed to copy file '" + file + "'");
                  throw e;
               }
            }
            // copy recurse?
            if (recurse)
            {
                foreach (String folder in FileOperations.GetDirectories(backup))
                {
                   String name = FileOperations.GetFileName(folder);
                   FileOperations.CopyDirectory(folder, pathSaveGame + "/" + name);
                }
            }
         }


         public void RestoreFrom(String backup)
         {
            Log.Info("restoring game '" + this.name + "' from backup " + backup);
            String backupRootFolder = SAVE.configuration.backupPath + "/" + name;
            try 
            {
               status = STATUS.RESTORING;
               CreateBackup(true);
               if(status==STATUS.CORRUPT)
               {
                  Log.Error("save game is corrupted; aborting restore");
                  return;
               }
               DeleteSaveGameFiles();
               RestoreFilesFromBackup(backupRootFolder+"/"+backup, SAVE.configuration.recurseBackup);
               status = STATUS.OK;
            }
            catch
            {
               Log.Error("save game is corrupted; restore failed");
               status = STATUS.CORRUPT;
            }
         }

         private void DeleteFolder(String folder)
         {
            Log.Info("delting folder " + folder);
            try
            {
               FileOperations.DeleteDirectory(folder);
            }
            catch (Exception e)
            {
               Log.Error("exception caught: " + e.GetType() + ": " + e.Message);
               Log.Error("failed to cleanup folder " + folder);
            }
         }

         public void Cleanup()
         {
            Log.Info("cleaning up backup "+name);

            int minNumberOfBackups = SAVE.configuration.minNumberOfBackups;
            int maxNumberOfBackups = SAVE.configuration.maxNumberOfBackups;
            int daysToKeepBackups = SAVE.configuration.daysToKeepBackups;

            // no cleanup (keep all backups forever?)
            if ( maxNumberOfBackups==0 && daysToKeepBackups==0)
            {
               return;
            }


            String[] backupFolders = GetBackupFolders();
            DateTime timeOfObsoleteBackups = DateTime.Now.AddDays(-daysToKeepBackups);

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

            for (int i = 0; i < backupsToClean; i++)
            {
               String folder = backupFolders[i];
               String backupName = FileOperations.GetFileName(folder);
               DateTime t = GetBackupTimeForFolder(backupName);
               // backup has to be kept, because of time constraints
               bool backupObsoleteByTime =  ( t < timeOfObsoleteBackups ) && ( daysToKeepBackups > 0 );
               bool backupObsoleteByNumber =  ( totalBackupCount - i > maxNumberOfBackups ) && ( maxNumberOfBackups > 0 );
               if (backupObsoleteByTime || backupObsoleteByNumber)
               {
                  // delete this backup
                  DeleteFolder(folder);
                  backups.Remove(backupName);
               }
            }
            CreateBackupArray();
         }
      }


   }
}
