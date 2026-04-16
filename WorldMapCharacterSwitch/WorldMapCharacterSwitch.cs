using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using FP2Lib.Player;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;

namespace WorldMapCharacterSwitch
{
    [BepInPlugin("com.kuborro.plugin.fp2.worldmapcharaswap", "WorldMapCharacterSwap", "2.0.0")]
    [BepInDependency("000.kuborro.libraries.fp2.fp2lib")]
    public class WorldMapCharacterSwitch : BaseUnityPlugin
    {
        internal static ConfigEntry<bool> allowAllInAdventureMode;
        internal static ConfigEntry<bool> ignoreDisabledInAdventure;
        private void Awake()
        {
            allowAllInAdventureMode = Config.Bind("General", "Allow incompatible characters in Adventure", false, "Setting this option will include even characters which might not work due to lacking code or assets");
            ignoreDisabledInAdventure = Config.Bind("General", "Ignore Disabled In Adventure flag", false, "Setting this option will include even characters whose authors disabled them in Adventure Mode");
            Harmony.CreateAndPatchAll(typeof(PatchWorldMap));
        }
    }

    
    internal class PatchWorldMap
    {
        private static bool charChanged;
        private static float fadeTimer = 0f;
        private static int index = 0;
        private static FPCharacterID character;
        private static List<FPCharacterID> availableCharacters;

        internal static readonly FPCharacterID[] availableBaseCharacters = [FPCharacterID.LILAC, FPCharacterID.CAROL, FPCharacterID.MILLA, FPCharacterID.NEERA];
        internal static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("WorldMapCharacterSwap");

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(MenuWorldMap), "Start", MethodType.Normal)]
        [HarmonyPatch(typeof(MenuClassic), "Start", MethodType.Normal)]
        static void PatchAdventureStart()
        {
            List<FPCharacterID> moddedCharacters = [];
            foreach (PlayableChara character in PlayerHandler.PlayableChars.Values)
            {
                //Skip any broken cases and not loaded characters
                if (character == null) continue;
                if (character.prefab == null) continue;

                if (FPSaveManager.gameMode == FPGameMode.ADVENTURE)
                {
                    //Even with the option on, this *WILL* crash the game, so we bail regardless
                    if (character.worldMapIdle == null || character.worldMapWalk == null) continue;
                    //If we do not have the option enabled, we exclude characters with no sprites set
                    if (!WorldMapCharacterSwitch.allowAllInAdventureMode.Value)
                    {
                        if (character.worldMapIdle[0] == null || character.worldMapWalk[0] == null) 
                        {
                            Logger.LogDebug("Skipping character with no world map sprites: " + character.Name);
                            continue;
                        }
                    }
                    if (!WorldMapCharacterSwitch.ignoreDisabledInAdventure.Value && !character.enabledInAventure) continue;
                }

                //Add them to the list
                moddedCharacters.Add((FPCharacterID)character.id);
                Logger.LogDebug("Added custom character to rotation: " + character.Name);
            }

            //Rebuild the list
            moddedCharacters.Sort();
            availableCharacters = [.. availableBaseCharacters, .. moddedCharacters];
            //Set the right index
            index = availableCharacters.IndexOf(FPSaveManager.character);

            //Set the right character
            character = FPSaveManager.character;
        }

        //Adventure Map
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuWorldMap),"Update",MethodType.Normal)]
        static void PatchAdventureUpdate(ref float ___movementTimer, int ___currentMap, ref int ___currentLocation, FPMap[] ___maps)
        {

            if (FPStage.menuInput.specialHold && !charChanged)
            {
                //Play transition
                FPScreenTransition component = GameObject.Find("Screen Transition").GetComponent<FPScreenTransition>();
                component.transitionType = FPTransitionTypes.LOCAL_WIPE;
                component.transitionSpeed = 48f;
                component.SetTransitionColor(0f, 0f, 0f);
                component.BeginTransition();
                FPAudio.PlayMenuSfx(3);

                index = availableCharacters.IndexOf(FPSaveManager.character);
                if (++index < availableCharacters.Count)
                {
                    character = availableCharacters[index];
                }
                else
                {
                    //Reset to start
                    index = 0;
                    character = FPCharacterID.LILAC;
                }
                Logger.LogDebug("Queued transition to character ID: " + character);
                charChanged = true;
            }

            if (charChanged) 
            {
                if (fadeTimer < 24f)
                {
                    fadeTimer += FPStage.deltaTime;
                }
                else
                {
                    //Set the character proper
                    if (character > FPCharacterID.NEERA)
                    {
                        PlayableChara newCharacter = PlayerHandler.GetPlayableCharaByRuntimeId((int)character);
                        if (newCharacter != null)
                        {
                            PlayerHandler.SwitchToCharacterByUID(newCharacter.uid);
                        }
                    }
                    else
                    {
                        FPSaveManager.character = character;
                        PlayerHandler.currentCharacter = null;
                    }
                    //Map reload logic
                    ___movementTimer = 0f;
                    FPSaveManager.lastMap = ___currentMap;
                    if (___maps[___currentMap].locations[___currentLocation].type != FPMapLocationType.NONE) 
                    {
                        //Only set the map tile if we are in a 'safe' location that allows input. This prevents horrible softlocks.
                        FPSaveManager.lastMapLocation = ___currentLocation;
                    }
                    else
                    {
                        Logger.LogDebug("Player on unsafe tile! Resetting position.");
                    }
                    charChanged = false;
                    FPStage.LoadScene("AdventureMenu");
                }
            }
            else 
                fadeTimer = 0f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuClassic), "Update", MethodType.Normal)]
        static void PatchClassicUpdate(ref float ___movementTimer, int ___currentTile)
        {
            FPCharacterID character;
            if (FPStage.menuInput.specialHold && !charChanged)
            {
                index = availableCharacters.IndexOf(FPSaveManager.character);

                if (++index < availableCharacters.Count)
                {
                    character = availableCharacters[index];
                }
                else
                {
                    //Reset to start
                    index = 0;
                    character = FPCharacterID.LILAC;
                }

                charChanged = true;
                //Set the new character
                if (character > FPCharacterID.NEERA)
                {
                    PlayableChara newCharacter = PlayerHandler.GetPlayableCharaByRuntimeId((int)character);
                    if (newCharacter != null)
                    {
                        PlayerHandler.SwitchToCharacterByUID(newCharacter.uid);
                    }
                }
                else
                {
                    FPSaveManager.character = character;
                    PlayerHandler.currentCharacter = null;
                }
                Logger.LogDebug("Queued transition to character ID: " + character);
                //Play transition
                FPScreenTransition component = GameObject.Find("Screen Transition").GetComponent<FPScreenTransition>();
                component.transitionType = FPTransitionTypes.LOCAL_WIPE;
                component.transitionSpeed = 48f;
                component.SetTransitionColor(0f, 0f, 0f);
                component.BeginTransition();
                FPAudio.PlayMenuSfx(3);
            }

            if (charChanged)
            {
                if (fadeTimer < 24f)
                {
                    fadeTimer += FPStage.deltaTime;
                }
                else
                {
                    ___movementTimer = 0f;
                    FPSaveManager.lastMapLocation = ___currentTile;
                    charChanged = false;
                    FPStage.LoadScene("ClassicMenu");
                }
            }
            else
                fadeTimer = 0f;
        }
    }

}
