using System;
using UnityEngine;
using KSP.IO;

namespace Nereid
{
   namespace SAVE
   {
      class MainMenuGui : MonoBehaviour
      {
         private const String TITLE = "S.A.V.E - Automatic Backup System";
         private const int WIDTH = 300;
         private const int BACKUP_DISPLAY_REMAINS_OPEN_TIME = 5;

         private static readonly Rect RECT_GAME_CHOOSER = new Rect(0,10,WIDTH-20, 150);

         private Rect bounds = new Rect(0, 0, WIDTH, 0);
         private Vector2 gameListscrollPosition = Vector2.zero;
         private Vector2 backupListscrollPosition = Vector2.zero;

         private enum DISPLAY { HIDDEN = 0, BACKUP = 1, RESTORE = 2, CONFIGURE = 3, STATUS = 4 };
         private DISPLAY display = DISPLAY.HIDDEN;


         private int selectedGameToRestore = 0;
         private int selectedBackupToRestore = 0;

         // for All backup dialog
         private int backupCount = 0;
         private DateTime backupCloseTime;

         private volatile bool visible = false;

         // 
         protected void OnGUI()
         {
            try
            {
               if (visible)
               {
                  this.bounds = GUILayout.Window(this.GetInstanceID(), this.bounds, this.Window, TITLE, HighLogic.Skin.window);
                  bounds.x = Screen.width - bounds.width;
               }
            }
            catch (Exception e)
            {
               Log.Error("exception: "+e.Message);
            }
         }

         private void Window(int id)
         {
            Configuration config = SAVE.configuration;

            DISPLAY lastDisplay = display;


            try
            {
               GUILayout.BeginVertical();
               GUILayout.BeginHorizontal();
               if (GUILayout.Button("Backup All", GUI.skin.button))
               {
                  display = DISPLAY.BACKUP;
                  backupCount = SAVE.manager.BackupAll();
                  backupCloseTime = DateTime.Now.AddSeconds(BACKUP_DISPLAY_REMAINS_OPEN_TIME);
               }
               // Restore
               DrawDisplayToggle("Restore", DISPLAY.RESTORE);
               // Configure
               DrawDisplayToggle("Configure", DISPLAY.CONFIGURE);
               // Status
               DrawDisplayToggle("Status", DISPLAY.STATUS);
               // Hide
               DrawDisplayToggle("Hide", DISPLAY.HIDDEN);
               GUILayout.EndHorizontal();
               //
               switch (display)
               {
                  case DISPLAY.BACKUP:
                     DisplayBackup();
                     break;
                  case DISPLAY.RESTORE:
                     DisplayRestore();
                     break;
                  case DISPLAY.CONFIGURE:
                     DisplayConfigure();
                     break;
                  case DISPLAY.STATUS:
                     DisplayStatus();
                     break;
               }
               GUILayout.EndVertical();

               if(display==DISPLAY.BACKUP && backupCloseTime < DateTime.Now && SAVE.manager.Queuedbackups()==0)
               {
                  display = DISPLAY.HIDDEN;
               }

            }
            catch (Exception e)
            {
               Log.Error("exception: " + e.Message);
            }

            // resize GUI if display changes
            if (lastDisplay != display)
            {
               this.bounds.height = 0;
            }
         }

         private bool DrawDisplayToggle(String text, DISPLAY display)
         {
            bool b = GUILayout.Toggle(this.display == display, text, GUI.skin.button);
            if (b)
            {
               this.display = display;
            }
            return b;
         }

         private void DrawTitle(String text)
         {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text, HighLogic.Skin.label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
         }

         private void DisplayBackup()
         {
            GUILayout.BeginHorizontal();
            if (SAVE.manager.Queuedbackups() == 0)
            {
               GUILayout.Label("Done (" + backupCount+" games)");
            }
            else
            {
               GUILayout.Label(SAVE.manager.Queuedbackups().ToString() + "/" + backupCount+" backups in queue");
            }
            GUILayout.FlexibleSpace();
            TimeSpan open = backupCloseTime - DateTime.Now;
            String dots = "";
            for (int i = 0; i < open.Seconds; i++ )
            {
               dots = dots + ".";
            }
            GUILayout.Label(dots);
            GUILayout.EndHorizontal();
         }

