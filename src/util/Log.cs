using System;
using UnityEngine;

namespace Nereid
{
   namespace SAVE
   {
      public static class Log
      {
         public enum LEVEL { OFF=0, ERROR=1, WARNING=2, INFO=3, DETAIL=4, TRACE=5 };

         public  static LEVEL level = LEVEL.INFO ;

         private static readonly String PREFIX = "S.A.V.E: ";

         public static LEVEL GetLevel()
         {
            return level;
         }

         public static void SetLevel(LEVEL level)
         {
            Debug.Log("log level "+level);
            Log.level = level;
         }

         public static LEVEL GetLogLevel()
         {
            return level;
         }

         private static bool IsLevel(LEVEL level)
         {
            return level == Log.level;
         }

         public static bool IsLogable(LEVEL level)
         {
            return level <= Log.level;
         }

         private static void LogOnMainThread(String msg)
         {
            if(SAVE.configuration.asynchronous)
            {
               MainThreadDispatcher.RunOnMainThread( () => { Debug.Log(msg); } );
            }
            else
            {
               Debug.Log(msg);
            }
         }

         public static void Trace(String msg)
         {
            if (IsLogable(LEVEL.TRACE))
            {
               LogOnMainThread(PREFIX + msg);
            }
         }

         public static void Detail(String msg)
         {
            if (IsLogable(LEVEL.DETAIL))
            {
               LogOnMainThread(PREFIX + msg);
            }
         }


         public static void Info(String msg)
         {
            if (IsLogable(LEVEL.INFO))
            {
               LogOnMainThread(PREFIX + msg);
            }
         }

         // for debuging only; calls should be removed for release
         public static void Test(String msg)
         {
            //if (IsLogable(LEVEL.INFO))
            {
               if (SAVE.configuration.asynchronous)
               {
                  MainThreadDispatcher.RunOnMainThread(() => { Debug.LogWarning(PREFIX + "TEST:" + msg); });
               }
               else
               {
                  Debug.LogWarning(PREFIX + "TEST:" + msg);
               }
            }
         }

         public static void Warning(String msg)
         {
            if (IsLogable(LEVEL.WARNING))
            {
               if (SAVE.configuration.asynchronous)
               {
                  MainThreadDispatcher.RunOnMainThread(() => { Debug.LogWarning(PREFIX + msg); });
               }
               else
               {
                  Debug.LogWarning(PREFIX + msg); ;
               }               
            }
         }

         public static void Error(String msg)
         {
            if (IsLogable(LEVEL.ERROR))
            {
               if (SAVE.configuration.asynchronous)
               {
                  MainThreadDispatcher.RunOnMainThread(() => { Debug.LogError(PREFIX + msg); });
               }
               else
               {
                  Debug.LogError(PREFIX + msg);
               }
            }
         }

         public static void Exception(Exception e)
         {
            String msg = PREFIX + "exception caught: " + e.GetType() + ": " + e.Message;
            if (IsLogable(LEVEL.ERROR))
            {
               if (SAVE.configuration.asynchronous)
               {
                  MainThreadDispatcher.RunOnMainThread(() => { Debug.LogError(msg); });
               }
               else
               {
                  Log.Error(msg);
               }
            }
         }

      }
   }
}
