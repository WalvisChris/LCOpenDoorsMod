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
    [BepInPlugin("Mod.Chris.OpenDoors", "LC Open Doors", "1.0.1")]
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
                bigDoors = UnityEngine.Object.FindObjectsOfType<TerminalAccessibleObject>().Where(obj => obj.isBigDoor).ToArray();

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
        internal static TerminalAccessibleObject[] bigDoors;
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
                foreach (TerminalAccessibleObject door in GamePatcher.bigDoors)
                {
                    Plugin.mls.LogMessage(door.objectCode);
                }
                return true;
            }
            
            // possible door
            if (command.Length == 2)
            {
                // check doors
                var matchingObject = GamePatcher.bigDoors.FirstOrDefault(obj => obj.objectCode == command);
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