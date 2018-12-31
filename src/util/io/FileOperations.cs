// just uncomment this line to restrict file access to KSP installation folder
#define _UNLIMITED_FILE_ACCESS
// for debugging
// #define _DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Nereid.SevenZip.Compression.LZMA;


namespace Nereid
{
   namespace SAVE
   {
      public static class FileOperations
      {
         private const String COMPRESSED_SUFFIX = ".save.compressed";

         private static readonly String ROOT_PATH = KSPUtil.ApplicationRootPath;
         private static readonly String CONFIG_BASE_FOLDER = ROOT_PATH + "/GameData/";

         public static bool InsideApplicationRootPath(String path)
         {
            if (path == null) return false;
            try
            {
               String fullpath = Path.GetFullPath(path);
               return fullpath.StartsWith(Path.GetFullPath(ROOT_PATH));
            }
            catch
            {
               return false;
            }
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

         public static void CopyDirectory(String from, String to, String excludemarkerfile = ".nobackup")
         {    
            if (FileExists(from+"/"+excludemarkerfile))
            {
               Log.Info("directory '" + from + "' excluded from backup (marked by file)");
               return;
            }
            Log.Detail("no exclude marker file '"+ excludemarkerfile+"' found in folder '"+from+"'");

            string dirName = new DirectoryInfo(from).Name;
            foreach (var e in S.A.V.E.src.util.io.ConfigNodeIO.excludes)
            {
               if (dirName == e)
               {
                  Log.Info("directory '" + dirName + "' excluded from backup (excluded by config)");
                  return;
               }
            }
            Log.Detail("folder '" + from + "' not in exclude list");

            CheckPathForWriteOperation(to);

            Log.Info("copy directory " + from + " to " + to);

            // create target directory if not existient
            if (!DirectoryExists(to))
            {
               CreateDirectoryRetry(to);
            }

            String[] files = GetFiles(from);
            foreach (String file in files)
            {
               String name = GetFileName(file);
               CopyFileRetry(file, to + "/" + name);
            }
            String[] folders = GetDirectories(from);
            foreach (String folder in folders)
            {
               String name = GetFileName(folder);
               CreateDirectoryRetry(to + "/" + name);
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

         public static void CreateDirectoryRetry(String directory, int retries=3, int delayinMillis = 500)
         {
            do
            {
               try
               {
                  CreateDirectory(directory);
                  return;
               }
               catch(Exception e)
               {
                  Log.Exception(e);
                  if (retries > 0 && SAVE.configuration.asynchronous)
                  {
                     retries--;
                     Log.Info("retrying operation: create directory in " + delayinMillis+" ms");
                     Thread.Sleep(delayinMillis);
                  }
                  else
                  {
                     throw e;
                  }
               }
            }  while (retries > 0);
         }

         public static void CopyFileRetry(String from, String to, int retries = 6, int delayinMillis = 200)
         {
            do
            {
               try
               {
                  CopyFile(from, to);
                  return;
               }
               catch (Exception e)
               {
                  Log.Exception(e);
                  if (retries > 0 && SAVE.configuration.asynchronous)
                  {
                     retries--;
                     Log.Info("retrying operation: copy file in " + delayinMillis + " ms");
                     Thread.Sleep(delayinMillis);
                  }
                  else
                  {
                     throw e;
                  }
               }
            } while (retries > 0);
         }

         public static void DeleteFileRetry(String file, int retries = 6, int delayinMillis = 100)
         {
            do
            {
               try
               {
                  DeleteFile(file);
                  return;
               }
               catch (Exception e)
               {
                  Log.Exception(e);
                  if (retries > 0 && SAVE.configuration.asynchronous)
                  {
                     retries--;
                     Log.Info("retrying operation: delete file in " + delayinMillis + " ms");
                     Thread.Sleep(delayinMillis);
                  }
                  else
                  {
                     throw e;
                  }
               }
            } while (retries > 0);
         }

         public static void DeleteDirectoryRetry(String directory, int retries = 6, int delayinMillis = 100)
         {
            do
            {
               try
               {
                  DeleteDirectory(directory);
                  return;
               }
               catch (Exception e)
               {
                  Log.Exception(e);
                  if (retries > 0 && SAVE.configuration.asynchronous)
                  {
                     retries--;
                     Log.Info("retrying operation: delete directory in " + delayinMillis + " ms");
                     Thread.Sleep(delayinMillis);
                  }
                  else
                  {
                     throw e;
                  }
               }
            } while (retries > 0);
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

         public static String ExpandBackupPath(String path)
         {
            if (path == null) return KSPUtil.ApplicationRootPath;
            path = path.Trim();
            if (path.StartsWith("./") || path.StartsWith(".\\"))
            {
               path = KSPUtil.ApplicationRootPath + path.Substring(2);
            }
            return path;
         }

#if (_DEBUG)
         /**
          * Used for debugging purposes only
          */
         public static void AppendText(String filename, String text)
         {
            using (StreamWriter sw = File.AppendText(filename))
            {
               sw.WriteLine(text);
               sw.Flush();
            }
         }
#endif

         public static bool CompressFolder(DirectoryInfo folder)
         {
            try
            {
               foreach (FileInfo file in folder.GetFiles())
               {
                  if (!file.Name.EndsWith(COMPRESSED_SUFFIX) && !file.Name.StartsWith(".") && file.Length>0)
                  {
                     CompressAndDeleteFile(file);
                  }
               }
               foreach (DirectoryInfo subfolder in folder.GetDirectories())
               {
                  CompressFolder(subfolder);
               }
            }
            catch (Exception e)
            {
               Log.Error("failed to compress folder " + folder.Name + ": " + e.Message);
               return false;
            }
            return true;
         }


         public static bool CompressFolder(String path)
         {
            return CompressFolder(new DirectoryInfo(path));
         }

         public static bool DecompressFolder(DirectoryInfo folder)
         {
            Log.Detail("decompressing folder " + folder.Name);
            try
            {
               foreach (FileInfo file in folder.GetFiles())
               {
                  if (file.Name.EndsWith(COMPRESSED_SUFFIX))
                  {
                     DecompressAndDeleteFile(file);
                  }
               }
               foreach (DirectoryInfo subfolder in folder.GetDirectories())
               {
                  DecompressFolder(subfolder);
               }
            }
            catch (Exception e)
            {
               Log.Error("failed to decompress folder " + folder.Name+": "+e.Message);
               return false;
            }
            return true;
         }

         public static bool DecompressFolder(String path)
         {
            return DecompressFolder(new DirectoryInfo(path));
         }

         public static void CompressAndDeleteFile(FileInfo file)
         {
            CheckPathForWriteOperation(file.FullName);
            Log.Detail("compressing file " + file.Name);
            CompressFile(file);
            Log.Detail("deleting file " + file.Name);
            file.Delete();
            Log.Detail(file.Name + " compressed and deleted");
         }

         public static void DecompressAndDeleteFile(FileInfo file)
         {
            CheckPathForWriteOperation(file.FullName);
            Log.Detail("decompressing file " + file.Name);
            DecompressFile(file);
            Log.Detail("deleting file " + file.Name);
            file.Delete();
            Log.Detail(file.Name+" decompressed and deleted");
         }

         public static void CompressFile(FileInfo fi)
         {
            try
            {
               String outputName = fi.FullName + COMPRESSED_SUFFIX;
               using (FileStream inStream = fi.OpenRead())
               {
                  using (FileStream outStream = new FileStream(outputName, FileMode.Create, FileAccess.Write))
                  {
                     Encoder encoder = new Encoder();
                     BinaryWriter writer = new BinaryWriter(outStream);
                     writer.Write(fi.Length);
                     encoder.SetCoderProperties(CompressionConstants.propIDs, CompressionConstants.properties);
                     encoder.WriteCoderProperties(outStream);
                     encoder.Code(inStream, outStream, -1, -1, null);
                  }
               }
            }
            catch (Exception e)
            {
               System.Console.WriteLine("Exception CompressFile: " + e.GetType() + " " + e.Message);
               throw e;
            }
         }

         public static void DecompressFile(FileInfo fi)
         {
            try
            {
               String outputName = fi.FullName.Substring(0, fi.FullName.Length - COMPRESSED_SUFFIX.Length);
               using (FileStream inStream = fi.OpenRead())
               {
                  using (FileStream outStream = new FileStream(outputName, FileMode.Create, FileAccess.Write))
                  {
                     Decoder decoder = new Decoder();
                     BinaryReader reader = new BinaryReader(inStream);
                     long outSize = reader.ReadInt64();
                     long compressedSize = inStream.Length - inStream.Position;
                     byte[] properties = new byte[5];
                     if (inStream.Read(properties, 0, 5) != 5)
                        throw (new Exception("input is too short"));
                     decoder.SetDecoderProperties(properties);
                     decoder.Code(inStream, outStream, compressedSize, outSize, null);
                  }
               }
            }
            catch (Exception e)
            {
               System.Console.WriteLine("Exception DecompressFile: " + e.GetType() + " " + e.Message);
               throw e;
            }
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
                  //
                  writer.Write((Int16)configuration.customBackupInterval);
                  //
                  writer.Write(configuration.asynchronous);
                  //
                  writer.Write(configuration.compressBackups);
               }
            }
            catch(Exception e)
            {
               Log.Exception(e);
               Log.Error("saving configuration failed");
            }
            finally
            {
               configuration.backupPath = ExpandBackupPath(configuration.backupPath);
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
                     //
                     configuration.customBackupInterval = reader.ReadUInt16();
                     //
                     configuration.asynchronous = reader.ReadBoolean();
                     //
                     configuration.compressBackups = reader.ReadBoolean();
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
