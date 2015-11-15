using System;
using UnityEngine;

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

         private enum DISPLAY { HIDDEN = 0, BACKUP = 1, RESTORE = 2, CONFIGURE = 3, STATUS = 4, RESTORING = 5 };
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
               if (!SAVE.manager.RestoreCompleted())
               {
                  GUI.enabled = false;
               }
               if (GUILayout.Button("Backup All", GUI.skin.button))
               {
                  display = DISPLAY.BACKUP;
                  // don't start another backup if there is still a backup running
                  if (SAVE.manager.BackupsCompleted())
                  {
                     backupCount = SAVE.manager.BackupAll();
                     backupCloseTime = DateTime.Now.AddSeconds(BACKUP_DISPLAY_REMAINS_OPEN_TIME);
                  }
                  else
                  {
                     Log.Warning("won't start another backup until all backups finished");
                  }
               }
               GUI.enabled = true;
               // Restore
               if(DrawDisplayToggle("Restore", DISPLAY.RESTORE) && !SAVE.manager.RestoreCompleted())
               {
                  display = DISPLAY.RESTORING;
               }
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
                  case DISPLAY.RESTORING:
                     DisplayRestoring();
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
                  case DISPLAY.HIDDEN:
                     // are we ingame? then make the window disappear (this shouldn't be neccessary, but just to be sure...)
                     if(HighLogic.LoadedScene == GameScenes.MAINMENU)
                     {
                        SetVisible(true);
                     }
                     else
                     {
                        SetVisible(false);
                     }
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

         private void DisplayRestoring()
         {
            String game = SAVE.manager.GetRestoredGame();
            DrawTitle("Restoring game " + game);
            bool completed = SAVE.manager.RestoreCompleted();
            GUILayout.BeginHorizontal();
            GUI.enabled = completed;
            if (completed)
            {
               GUILayout.Label("Restore complete");
            }
            else
            {
               GUILayout.Label("Restoring...");
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("CLOSE"))
            {
               display = DISPLAY.HIDDEN;
            }
            GUI.enabled = true;
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
            String game = games[selectedGameToRestore];
            GUILayout.EndScrollView();
            BackupSet backupSet = SAVE.manager.GetBackupSetForName(games[selectedGameToRestore]);
            String[] backups = backupSet.GetBackupsAsArray();
            GUILayout.Label("From backup", HighLogic.Skin.label);
            backupListscrollPosition = GUILayout.BeginScrollView(backupListscrollPosition, GUI.skin.box, GUILayout.Height(210));
            selectedBackupToRestore = GUILayout.SelectionGrid(selectedBackupToRestore, backups, 1);
            String backup = backups.Length>0?backups[selectedBackupToRestore]:"";
            GUILayout.EndScrollView();
            SAVE.configuration.backupBeforeRestore = GUILayout.Toggle(SAVE.configuration.backupBeforeRestore, "Create a backup before restore");
            GUILayout.BeginHorizontal();
            GUILayout.Label("");
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("Cancel"))
            {
               display = DISPLAY.HIDDEN;
            }
            GUI.enabled = backups.Length>0;
            if(GUILayout.Button("RESTORE"))
            {
               if(SAVE.manager.RestoreGame(game, backup))
               {
                  display = DISPLAY.RESTORING;
               }
            }
            GUI.enabled = true;
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
            STYLE_BACKUPSET_STATUS.fixedWidth = 70;
            STYLE_RECOVER_BUTTON.stretchWidth = false;
            STYLE_RECOVER_BUTTON.fixedWidth = 80;
            DrawTitle("Games");
            foreach (BackupSet set in SAVE.manager)
            {
               GUILayout.BeginHorizontal();
               GUILayout.Label(set.name, STYLE_BACKUPSET_NAME);
               GUILayout.Label(set.status.ToString(), STYLE_BACKUPSET_STATUS);
               GUI.enabled = SAVE.manager.RestoreCompleted() && SAVE.manager.BackupsCompleted() && set.status != BackupSet.STATUS.NONE;
               if(GUILayout.Button("Restore", STYLE_RECOVER_BUTTON))
               {
                  String[] sets = SAVE.manager.GetBackupSetNameArray();
                  selectedGameToRestore = IndexOf(set.name, sets);
                  display = DISPLAY.RESTORE;
               }
               GUI.enabled = true;
               GUILayout.EndHorizontal();
            }
         }

         private int IndexOf(String s, String[] a)
         {
            try
            {
               for (int i = 0; i < a.Length; i++)
               {
                  if (a[i].Equals(s))
                  {
                     return i;
                  }
               }
            }
            catch
            {
               Log.Error("internal error in IndexOf "+s+", a["+a.Length+"]");
            }
            return 0;
         }

         private void DisplayConfigure()
         {
            Configuration config = SAVE.configuration;
            GUIStyle STYLE_BACKUP_PATH_LABEL = new GUIStyle(GUI.skin.label);
            GUIStyle STYLE_BACKUP_PATH_FIELD = new GUIStyle(GUI.skin.textField);
            STYLE_BACKUP_PATH_FIELD.stretchWidth = false;
            STYLE_BACKUP_PATH_FIELD.fixedWidth = 190;
            GUIStyle STYLE_TEXTFIELD = new GUIStyle(GUI.skin.textField);
            STYLE_TEXTFIELD.stretchWidth = false;
            STYLE_TEXTFIELD.fixedWidth = 60;
            //
            GUILayout.BeginVertical();
            DrawTitle("Configuration");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Log:");
            LogLevelButton(Log.LEVEL.OFF, "OFF");
            LogLevelButton(Log.LEVEL.ERROR, "ERROR");
            LogLevelButton(Log.LEVEL.WARNING, "WARNING");
            LogLevelButton(Log.LEVEL.INFO, "INFO");
            LogLevelButton(Log.LEVEL.DETAIL, "DETAIL");
            LogLevelButton(Log.LEVEL.TRACE, "TRACE");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (FileOperations.ValidPathForWriteOperation(config.backupPath))
            {
               if (FileOperations.InsideApplicationRootPath(config.backupPath))
               {
                  STYLE_BACKUP_PATH_FIELD.normal.textColor = GUI.skin.textField.normal.textColor;
                  STYLE_BACKUP_PATH_LABEL.normal.textColor = GUI.skin.label.normal.textColor;
               }
               else
               {
                  STYLE_BACKUP_PATH_FIELD.normal.textColor = Color.yellow;
                  STYLE_BACKUP_PATH_LABEL.normal.textColor = Color.yellow;
               }
            }
            else
            {
               STYLE_BACKUP_PATH_FIELD.normal.textColor = Color.red;
               STYLE_BACKUP_PATH_LABEL.normal.textColor = Color.red;
            }
            GUILayout.Label("Backup path: ", STYLE_BACKUP_PATH_LABEL);
            config.backupPath = FileOperations.ExpandBackupPath(GUILayout.TextField(config.backupPath, STYLE_BACKUP_PATH_FIELD));
            GUILayout.EndHorizontal();
            // async
            config.asynchronous = GUILayout.Toggle(config.asynchronous, " Asynchronous backup/restore");
            // recurse
            config.recurseBackup = GUILayout.Toggle(config.recurseBackup, " Recurse subfolders");
            // compress
            //GUI.enabled = false;
            //config.compressBackups = GUILayout.Toggle(config.compressBackups, " Compress backups");
            //GUI.enabled = true;
            // interval
            GUILayout.Label("Backup interval: ");
            BackupIntervalToggle(Configuration.BACKUP_INTERVAL.ON_QUIT, "On quit");
            BackupIntervalToggle(Configuration.BACKUP_INTERVAL.EACH_SAVE, "Each save");
            BackupIntervalToggle(Configuration.BACKUP_INTERVAL.ONCE_IN_10_MINUTES, "Once in 10 minutes");
            BackupIntervalToggle(Configuration.BACKUP_INTERVAL.ONCE_IN_30_MINUTES, "Once in 30 minutes");
            BackupIntervalToggle(Configuration.BACKUP_INTERVAL.ONCE_PER_HOUR, "Once per hour");
            BackupIntervalToggle(Configuration.BACKUP_INTERVAL.ONCE_IN_2_HOURS, "Once in 2 hours");
            BackupIntervalToggle(Configuration.BACKUP_INTERVAL.ONCE_IN_4_HOURS, "Once in 4 hours");
            BackupIntervalToggle(Configuration.BACKUP_INTERVAL.ONCE_PER_DAY, "Once per day");
            BackupIntervalToggle(Configuration.BACKUP_INTERVAL.ONCE_PER_WEEK, "Once per week");
            GUILayout.BeginHorizontal();
            BackupIntervalToggle(Configuration.BACKUP_INTERVAL.CUSTOM, "Custom (minutes)");
            GUILayout.FlexibleSpace();
            String sCustomInterval = GUILayout.TextField(config.customBackupInterval.ToString(), STYLE_TEXTFIELD);
            config.customBackupInterval = ParseInt(sCustomInterval);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Days to keep backups: ");
            String sDaysToKeepBackups = GUILayout.TextField(config.daysToKeepBackups.ToString(), STYLE_TEXTFIELD);
            config.daysToKeepBackups = ParseInt(sDaysToKeepBackups);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Min number of backups: ");
            String sMinNumberOfbackups = GUILayout.TextField(config.minNumberOfBackups.ToString(), STYLE_TEXTFIELD);
            config.minNumberOfBackups = ParseInt(sMinNumberOfbackups);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Max number of backups: ");
            String sMaxNumberOfbackups = GUILayout.TextField(config.maxNumberOfBackups.ToString(), STYLE_TEXTFIELD);
            config.maxNumberOfBackups = ParseInt(sMaxNumberOfbackups);
            GUILayout.EndHorizontal();
         }

         private int ParseInt(String s)
         {
            try
            {
               return int.Parse(s);
            }
            catch(NotFiniteNumberException)
            {
               Log.Warning("invalid number format: " + s);
               return 0;
            }
         }

         private void BackupIntervalToggle(Configuration.BACKUP_INTERVAL interval, String text)
         {
            if (GUILayout.Toggle(SAVE.configuration.backupInterval == interval, " "+text))
            {
               SAVE.configuration.backupInterval = interval;
            }
         }

         private void LogLevelButton(Log.LEVEL level, String text)
         {
            if (GUILayout.Toggle(Log.GetLevel() == level, text,GUI.skin.button) && Log.GetLevel() != level)
            {
               SAVE.configuration.logLevel = level;
               Log.SetLevel(level);
            }
         }

         public void SetVisible(bool visible)
         {
            this.visible = visible;
         }
      }
   }
}
