// just uncomment this line to allow file access anywhere on the file system
//#define _UNLIMITED_FILE_ACCESS

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace Nereid
{
   namespace SAVE
   {
      public static class FileOperations
      {
         private static readonly String ROOT_PATH = KSPUtil.ApplicationRootPath;
         private static readonly String CONFIG_BASE_FOLDER = ROOT_PATH + "/GameData/";

         public static void DeleteFile(String file)
         {
            String path = Path.GetFullPath(file);
            Log.Info("deleting file " + file);
#if (_UNLIMITED_FILE_ACCESS)
           File.Delete(file);
#else
           if (path.StartsWith(KSPUtil.ApplicationRootPath))
           {
              File.Delete(file);
           }
           else
           {
              throw new InvalidOperationException("can't delete files outside the KSP home folder");
           }
#endif
         }

         public static void CopyFile(String from, String to)
         {
            String path = Path.GetFullPath(to);
            Log.Info("copy file " + from + " to " + to);
#if (_UNLIMITED_FILE_ACCESS)
            File.Copy(from,to);
#else
            if (path.StartsWith(KSPUtil.ApplicationRootPath))
            {
               File.Copy(from,to);
            }
            else
            {
               throw new InvalidOperationException("can't create files outside the KSP home folder");
            }
#endif
         }

         public static void CreateDirectory(String directory)
         {
            String path = Path.GetFullPath(directory);
            Log.Info("creating directory "+directory);
#if (_UNLIMITED_FILE_ACCESS)
            Directory.CreateDirectory(directory);
#else
            if (path.StartsWith(KSPUtil.ApplicationRootPath))
            {
               Directory.CreateDirectory(directory);
            }
            else
            {
               throw new InvalidOperationException("can't create directories outside the KSP home folder");
            }
#endif
         }

         public static void DeleteDirectory(String directory)
         {
            String path = Path.GetFullPath(directory);
            Log.Info("deleting directory " + directory);
#if (_UNLIMITED_FILE_ACCESS)
            Directory.Delete(directory,true);
#else
            if (path.StartsWith(KSPUtil.ApplicationRootPath))
            {
               Directory.Delete(directory,true);
            }
            else
            {
               throw new InvalidOperationException("can't delete directories outside the KSP home folder");
            }
#endif
         }

         public static String[] GetDirectories(String path)
         {
            return Directory.GetDirectories(path);
         }

         public static String[] GetFiles(String path)
         {
            return Directory.GetFiles(path);
         }

         public static bool FileExists(String file)
         {
            return File.Exists(file);
         }

         public static bool DirectoryExists(String file)
         {
            return Directory.Exists(file);
         }

         public static String GetFileName(String path)
         {
            return Path.GetFileName(path);
         }

         public static void CreateFile(String file)
         {
            String path = Path.GetFullPath(file);
            Log.Info("creating file " + file);

#if (_UNLIMITED_FILE_ACCESS)
            File.Create(file);
#else
            if (path.StartsWith(KSPUtil.ApplicationRootPath))
            {
               File.Create(file);
            }
            else
            {
               throw new InvalidOperationException("can't create files outside the KSP home folder");
            }
#endif
         }

         public static void SaveConfiguration(Configuration configuration, String file)
         {
            String filename = CONFIG_BASE_FOLDER + file;
            Log.Info("storing configuration in " + filename);
            try
            {
               using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
               {
                  writer.Write((Int16)configuration.logLevel);
                  //
                  writer.Write(configuration.backupPath);
                  //
                  writer.Write((Int16)configuration.backupInterval);
                  //
                  writer.Write((Int16)configuration.daysToKeepBackups);
                  //
                  writer.Write((Int16)configuration.minNumberOfBackups);
                  //
                  writer.Write((Int16)configuration.maxNumberOfBackups);
               }
            }
            catch
            {
               Log.Error("saving configuration failed");
            }
         }

         public static void LoadConfiguration(Configuration configuration, String file)
         {
            String filename = CONFIG_BASE_FOLDER + file;
            try
            {
               if (File.Exists(filename))
               {
                  Log.Info("loading configuration from " + filename);
                  using (BinaryReader reader = new BinaryReader(File.OpenRead(filename)))
                  {
                     configuration.logLevel = (Log.LEVEL)reader.ReadInt16();
                     //
                     configuration.backupPath = reader.ReadString();
                     //
                     configuration.backupInterval = (Configuration.BACKUP_INTERVAL)reader.ReadUInt16();
                     //
                     configuration.daysToKeepBackups = reader.ReadUInt16();
                     //
                     configuration.minNumberOfBackups = reader.ReadUInt16();
                     //
                     configuration.maxNumberOfBackups = reader.ReadUInt16();
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
