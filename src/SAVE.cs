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
            //configuration = new Configuration();
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
            GameEvents.onGameStateSaved.Add(manager.CallbackGameSaved);
            GameEvents.onGameSceneLoadRequested.Add(this.CallbackGameSceneLoadRequested);
         }

         private void CallbackGameSceneLoadRequested(GameScenes scene)
         {
            Log.Info("SAVE::onGameSceneLoadRequested ");
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
