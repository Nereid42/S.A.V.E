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
            Log.Info("start");
            configuration.Load();
            if (this.gui==null)
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
