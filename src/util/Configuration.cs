using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Nereid;

namespace Nereid
{
   namespace SAVE
   {
      public class Configuration
      {
         private static readonly String ROOT_PATH = KSPUtil.ApplicationRootPath;
         private static readonly String CONFIG_BASE_FOLDER = ROOT_PATH + "/GameData/";
         private static readonly String FILE_NAME = "S.A.V.E.dat";

         private Log.LEVEL logLevel = Log.LEVEL.INFO;


         // backup interval
         public enum BACKUP_INTERVAL { EACH_SAVE = 0, ONCE_PER_HOUR = 1, ONCE_PER_DAY = 2, ONCE_PER_WEEK=3 }

         public String backupPath { get; set; }
         public BACKUP_INTERVAL backupInterval { get; set; }
         public int daysToKeepBackups { get; set; }
         public int minNumberOfBackups { get; set; }
         public int maxNumberOfBackups { get; set; }


         public Configuration()
         {
            backupInterval = BACKUP_INTERVAL.ONCE_PER_DAY;
            backupPath = "./backup";
            daysToKeepBackups = 14;
         }


         public void Save()
         {
            String filename = CONFIG_BASE_FOLDER + FILE_NAME;
            Log.Info("storing configuration in " + filename);
            try
            {
               using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
               {
                  writer.Write((Int16)logLevel);
                  //
                  writer.Write(backupPath);
                  //
                  writer.Write((Int16)backupInterval);
                  //
                  writer.Write((Int16)daysToKeepBackups);
                  //
                  writer.Write((Int16)minNumberOfBackups);
                  //
                  writer.Write((Int16)maxNumberOfBackups);
               }
            }
            catch
            {
               Log.Error("saving configuration failed");
            }
         }

         public void Load()
         {
            String filename = CONFIG_BASE_FOLDER+FILE_NAME;
            try
            {
               if (File.Exists(filename))
               {
                  Log.Info("loading configuration from " + filename);
                  using (BinaryReader reader = new BinaryReader(File.OpenRead(filename)))
                  {
                     logLevel = (Log.LEVEL) reader.ReadInt16();
                     //
                     backupPath = reader.ReadString();
                     //
                     backupInterval = (BACKUP_INTERVAL)reader.ReadUInt16();
                     //
                     daysToKeepBackups = reader.ReadUInt16();
                     //
                     minNumberOfBackups = reader.ReadUInt16();
                     //
                     maxNumberOfBackups = reader.ReadUInt16();
                  }
               }
               else
               {
                  Log.Info("no config file: default configuration");
               }
            }
            catch
            {
               Log.Warning("loading configuration failed or incompatible file");
            }
         }
      }
   }
}
