using BepInEx;
using BepInEx.Configuration;
using ConstantClassNamespace;
using HarmonyLib;
using ReflectionUtility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UncensoredBox_BepInEx
{
    [BepInPlugin(ConstantClass.pluginGuid, ConstantClass.pluginName, ConstantClass.pluginVersion)]
    public class UncensoredBoxClass : BaseUnityPlugin
    {
        public static Harmony harmony = new Harmony(ConstantClass.pluginName);
        private bool _initialized = false;
        public static List<string> Names = new List<string>();
        public static Dictionary<string, string> NamesDictionary = new Dictionary<string, string>();
        public static Dictionary<string, List<string>> СловарьИмён = new Dictionary<string, List<string>>();


        public void Awake()
        {
            harmony.Patch(AccessTools.Method(typeof(NameGenerator), "checkBlackList"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), "checkBlackList_Prefix")));
        }

        public void Update()
        {

            if (global::Config.gameLoaded)
            {
                foreach (var unit in World.world.units)
                {
                    //BaseStats stats = (BaseStats)Reflection.GetField(unit.GetType(), unit, "stats");
                    ActorData data = (ActorData)Reflection.GetField(unit.GetType(), unit, "data");

                    unit.getName();

                    if (checkBlackList(data.name) && !data.favorite && !Names.Contains(data.name))
                    {
                        data.favorite = true;

                        if (!СловарьИмён.ContainsKey(unit.asset.nameTemplate))
                        {
                            СловарьИмён[unit.asset.nameTemplate] = new List<string>();
                        }
                        if (!СловарьИмён[unit.asset.nameTemplate].Contains(data.name))
                        {
                            СловарьИмён[unit.asset.nameTemplate].Add(data.name);
                            Names.Add(data.name);
                        }
                    }
                    if (!data.favorite)
                    {
                        unit.killHimself();
                    }
                }

                if (Input.GetKeyDown(KeyCode.F1))
                {
                    Logger.LogMessage($"Уникальных имён - {Names.Count}");
                    foreach (var kvp in СловарьИмён)
                    {
                        string values = string.Join(", ", kvp.Value);
                        Logger.LogMessage($"{kvp.Key}: {values}, всего - {kvp.Value.Count}");
                    }
                }
            }
        }

        public static bool checkBlackList(string pName)
        {
            string text = pName.ToLower();
            char[] array = (from c in text.Distinct<char>()
                            where char.IsLetter(c)
                            select c).ToArray<char>();
            string text2 = (string)Reflection.CallStaticMethod(typeof(NameGenerator), "cleanString", text);
            bool flag = !(text2 == text);
            Dictionary<char, string[]> dictionary = (Dictionary<char, string[]>)Reflection.GetField(typeof(NameGenerator), null, "profanity");
            foreach (char key in array)
            {
                if (dictionary.ContainsKey(key))
                {
                    for (int j = 0; j < dictionary[key].Length; j++)
                    {
                        if (text.Contains(dictionary[key][j]))
                        {
                            return true;
                        }
                        if (flag && text2.Contains(dictionary[key][j]))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    public class Patches
    {
        public static bool checkBlackList_Prefix(string pName, ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    public class Настройка : BaseUnityPlugin
    {
        public ConfigEntry<bool> ОтмечатьЮнитовКакФаворитов;
        public ConfigEntry<bool> ВестиПодсчётСтатистики;
        public ConfigEntry<bool> УничтожатьВсехНеФаворитов; 

        private void Awake()
        {
            ОтмечатьЮнитовКакФаворитов = Config.Bind("General",      // The section under which the option is shown
                                         "Tag Units as Favorites",  // The key of the configuration option in the configuration file
                                         false, // The default value
                                         "All units with blacklisted names will be automatically made favorites"); // Description of the option to show in the config file

            УничтожатьВсехНеФаворитов = Config.Bind("General",
                                                "Log Statistics",
                                                false,
                                                "Blacklisted name statistics will be recorded and shown in the console when pressing the F1 key");
            // Test code
            Logger.LogInfo("Hello, world!");
        }
    }
}
