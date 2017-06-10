using System;
using UnityEngine;

namespace Nereid
{
   namespace SAVE
   {
      class MainMenuGui : MonoBehaviour
      {
         private const String TITLE = "S.A.V.E - Automatic Backup System";
         private const int WIDTH = 400;
         private const int BACKUP_DISPLAY_REMAINS_OPEN_TIME = 5;
         //
         private const int SELECTION_GRID_WIDTH = 362;
         private const int CONFIG_TEXTFIELD_RIGHT_MARGIN = 165;


         private static readonly Rect RECT_GAME_CHOOSER = new Rect(0,10,WIDTH-20, 150);

         private Rect bounds = new Rect(0, 0, WIDTH, 0);
         private Vector2 restoreListscrollPosition = Vector2.zero;
         private Vector2 backupListscrollPosition = Vector2.zero;
         private Vector2 statusListscrollPosition = Vector2.zero;
         private Vector2 cloneListscrollPosition = Vector2.zero;

         private enum DISPLAY { HIDDEN = 0, BACKUP = 1, RESTORE = 2, CONFIGURE = 3, STATUS = 4, RESTORING = 5, CLONE = 6, CLONING = 7 };
         private DISPLAY display = DISPLAY.HIDDEN;

         private GUIStyle STYLE_BACKUPSET_STATUS_NAME = null;
         private GUIStyle STYLE_BACKUPSET_CLONE_NAME = null;
         private GUIStyle STYLE_BACKUPSET_STATUS = null;
         private GUIStyle STYLE_RECOVER_BUTTON = null;
         private GUIStyle STYLE_NAME_TEXTFIELD = null;
         private GUIStyle STYLE_CONFIG_BACKUP_PATH_LABEL = null;
         private GUIStyle STYLE_CONFIG_BACKUP_PATH_FIELD = null;
         private GUIStyle STYLE_CONFIG_TEXTFIELD = null;

         private GUIStyle STYLE_DELETE_BUTTON = null;


         private int selectedGameToRestore = 0;
         private int selectedBackupToRestore = 0;
         private String selectedGameToClone = "";
         private String cloneGameInto = "";
         private bool cloneBackups = false;
         private bool cloneFromBackupEnabled = false;
         private bool cloneFromBackup = false;

         // for All backup dialog
         private int backupCount = 0;
         private DateTime backupCloseTime;

         private volatile bool visible = false;

         public MainMenuGui()
         {

         }

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
               GUI.enabled = SAVE.manager.RestoreCompleted() && SAVE.manager.BackupsCompleted();
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
               DrawDisplayToggle("Clone", DISPLAY.CLONE);
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
                  case DISPLAY.CLONE:
                     DisplayClone();
                     break;
                  case DISPLAY.CLONING:
                     DisplayCloning();
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
            InitStyles();

            String[] games = SAVE.manager.GetBackupSetNameArray();

            GUILayout.BeginVertical();
            DrawTitle("Restore game");
            restoreListscrollPosition = GUILayout.BeginScrollView(restoreListscrollPosition, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.Height(155));
            selectedGameToRestore = GUILayout.SelectionGrid(selectedGameToRestore, games, 1, GUILayout.Width(SELECTION_GRID_WIDTH));
            String game = games[selectedGameToRestore];
            GUILayout.EndScrollView();
            BackupSet backupSet = SAVE.manager.GetBackupSetForName(games[selectedGameToRestore]);
            String[] backups = backupSet.GetBackupsAsArray();
            GUILayout.Label("From backup", HighLogic.Skin.label);
            backupListscrollPosition = GUILayout.BeginScrollView(backupListscrollPosition, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.Height(205));
            selectedBackupToRestore = GUILayout.SelectionGrid(selectedBackupToRestore, backups, 1, GUILayout.Width(SELECTION_GRID_WIDTH));
            String backup = backups.Length>0?backups[selectedBackupToRestore]:"";
            GUILayout.EndScrollView();
            SAVE.configuration.backupBeforeRestore = GUILayout.Toggle(SAVE.configuration.backupBeforeRestore, " Create a backup before restore");
            SAVE.configuration.disabled = GUILayout.Toggle(SAVE.configuration.disabled, " Temporary disable backups until restart");
            GUILayout.BeginHorizontal();
            GUI.enabled = backups.Length > 0;
            GUILayout.Button("Delete",STYLE_DELETE_BUTTON);
            GUI.enabled = true;
            GUILayout.Button("Erase Backup", STYLE_DELETE_BUTTON);
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