         private void DisplayRestore()
         {
            String[] games = SAVE.manager.GetBackupSetNameArray();

            GUILayout.BeginVertical();
            DrawTitle("Restore game");
            gameListscrollPosition = GUILayout.BeginScrollView(gameListscrollPosition, GUI.skin.box, GUILayout.Height(105));
            selectedGameToRestore = GUILayout.SelectionGrid(selectedGameToRestore, games, 1);
            GUILayout.EndScrollView();
            BackupSet backupSet = SAVE.manager.GetBackupSetForName(games[selectedGameToRestore]);
            String[] backups = backupSet.GetBackupsAsArray();
            GUILayout.Label("From backup", HighLogic.Skin.label);
            backupListscrollPosition = GUILayout.BeginScrollView(backupListscrollPosition, GUI.skin.box, GUILayout.Height(210));
            selectedBackupToRestore = GUILayout.SelectionGrid(selectedBackupToRestore, backups, 1);
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            GUILayout.Label("");
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("Cancel"))
            {
               display = DISPLAY.HIDDEN;
            }
            GUILayout.Button("RESTORE");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
         }

         private void DisplayStatus()
         {
            GUIStyle STYLE_BACKUPSET_NAME = new GUIStyle(GUI.skin.label);
            GUIStyle STYLE_BACKUPSET_STATUS = new GUIStyle(GUI.skin.label);
            GUIStyle STYLE_RECOVER_BUTTON = new GUIStyle(GUI.skin.button);
            STYLE_BACKUPSET_NAME.stretchWidth = false;
            STYLE_BACKUPSET_NAME.fixedWidth = 150;
            STYLE_BACKUPSET_STATUS.stretchWidth = false;
            STYLE_BACKUPSET_STATUS.fixedWidth = 60;
            STYLE_RECOVER_BUTTON.stretchWidth = false;
            STYLE_RECOVER_BUTTON.fixedWidth = 80;
            DrawTitle("Save Games");
            foreach (BackupSet set in SAVE.manager)
            {
               GUILayout.BeginHorizontal();
               GUILayout.Label(set.name, STYLE_BACKUPSET_NAME);
               GUILayout.Label(set.status.ToString(), STYLE_BACKUPSET_STATUS);
               GUILayout.Button("Restore", STYLE_RECOVER_BUTTON);
               GUILayout.EndHorizontal();
            }
         }

         private void DisplayConfigure()
         {
            Configuration config = SAVE.configuration;
            GUIStyle STYLE_BACKUP_PATH = new GUIStyle(GUI.skin.textField);
            STYLE_BACKUP_PATH.stretchWidth = false;
            STYLE_BACKUP_PATH.fixedWidth = 190;
            GUIStyle STYLE_TEXTFIELD = new GUIStyle(GUI.skin.textField);
            STYLE_TEXTFIELD.stretchWidth = false;
            STYLE_TEXTFIELD.fixedWidth = 60;
            //
            GUILayout.BeginVertical();
            DrawTitle("Configuration");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Backup path: ");
            config.backupPath = GUILayout.TextField(config.backupPath, STYLE_BACKUP_PATH);
            GUILayout.EndHorizontal();
            BackupIntervalToggle(Configuration.BACKUP_INTERVAL.EACH_SAVE, "Each save");
            BackupIntervalToggle(Configuration.BACKUP_INTERVAL.ONCE_PER_HOUR, "Once per hour");
            BackupIntervalToggle(Configuration.BACKUP_INTERVAL.ONCE_PER_DAY, "Once per day");
            BackupIntervalToggle(Configuration.BACKUP_INTERVAL.ONCE_PER_WEEK, "Once per week");
            GUILayout.EndVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Days to keep Backups: ");
            String sDaysToKeepBackups = GUILayout.TextField(config.daysToKeepBackups.ToString(), STYLE_TEXTFIELD);
            config.daysToKeepBackups = int.Parse(sDaysToKeepBackups);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Min number of backups: ");
            String sMinNumberOfbackups = GUILayout.TextField(config.minNumberOfBackups.ToString(), STYLE_TEXTFIELD);
            config.minNumberOfBackups = int.Parse(sMinNumberOfbackups);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Max number of backups: ");
            String sMaxNumberOfbackups = GUILayout.TextField(config.maxNumberOfBackups.ToString(), STYLE_TEXTFIELD);
            config.maxNumberOfBackups = int.Parse(sMaxNumberOfbackups);
            GUILayout.EndHorizontal();

         }

         private void BackupIntervalToggle(Configuration.BACKUP_INTERVAL interval, String text)
         {
            if (GUILayout.Toggle(SAVE.configuration.backupInterval == interval, text))
            {
               SAVE.configuration.backupInterval = interval;
            }
         }

         public void SetVisible(bool visible)
         {
            this.visible = visible;
         }
      }
   }
}
