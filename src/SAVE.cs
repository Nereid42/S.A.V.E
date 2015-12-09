using System;
using UnityEngine;
using KSP.IO;
namespace Nereid
{
   namespace SAVE
   {
      [KSPAddon(KSPAddon.Startup.Instantly, true)]
      public class SAVE : MonoBehaviour
      {
         public static readonly Configuration configuration = new Configuration();

         public static readonly BackupManager manager = new BackupManager();

         static SAVE()
         {
         }

         private MainMenuGui gui;

         public SAVE()
         {
            Log.Info("new instance of S.A.V.E");
         }

         public void Awake()
         {
            Log.Info("awake");

            DontDestroyOnLoad(this);
         }

         public void Start()
         {
            Log.SetLevel(Log.LEVEL.INFO);
            Log.Info("start");
            configuration.Load();
            Log.SetLevel(configuration.logLevel);
            if (this.gui == null)
            {
               this.gui = this.gameObject.AddComponent<MainMenuGui>();
               this.gui.SetVisible(true);
               manager.ScanSavegames();
               manager.Start();
               RegisterEvents();
            }
         }

         private void RegisterEvents()
         {
            Log.Info("registering events");
            GameEvents.onGameStateSaved.Add(manager.CallbackGameSaved);
            GameEvents.onGameSceneLoadRequested.Add(this.CallbackGameSceneLoadRequested);
         }

         private void CallbackGameSceneLoadRequested(GameScenes scene)
         {
            this.gui.SetVisible(scene == GameScenes.MAINMENU);
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER && scene == GameScenes.MAINMENU && configuration.backupInterval == Configuration.BACKUP_INTERVAL.ON_QUIT)
            {
               if (!SAVE.configuration.disabled)
               {
                  String game = HighLogic.SaveFolder;
                  if (name != null && name.Length > 0)
                  {
                     manager.BackupGame(game);
                  }
                  else
                  {
                     Log.Warning("failed to save game on quit");
                  }
               }
               else
               {
                  Log.Info("backup on quit disabled");
               }
            }
         }

         internal void OnDestroy()
         {
            Log.Info("destroying S.A.V.E");
            configuration.Save();
            manager.Stop();
         }

      }
   }
}
