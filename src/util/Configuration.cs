using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nereid
{
   namespace SAVE
   {
      public class Configuration
      {
         private static readonly String FILE_NAME = "S.A.V.E.dat";

         public Log.LEVEL logLevel { get; set; }


         // backup interval
         public enum BACKUP_INTERVAL { EACH_SAVE = 0, ONCE_IN_10_MINUTES = 1, ONCE_IN_30_MINUTES = 2, ONCE_PER_HOUR = 3, ONCE_PER_DAY = 4, ONCE_PER_WEEK = 5, ONCE_IN_2_HOURS = 6, ONCE_IN_4_HOURS = 7, CUSTOM = 8, ON_QUIT=9 }
         public  enum WindowPos {UpperRight = 0, UpperLeft = 1, Floating= 2 };

         public String backupPath { get; set; }
         public WindowPos windowPosition { get; set; } = WindowPos.UpperRight;
         public BACKUP_INTERVAL backupInterval { get; set; }
         public int daysToKeepBackups { get; set; }
         public int minNumberOfBackups { get; set; }
         public int maxNumberOfBackups { get; set; }
         public bool recurseBackup { get; set; }
         public bool asynchronous { get; set; }
         public int customBackupInterval { get; set; }
         public bool compressBackups { get; set; }
         public bool disabled { get; set; }

         // non persistent, will reset to default every start of KSP
         public bool backupBeforeRestore { get; set; }


         public Configuration()
         {
            logLevel = Log.LEVEL.INFO;
            backupInterval = BACKUP_INTERVAL.ONCE_PER_HOUR;
            backupPath = "./backup";
            daysToKeepBackups = 14;
            minNumberOfBackups = 20;
            maxNumberOfBackups = 200;
            recurseBackup = true;
            customBackupInterval = 1;
            backupBeforeRestore = true;
            asynchronous = false;
            compressBackups = false;
            windowPosition = WindowPos.UpperRight;
            disabled = false;
         }


         public void Save()
         {
            FileOperations.SaveConfiguration(this, FILE_NAME);
         }

         public void Load()
         {
            FileOperations.LoadConfiguration(this, FILE_NAME);
         }
      }
   }
}
