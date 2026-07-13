using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using WorldMapCharacterSwitch.Objects;

namespace WorldMapCharacterSwitch.Patches
{
    internal class PatchMenus
    {
        //Adventure Map
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuWorldMap), "Start", MethodType.Normal)]
        static void PatchAdventureStart()
        {

        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuWorldMap), "Update", MethodType.Normal)]
        static void PatchAdventureUpdate()
        {
            if (FPStage.menuInput.specialHold)
            {

            }
        }

        //Classic Map
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuClassic), "Start", MethodType.Normal)]
        static void PatchClassicStart()
        {

        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuClassic), "Update", MethodType.Normal)]
        static void PatchClassicUpdate()
        {
            if (FPStage.menuInput.specialHold)
            {

            }
        }

        //Menu Replacements
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuBasic), "Start", MethodType.Normal)]
        static void PatchMenuBasicStart(MenuBasic __instance)
        {
            //Swap to out own Menu implementation
            if (__instance.gameObject.name.Contains("CharacterSelectMenu"))
            {
                __instance.gameObject.AddComponent<MenuCharacterSwitch>();
                GameObject.Destroy(__instance);
            }
        }
    }
}
