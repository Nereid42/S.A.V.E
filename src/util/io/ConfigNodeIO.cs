using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace S.A.V.E.src.util.io
{
    class ConfigNodeIO
    {

        static string PLUGINDATA = "GameData/Nereid/S.A.V.E/PluginData/Settings.cfg";
        const string DATANODE = "S.A.V.E";
        const string EXCLUDE = "Exclude";

        internal static bool fixedWindowUpperRight = true;
        internal static bool fixedWindowUpperLeft = false;
        internal static bool fixedWindowFloating = false;

        internal static List<string> excludes = null;
        internal static string SafeLoad(string value, bool oldvalue)
        {
            if (value == null)
                return oldvalue.ToString();
            return value;
        }

        static public void LoadData()
        {
            if (File.Exists(KSPUtil.ApplicationRootPath + PLUGINDATA))
            {
                ConfigNode data = ConfigNode.Load(KSPUtil.ApplicationRootPath + PLUGINDATA);
                if (data != null)
                {
                    ConfigNode dataNode = data.GetNode(DATANODE);
                    if (dataNode != null)
                    {
                        string fixedWindowPos = dataNode.GetValue("WindowPos");
                        if (fixedWindowPos != null)
                        {
                            // Set this false here because it is initialized to true
                            fixedWindowUpperRight = false;

                            fixedWindowUpperRight = (fixedWindowPos == "upperRight");
                            fixedWindowUpperLeft = (fixedWindowPos == "upperLeft");
                            fixedWindowFloating = (fixedWindowPos == "floating");

                            // If none are specified, then default to the original location

                            if (!fixedWindowUpperRight && !fixedWindowUpperLeft && !fixedWindowFloating)
                                fixedWindowUpperRight = true;
                        }

                        excludes = dataNode.GetValuesList(EXCLUDE);
                        foreach (var s in excludes)
                            Nereid.SAVE.Log.Info("Exclude: " + s);
                    }
                }
            }
            else
            {
                excludes = new List<string>();
            }
        }

#if false
        static public void SaveData(List<string> excludes)
        {
            ConfigNode dataFile = new ConfigNode(DATANODE);
            ConfigNode dataNode = new ConfigNode(DATANODE);
            dataFile.AddNode(dataNode);

            foreach (var entry in excludes)
            {
                ConfigNode n = new ConfigNode();

                dataNode.AddValue(EXCLUDE, entry);
            }
            dataFile.Save(PLUGINDATA);
        }
#endif

    }
}