         private void InitStyles()
         {
            // for some reasons, this styles cant be created in the constructor
            // but we do not want to create a new instance every frame...
            if (STYLE_BACKUPSET_STATUS_NAME == null)
            {
               STYLE_BACKUPSET_STATUS_NAME = new GUIStyle(GUI.skin.label);
               STYLE_BACKUPSET_STATUS_NAME.stretchWidth = false;
               STYLE_BACKUPSET_STATUS_NAME.fixedWidth = 234;
               STYLE_BACKUPSET_STATUS_NAME.wordWrap = false;
            }
            if (STYLE_BACKUPSET_CLONE_NAME == null)
            {
               STYLE_BACKUPSET_CLONE_NAME = new GUIStyle(GUI.skin.label);
               STYLE_BACKUPSET_CLONE_NAME.stretchWidth = false;
               STYLE_BACKUPSET_CLONE_NAME.fixedWidth = 290;
               STYLE_BACKUPSET_CLONE_NAME.wordWrap = false;
            }
            if (STYLE_BACKUPSET_STATUS == null)
            {
               STYLE_BACKUPSET_STATUS = new GUIStyle(GUI.skin.label);
               STYLE_BACKUPSET_STATUS.stretchWidth = false;
               STYLE_BACKUPSET_STATUS.margin = new RectOffset(15, 0, 4, 0);
               STYLE_BACKUPSET_STATUS.fixedWidth = 60;
            }
            if (STYLE_RECOVER_BUTTON == null)
            {
               STYLE_RECOVER_BUTTON = new GUIStyle(GUI.skin.button);
               STYLE_RECOVER_BUTTON.stretchWidth = false;
               STYLE_RECOVER_BUTTON.fixedWidth = 65;
            }
            if (STYLE_NAME_TEXTFIELD == null)
            {
               STYLE_NAME_TEXTFIELD = new GUIStyle(GUI.skin.textField);
               STYLE_NAME_TEXTFIELD.stretchWidth = false;
               STYLE_NAME_TEXTFIELD.fixedWidth = 355;
               STYLE_NAME_TEXTFIELD.wordWrap = false;
            }
            if (STYLE_CONFIG_BACKUP_PATH_LABEL == null)
            {
               STYLE_CONFIG_BACKUP_PATH_LABEL = new GUIStyle(GUI.skin.label);
            }
            if (STYLE_CONFIG_BACKUP_PATH_FIELD == null)
            {
               STYLE_CONFIG_BACKUP_PATH_FIELD = new GUIStyle(GUI.skin.textField);
               STYLE_CONFIG_BACKUP_PATH_FIELD.stretchWidth = false;
               STYLE_CONFIG_BACKUP_PATH_FIELD.fixedWidth = 295;
            }
            if (STYLE_CONFIG_TEXTFIELD == null)
            {
               STYLE_CONFIG_TEXTFIELD = new GUIStyle(GUI.skin.textField);
               STYLE_CONFIG_TEXTFIELD.stretchWidth = false;
               STYLE_CONFIG_TEXTFIELD.fixedWidth = 60;
               //STYLE_CONFIG_TEXTFIELD.margin = new RectOffset(0,200,0,0);
            }
            if (STYLE_DELETE_BUTTON == null)
            {
               STYLE_DELETE_BUTTON = new GUIStyle(GUI.skin.button);
               Color orange = new Color(255, 150, 60);
               STYLE_DELETE_BUTTON.normal.textColor = orange;
               STYLE_DELETE_BUTTON.active.textColor = orange;
               STYLE_DELETE_BUTTON.hover.textColor = orange;
               STYLE_DELETE_BUTTON.focused.textColor = orange;
               STYLE_DELETE_BUTTON.onNormal.textColor = orange;
               STYLE_DELETE_BUTTON.onActive.textColor = orange;
               STYLE_DELETE_BUTTON.onHover.textColor = orange;
               STYLE_DELETE_BUTTON.onFocused.textColor = orange;
            }
         }

         private void DisplayCloning()
         {
            InitStyles();
            //
            //
            DrawTitle("Cloning");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Game ");
            GUILayout.Label(selectedGameToClone, STYLE_BACKUPSET_CLONE_NAME);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("From  ");
            cloneFromBackup = ! GUILayout.Toggle(!cloneFromBackup, " game  ");
            GUI.enabled = cloneFromBackupEnabled;
            cloneFromBackup = GUILayout.Toggle(cloneFromBackup, " backup  ");
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Into ");
            bool cloneExists = FileOperations.DirectoryExists(BackupManager.SAVE_ROOT+"/"+cloneGameInto);
            if (cloneExists)
            {
               STYLE_NAME_TEXTFIELD.normal.textColor = Color.red;
            }
            else
            {
               STYLE_NAME_TEXTFIELD.normal.textColor = Color.white;
            }
            cloneGameInto = GUILayout.TextField(cloneGameInto, STYLE_NAME_TEXTFIELD);
            STYLE_NAME_TEXTFIELD.normal.textColor = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.enabled = cloneFromBackupEnabled;
            cloneBackups = GUILayout.Toggle(cloneBackups, "Include backups");
            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = !cloneExists;
            if (GUILayout.Button("Clone", GUI.skin.button))
            {
               display = DISPLAY.HIDDEN;
               String backupRootFolder = SAVE.configuration.backupPath + "/" + name;
               if(cloneFromBackup)
               {
                  SAVE.manager.CloneGameFromBackup(selectedGameToClone, cloneGameInto);
               }
               else
               {
                  SAVE.manager.CloneGame(selectedGameToClone, cloneGameInto);
               }
               if (cloneBackups)
               {
                  SAVE.manager.CloneBackup(selectedGameToClone, cloneGameInto);
               }
            }
            GUI.enabled = true;
            if (GUILayout.Button("Cancel", GUI.skin.button))
            {
               display = DISPLAY.CLONE;
            }
            GUILayout.EndHorizontal();
         }

