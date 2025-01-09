using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LCOpenDoors
{
    [BepInPlugin("Mod.Chris.OpenDoors", "LC Open Doors", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("Mod.Chris.OpenDoors");
        internal static Plugin Instance;
        internal static ManualLogSource mls;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            mls = BepInEx.Logging.Logger.CreateLogSource("Mod.Chris.OpenDoors");
            mls.LogInfo("Loaded Succesfully");
            harmony.PatchAll(typeof(GamePatcher));
        }  
    }

    [HarmonyPatch]
    internal class GamePatcher
    {
        // read chat
        
        [HarmonyPatch(typeof(HUDManager), "SubmitChat_performed")]
        [HarmonyPrefix]
        static void HUDPatch(ref HUDManager __instance)
        {
            try
            {
                if (Utilities.HandleCommand(__instance.chatTextField.text.ToLower()))
                {
                    __instance.chatTextField.text = "";
                }
            }
            catch (Exception e)
            {
                Plugin.mls.LogInfo($"Unable to handle command: {e.Message}");
            }
        }

        // reset list

        [HarmonyPatch(typeof(StartOfRound), "Start")]
        [HarmonyPrefix]
        static void StartOfRoundPatch()
        {
            BigDoorsList.Clear();
            Plugin.mls.LogInfo("BigDoorsList cleared");
        }

        // list doors

        [HarmonyPatch(typeof(TerminalAccessibleObject), "SetCodeTo")]
        [HarmonyPostfix]
        static void BigDoorPatch(ref TerminalAccessibleObject __instance)
        {
            if (__instance.isBigDoor)
            {
                BigDoorsList.Add(__instance);
                
                // debugging:
                Plugin.mls.LogInfo($"objectCode: {__instance.objectCode}");
            }
        }

        internal static List<TerminalAccessibleObject> BigDoorsList = new List<TerminalAccessibleObject>();
    }

    internal class Utilities
    {
        internal static bool HandleCommand(string command)
        {
            // empty Enter
            if (command.IsNullOrWhiteSpace()) { return false; }

            // list doors
            if (command == "doors")
            {
                foreach (TerminalAccessibleObject door in GamePatcher.BigDoorsList)
                {
                    Plugin.mls.LogMessage(door.objectCode);
                }
                return true;
            }
            
            // possible door
            if (command.Length == 2)
            {
                // check doors
                var matchingObject = GamePatcher.BigDoorsList.FirstOrDefault(obj => obj.objectCode == command);
                if (matchingObject != null)
                {
                    matchingObject.SetDoorOpenServerRpc(true);
                    Plugin.mls.LogInfo($"Door {command} opened");
                    return true;
                } 
                Plugin.mls.LogInfo($"Door '{command}' not in level");
            }
            return false;
        }
    }
}