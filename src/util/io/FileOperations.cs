// just uncomment this line to allow file access anywhere on the file system
#define _UNLIMITED_FILE_ACCESS

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

         public static bool InsideApplicationRootPath(String path)
         {
            String fullpath = Path.GetFullPath(path);
            return fullpath.StartsWith(Path.GetFullPath(KSPUtil.ApplicationRootPath));
         }

         public static bool ValidPathForWriteOperation(String path)
         {
#if (_UNLIMITED_FILE_ACCESS)
            return true;
#else
            String fullpath = Path.GetFullPath(path);
            return InsideApplicationRootPath(fullpath);
#endif
         }

         private static void CheckPathForWriteOperation(String path)
         {
            if (!ValidPathForWriteOperation(path))
            {
               Log.Error("invalid write path: "+path);
               throw new InvalidOperationException("write path outside KSP home folder: "+path);
            }
         }


         public static void DeleteFile(String file)
         {
            CheckPathForWriteOperation(file);
            Log.Info("deleting file " + file);
            File.Delete(file);
         }

         public static void CopyFile(String from, String to)
         {
            CheckPathForWriteOperation(to);
            Log.Info("copy file " + from + " to " + to);
            File.Copy(from, to);
         }

         public static void CopyDirectory(String from, String to)
         {
            CheckPathForWriteOperation(to);
            Log.Info("copy directory " + from + " to " + to);
            String[] files = GetFiles(from);
            foreach(String file in files)
            {
               String name = GetFileName(file);
               CopyFile(file, to + "/" + name);
            }
            String[] folders = GetDirectories(from);
            foreach (String folder in folders)
            {
               String name = GetFileName(folder);
               CreateDirectory(to + "/" + name);
               CopyDirectory(folder, to + "/" + name);
            }
         }


         public static void CreateDirectory(String directory)
         {
            CheckPathForWriteOperation(directory);
            Log.Info("creating directory " + directory);
            Directory.CreateDirectory(directory);
         }

         public static void DeleteDirectory(String directory)
         {
            CheckPathForWriteOperation(directory);
            Log.Info("deleting directory " + directory);
            Directory.Delete(directory, true);
         }

         public static void CreateFile(String file)
         {
            CheckPathForWriteOperation(file);
            Log.Info("creating file " + file);
            File.Create(file);
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
                  //
                  writer.Write(configuration.recurseBackup);
               }
            }
            catch(Exception e)
            {
               Log.Exception(e);
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
                     //
                     configuration.recurseBackup = reader.ReadBoolean();
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