         private void DisplayClone()
         {
            InitStyles();
            //
            DrawTitle("Games");
            cloneListscrollPosition = GUILayout.BeginScrollView(cloneListscrollPosition, GUI.skin.box, GUILayout.Height(Screen.height - 100));
            foreach (BackupSet set in SAVE.manager)
            {
               GUILayout.BeginHorizontal();
               GUILayout.Label(set.name, STYLE_BACKUPSET_CLONE_NAME);
               GUILayout.FlexibleSpace();
               if (GUILayout.Button("Clone", STYLE_RECOVER_BUTTON))
               {
                  selectedGameToClone = set.name;
                  cloneFromBackupEnabled = set.Latest() != null;
                  cloneFromBackup = cloneFromBackupEnabled;
                  cloneGameInto = set.name + "-clone";
                  display = DISPLAY.CLONING;
               }
               GUI.enabled = true;
               GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
         }


         private void DisplayStatus()
         {
            InitStyles();
            //
            DrawTitle("Games");
            statusListscrollPosition = GUILayout.BeginScrollView(statusListscrollPosition, GUI.skin.box, GUILayout.Height(Screen.height-100));
            foreach (BackupSet set in SAVE.manager)
            {
               GUILayout.BeginHorizontal();
               GUILayout.Label(set.name, STYLE_BACKUPSET_STATUS_NAME);
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
            GUILayout.EndScrollView();
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
            InitStyles();
            //
            Configuration config = SAVE.configuration;
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
                  STYLE_CONFIG_BACKUP_PATH_FIELD.normal.textColor = GUI.skin.textField.normal.textColor;
                  STYLE_CONFIG_BACKUP_PATH_LABEL.normal.textColor = GUI.skin.label.normal.textColor;
               }
               else
               {
                  STYLE_CONFIG_BACKUP_PATH_FIELD.normal.textColor = Color.yellow;
                  STYLE_CONFIG_BACKUP_PATH_LABEL.normal.textColor = Color.yellow;
               }
            }
            else
            {
               STYLE_CONFIG_BACKUP_PATH_FIELD.normal.textColor = Color.red;
               STYLE_CONFIG_BACKUP_PATH_LABEL.normal.textColor = Color.red;
            }
            GUILayout.Label("Backup path: ", STYLE_CONFIG_BACKUP_PATH_LABEL);
            config.backupPath = FileOperations.ExpandBackupPath(GUILayout.TextField(config.backupPath, STYLE_CONFIG_BACKUP_PATH_FIELD));
            GUILayout.EndHorizontal();
            // disabled
            SAVE.configuration.disabled = GUILayout.Toggle(SAVE.configuration.disabled, " Backups temporary disabled");
            // async
            config.asynchronous = GUILayout.Toggle(config.asynchronous, " Asynchronous backup/restore");
            // recurse
            config.recurseBackup = GUILayout.Toggle(config.recurseBackup, " Recurse subfolders");
            // compress
            // not working right now
            config.compressBackups = GUILayout.Toggle(config.compressBackups, " Compress backups");
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
            String sCustomInterval = GUILayout.TextField(config.customBackupInterval.ToString(), STYLE_CONFIG_TEXTFIELD);
            GUILayout.Space(CONFIG_TEXTFIELD_RIGHT_MARGIN);
            config.customBackupInterval = ParseInt(sCustomInterval);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Days to keep backups: ");
            String sDaysToKeepBackups = GUILayout.TextField(config.daysToKeepBackups.ToString(), STYLE_CONFIG_TEXTFIELD);
            GUILayout.Space(CONFIG_TEXTFIELD_RIGHT_MARGIN);
            config.daysToKeepBackups = ParseInt(sDaysToKeepBackups);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Min number of backups: ");
            String sMinNumberOfbackups = GUILayout.TextField(config.minNumberOfBackups.ToString(), STYLE_CONFIG_TEXTFIELD);
            GUILayout.Space(CONFIG_TEXTFIELD_RIGHT_MARGIN);
            config.minNumberOfBackups = ParseInt(sMinNumberOfbackups);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Max number of backups: ");
            String sMaxNumberOfbackups = GUILayout.TextField(config.maxNumberOfBackups.ToString(), STYLE_CONFIG_TEXTFIELD);
            GUILayout.Space(CONFIG_TEXTFIELD_RIGHT_MARGIN);
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
