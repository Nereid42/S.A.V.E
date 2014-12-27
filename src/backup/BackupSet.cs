﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Nereid
{
   namespace SAVE
   {
      public class BackupSet
      {
         private const String OK_FILE = "backup.ok";

         public enum STATUS { OK = 1, FAILED = 2, NONE = 4, RESTORING = 5, CORRUPT =5 }

         public String name { get; private set; }
         public String pathSaveGame { get; private set; }
         public STATUS status { get; private set; }
         public DateTime time { get; private set; }

         private String[] backupArray;

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
               return Directory.GetDirectories(backupPath);
            }
            catch
            {
               return new String[0];
            }
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
               String backupName = Path.GetFileName(folder);
               if (File.Exists(folder + "/" + OK_FILE))
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
               int offset = prerestore.Count > 0 ? 1 : 0;
               int cnt = backups.Count + offset;
               String[] newArray = new String[cnt];
               if (prerestore.Count > 0)
               {
                  newArray[0] = "UNDO RESTORE";
               }
               int i = 0;
               foreach (String name in backups)
               {
                  newArray[i + offset] = name;
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


         public String CreateBackup(bool preRestore = false)
         {
            String backupRootFolder = SAVE.configuration.backupPath+"/"+name;
            Log.Info("creating backup of save game '" + name + "' in '" + backupRootFolder + "'");
            if (!Directory.Exists(backupRootFolder))
            {
               Log.Info("creating root backup folder " + backupRootFolder);
               Directory.CreateDirectory(backupRootFolder);
            }

            DateTime time = DateTime.Now;
            String timestamp = time.Hour.ToString("00") + time.Minute.ToString("00") + time.Second.ToString("00");
            String datestamp = time.Year.ToString("0000") + time.Month.ToString("00") + time.Day.ToString("00");
            String backupFolder = backupRootFolder + "/" + datestamp + "-" + timestamp + (preRestore?"-R":"");
            if (!Directory.Exists(backupFolder))
            {
               Directory.CreateDirectory(backupFolder);
            }
            else
            {
               Log.Warning("backup folder '"+backupFolder+"' already existing");
               return backupFolder;
            }

            foreach (String sourceFile in Directory.GetFiles(pathSaveGame))
            {
               Log.Info("creating backup of file " + sourceFile);
               String filename = Path.GetFileName(sourceFile);
               try
               {
                  File.Copy(sourceFile, backupFolder + "/" + filename);
               }
               catch(Exception e)
               {
                  Log.Error("failed to create backup of file " + sourceFile + " in " + backupFolder+": "+e.Message);
                  status = STATUS.FAILED;
                  return backupFolder;
               }
            }
            try
            {
               File.Create(backupFolder + "/"+ OK_FILE);
               status = STATUS.OK;
               this.time = time;
               CreateBackupArray();
               Log.Info("backup successful " + backupFolder );
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
            foreach (String file in Directory.GetFiles(pathSaveGame))
            {
               try
               {
                  File.Delete(file);
               }
               catch(Exception e)
               {
                  Log.Error("failed to delete file '"+file+"'");
                  throw e;
               }
            }
         }

         private void CopyGameFilesFromBackup(String backup)
         {
            foreach (String file in Directory.GetFiles(backup))
            {
               try
               {
                  String name = Path.GetFileName(file);
                  if (!name.Equals(OK_FILE))
                  {
                     Log.Info("copy file "+name);
                     File.Copy(file,pathSaveGame+"/"+name);
                  }
               }
               catch (Exception e)
               {
                  Log.Error("failed to copy file '" + file + "'");
                  throw e;
               }
            }
         }

         private bool LastBackupWasPreRestore()
         {
            if (prerestore.Count == 0) return false;
            if (backups.Count == 0) return true;
            if (backups.First().CompareTo(prerestore.First())>0) return false;
            return true;
         }

         public void DeleteAllPreRestoreBackups()
         {
            Log.Info("removing all pre restore backups...");
            foreach (String folder in Directory.GetFiles(SAVE.configuration.backupPath + "/" + name))
            {
               if(folder.EndsWith("-R"))
               {
                  Log.Info("delting folder "+folder);
                  Directory.Delete(folder);
               }
            }

         }

         public void RestoreFrom(String backup)
         {
            Log.Info("restoring game '" + this.name + "' from backup " + backup);
            try 
            {
               status = STATUS.RESTORING;
               if(!LastBackupWasPreRestore())
               {
                  DeleteAllPreRestoreBackups();
                  CreateBackup(true);
                  if(status==STATUS.CORRUPT)
                  {
                     Log.Error("save game is corrupted; aborting restore");
                     return;
                  }
               }
               DeleteSaveGameFiles();
               CopyGameFilesFromBackup(backup);
               status = STATUS.OK;
            }
            catch
            {
               Log.Error("save game is corrupted; restore failed");
               status = STATUS.CORRUPT;
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

            for (int i = 0; i < totalBackupCount - minNumberOfBackups; i++)
            {
               String folder = backupFolders[i];
               String backupName = Path.GetFileName(folder);
               DateTime t = GetBackupTimeForFolder(backupName);
               // backup has to be kept, because of time constraints
               if ( t > timeOfObsoleteBackups && daysToKeepBackups>0 ) continue;
               // all remaining backups kept because of number constraint
               if (totalBackupCount - i < maxNumberOfBackups) break;
               //
               // delete this backup
               try
               {
                  Directory.Delete(folder);
                  backups.Remove(backupName);
               }
               catch
               {
                  Log.Error("failed to cleanup folder "+folder);
               }
            }
            CreateBackupArray();
         }
      }


   }
}