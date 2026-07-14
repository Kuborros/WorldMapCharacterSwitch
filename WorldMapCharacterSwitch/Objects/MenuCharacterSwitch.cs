using BepInEx.Logging;
using FP2Lib.Player;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace WorldMapCharacterSwitch.Objects
{
    internal class MenuCharacterSwitch : MonoBehaviour
    {
        private static readonly ManualLogSource MenuLogSource = WorldMapCharacterSwitch.Logger;

        private FPObjectState state;
        private float genericTimer;
        private float animTimer = 0f;
        private readonly int buttonCount = 3;
        private float[] startX;
        private float[] targetX;
        private SpriteRenderer[] menuButtons;
        private bool switchBlocked = false;

        private int lastCharacterIndex = -1;
        private int selectedCharacterIndex = 0;

        private bool transitionStarted = false;
        private float fadeTimer = 0;

        [HideInInspector]
        public int menuSelection;
        public GameObject menuOptions;
        private CharacterInfo currentCharacter;

        [Header("Prefabs")]
        public MenuCursor cursor;
        public GameObject[] pfButtons;
        public Sprite[] menuSpritesRegular;
        public Sprite[] menuSpritesSelected;

        public static Sprite[] playerSprites = new Sprite[50];
        private static List<CharacterInfo> characters = [];

        internal static void AssembleCharacterList()
        {
            //Reset static vars
            playerSprites = new Sprite[50];
            characters = [];

            if (SceneManager.GetActiveScene().name == "AdventureMenu")
            {
                playerSprites = GameObject.Find("WorldMap").GetComponent<MenuWorldMap>().playerSprite;
            }

            //Add base game characters
            CharacterInfo lilac = new CharacterInfo
            {
                id = FPCharacterID.LILAC,
                name = "Lilac",
                brokenInAdventure = false,
                enabledInAdventure = true,
                modded = false,
                mapIdle = [playerSprites[0], playerSprites[4], playerSprites[8], playerSprites[12], playerSprites[16], playerSprites[20], playerSprites[24], playerSprites[28], playerSprites[32], playerSprites[36], playerSprites[40], playerSprites[44]]

            };

            CharacterInfo carol = new CharacterInfo
            {
                id = FPCharacterID.CAROL,
                name = "Carol",
                brokenInAdventure = false,
                enabledInAdventure = true,
                modded = false,
                mapIdle = [playerSprites[1], playerSprites[5], playerSprites[9], playerSprites[13], playerSprites[17], playerSprites[21], playerSprites[25], playerSprites[29], playerSprites[33], playerSprites[37], playerSprites[41], playerSprites[45]]
            };

            CharacterInfo milla = new CharacterInfo
            {
                id = FPCharacterID.MILLA,
                name = "Milla",
                brokenInAdventure = false,
                enabledInAdventure = true,
                modded = false,
                mapIdle = [playerSprites[2], playerSprites[6], playerSprites[10], playerSprites[14], playerSprites[18], playerSprites[22], playerSprites[26], playerSprites[30], playerSprites[34], playerSprites[38], playerSprites[42], playerSprites[46]]
            };

            CharacterInfo neera = new CharacterInfo
            {
                id = FPCharacterID.NEERA,
                name = "Neera",
                brokenInAdventure = false,
                enabledInAdventure = true,
                modded = false,
                mapIdle = [playerSprites[3], playerSprites[7], playerSprites[11], playerSprites[15], playerSprites[19], playerSprites[23], playerSprites[27], playerSprites[31], playerSprites[35], playerSprites[39], playerSprites[43], playerSprites[47]]
            };

            characters.AddRange([lilac, carol, milla, neera]);

            //Load FP2Lib provided characters
            foreach (PlayableChara character in PlayerHandler.PlayableChars.Values)
            {
                //Skip any broken cases, not loaded characters, and these disabled in all modes.
                if (character == null) continue;
                if (character.prefab == null) continue;
                if (!character.enabledInAventure && !character.enabledInClassic) continue;

                CharacterInfo data = new CharacterInfo
                {
                    id = (FPCharacterID)character.id,
                    name = character.Name,
                    modded = true,
                    profilePic = character.profilePic,
                    enabledInAdventure = character.enabledInAventure,
                };

                //Even with the option on, this *WILL* crash the game, so we make sure to not let the player select them.
                if (character.worldMapIdle == null || character.worldMapWalk == null)
                {
                    MenuLogSource.LogDebug("Found character that seems to have no world map assets: " + character.Name);
                    data.brokenInAdventure = true;
                }
                //If we do not have the option enabled, we exclude characters with no sprites set
                if (!WorldMapCharacterSwitch.allowAllInAdventureMode.Value && !data.brokenInAdventure)
                {
                    if (character.worldMapIdle[0] == null || character.worldMapWalk[0] == null)
                    {
                        MenuLogSource.LogDebug("Found character with null world map sprites: " + character.Name);
                        data.brokenInAdventure = true;
                    }
                }

                if (!data.brokenInAdventure) data.mapIdle = character.worldMapIdle;
                else data.mapIdle = [null];

                //Add them to the list
                characters.Add(data);
                MenuLogSource.LogDebug("Added custom character to rotation: " + data.name);
            }
            //Sort by id
            characters.Sort((x, y) => x.id.CompareTo(y.id));
        }

        private void Start()
        {
            FPStage.currentStage.SetRequestDisablePausing(this);
            menuOptions = gameObject.transform.GetChild(3).gameObject;
            cursor = menuOptions.transform.GetChild(0).GetComponent<MenuCursor>();

            //Setup buttons
            pfButtons = [
                menuOptions.transform.GetChild(1).gameObject,
                menuOptions.transform.GetChild(2).gameObject,
                menuOptions.transform.GetChild(3).gameObject
            ];

            menuSpritesRegular =
            [
                WorldMapCharacterSwitch.menuAssets.LoadAsset<Sprite>("lock"),
                WorldMapCharacterSwitch.menuAssets.LoadAsset<Sprite>("play_off"),
                WorldMapCharacterSwitch.menuAssets.LoadAsset<Sprite>("cancel_off")
            ];

            menuSpritesSelected =
            [
                WorldMapCharacterSwitch.menuAssets.LoadAsset<Sprite>("lock"),
                WorldMapCharacterSwitch.menuAssets.LoadAsset<Sprite>("play_on"),
                WorldMapCharacterSwitch.menuAssets.LoadAsset<Sprite>("cancel_on")
            ];

            startX = new float[pfButtons.Length];
            targetX = new float[pfButtons.Length];
            menuButtons = new SpriteRenderer[buttonCount];
            //Used for animations
            for (int i = 0; i < buttonCount; i++)
            {
                menuButtons[i] = pfButtons[i].GetComponent<SpriteRenderer>();
                startX[i] = pfButtons[i].transform.position.x;
                targetX[i] = pfButtons[i].transform.position.x;
            }
            //Extend MenuDigit's contents with custom characters
            FPHudDigit profiles = gameObject.transform.GetChild(2).GetChild(0).GetChild(0).gameObject.GetComponent<FPHudDigit>();
            foreach (CharacterInfo info in characters)
            {
                if (info.modded)
                {
                    profiles.digitFrames = profiles.digitFrames.AddToArray(info.profilePic);
                }
            }
            profiles.digitFrames = profiles.digitFrames.AddToArray(null);

            //Hide the character world map sprite in Classic
            if (FPSaveManager.gameMode == FPGameMode.CLASSIC)
            {
                gameObject.transform.GetChild(1).transform.position = new Vector3(-1000, -1000, 0);
            }

            //Set current character
            currentCharacter = characters.FirstOrDefault(i => i.id == FPSaveManager.character);
            selectedCharacterIndex = characters.IndexOf(currentCharacter);

            UpdateMenu();
            state = new FPObjectState(State_Main);
        }

        private void Update()
        {
            if (FPStage.state != FPStageState.STATE_PAUSED)
            {
                FPStage.UpdateMenuInput(false);
            }
            if (FPStage.objectsRegistered && state != null)
            {
                state();
            }
            animTimer += 0.15f * FPStage.deltaTime;
        }

        private void UpdateMenu()
        {
            float num = 5f * FPStage.frameScale;
            //Buttons
            for (int i = 0; i < buttonCount; i++)
            {
                float num2 = (pfButtons[i].transform.position.x * (num - 1f) + targetX[i]) / num;
                float y = pfButtons[i].transform.position.y;
                float z = pfButtons[i].transform.position.z;
                //Move cursor to selection
                if (i == menuSelection)
                {
                    if (i > 0)
                    {
                        cursor.transform.position = new Vector3(num2 - 32f, y, z);
                    }
                    else
                    {
                        //Hide it
                        cursor.transform.position = new Vector3(-1000, -1000, z);
                    }
                }
            }
            transform.position = new Vector3(transform.position.x, transform.position.y * (num - 1f) / num, transform.position.z);

            //Update button visuals
            switch (menuSelection)
            {
                case 0:
                    pfButtons[0].transform.GetChild(1).gameObject.SetActive(true);
                    pfButtons[0].transform.GetChild(2).gameObject.SetActive(true);
                    pfButtons[1].GetComponent<SpriteRenderer>().sprite = menuSpritesRegular[1];
                    pfButtons[2].GetComponent<SpriteRenderer>().sprite = menuSpritesRegular[2];
                    break;
                case 1:
                    pfButtons[0].transform.GetChild(1).gameObject.SetActive(false);
                    pfButtons[0].transform.GetChild(2).gameObject.SetActive(false);
                    pfButtons[1].GetComponent<SpriteRenderer>().sprite = menuSpritesSelected[1];
                    pfButtons[2].GetComponent<SpriteRenderer>().sprite = menuSpritesRegular[2];
                    break;
                case 2:
                    pfButtons[0].transform.GetChild(1).gameObject.SetActive(false);
                    pfButtons[0].transform.GetChild(2).gameObject.SetActive(false);
                    pfButtons[1].GetComponent<SpriteRenderer>().sprite = menuSpritesRegular[1];
                    pfButtons[2].GetComponent<SpriteRenderer>().sprite = menuSpritesSelected[2];
                    break;
            }

            if (currentCharacter == null || switchBlocked) pfButtons[1].GetComponent<SpriteRenderer>().sprite = menuSpritesRegular[0];

            //Character info
            //Update only when needed
            if (lastCharacterIndex != selectedCharacterIndex)
            {
                GameObject characterProfileBox = gameObject.transform.GetChild(2).GetChild(0).GetChild(0).gameObject;
                GameObject characterSelectBox = menuOptions.transform.GetChild(1).gameObject;
                GameObject characterDescriptionBox = gameObject.transform.GetChild(4).GetChild(0).gameObject;

                string adventureStatus, classicStatus = "<c=green>available</c>", extraAdventureInfo;

                //Drop out if things broke.
                if (characterProfileBox == null) return;

                currentCharacter = characters[selectedCharacterIndex];

                //Render current character
                //Make sure we did not get a cursed broken stage
                if (currentCharacter.id >= 0)
                {
                    //Name
                    characterSelectBox.transform.GetChild(0).GetComponent<TextMesh>().text = currentCharacter.name;
                    //Picture
                    characterProfileBox.GetComponent<FPHudDigit>().SetDigitValue(selectedCharacterIndex);

                    //Character broken in adventure (*will* crash if switched to)
                    if (currentCharacter.brokenInAdventure)
                    {
                        adventureStatus = "<c=red>broken</c>";
                        extraAdventureInfo = " (Missing critical sprites)";
                        switchBlocked = (FPSaveManager.gameMode == FPGameMode.ADVENTURE);
                    }
                    //Character disabled in adventure, and override not enabled
                    else if (!currentCharacter.enabledInAdventure && !WorldMapCharacterSwitch.ignoreDisabledInAdventure.Value)
                    {
                        adventureStatus = "<c=red>unavailable</c>";
                        extraAdventureInfo = " (Disabled by mod author)";
                        switchBlocked = (FPSaveManager.gameMode == FPGameMode.ADVENTURE);
                    }
                    //Character disabled in adventure, but override enabled
                    else if (!currentCharacter.enabledInAdventure && WorldMapCharacterSwitch.ignoreDisabledInAdventure.Value)
                    {
                        adventureStatus = "<c=yellow>available</c>";
                        extraAdventureInfo = " (Forced by a config option)";
                        characterDescriptionBox.GetComponent<SuperTextMesh>().text = "";
                        switchBlocked = false;
                    }
                    //Character fully functional
                    else
                    {
                        adventureStatus = "<c=green>available</c>";
                        extraAdventureInfo = "";
                        switchBlocked = false;
                    }


                    //Assemble final description
                    characterDescriptionBox.GetComponent<SuperTextMesh>().text = "This character is " + classicStatus + " in <c=orange>Classic Mode</c>.<br>" +
                        "They are " + adventureStatus + " in <c=orange>Adventure Mode</c>." + extraAdventureInfo;

                    //If we are already that character, prevent switching
                    if (currentCharacter.id == FPSaveManager.character)
                    {
                        switchBlocked = true;
                    }

                }

                MenuLogSource.LogDebug("Set character to ID: " + currentCharacter.id);
                lastCharacterIndex = selectedCharacterIndex;
            }
        }

        private void State_Main()
        {
            //Character animation
            if (FPSaveManager.gameMode == FPGameMode.ADVENTURE)
            {
                GameObject characterMapSpriteBox = gameObject.transform.GetChild(1).GetChild(0).gameObject;
                characterMapSpriteBox.GetComponent<SpriteRenderer>().sprite = currentCharacter.mapIdle[Mathf.Min((int)((animTimer) % currentCharacter.mapIdle.Length), currentCharacter.mapIdle.Length - 1)];
            }

            //Up-Down controls
            if (FPStage.menuInput.up)
            {
                menuSelection--;
                if (menuSelection < 0)
                {
                    menuSelection = 0;
                }
                else FPAudio.PlayMenuSfx(1);
            }
            else if (FPStage.menuInput.down)
            {
                menuSelection++;
                if (menuSelection > 2)
                {
                    menuSelection = 2;
                }
                else FPAudio.PlayMenuSfx(1);
            }
            //Straight to 'Exit'
            else if (FPStage.menuInput.cancel)
            {
                menuSelection = 2;
                genericTimer = 10f;
                FPAudio.PlayMenuSfx(1);
            }

            //Left-Right controls
            if (FPStage.menuInput.right)
            {
                //Level Selector
                if (menuSelection == 0)
                {
                    if (selectedCharacterIndex < characters.Count - 1)
                    {
                        selectedCharacterIndex++;
                    }
                    else selectedCharacterIndex = 0;
                    FPAudio.PlayMenuSfx(1);
                }
                //Bottom buttons
                else if (menuSelection == 1)
                {
                    menuSelection++;
                    FPAudio.PlayMenuSfx(1);
                }
            }
            if (FPStage.menuInput.left)
            {
                //Level Selector
                if (menuSelection == 0)
                {
                    if (selectedCharacterIndex > 0)
                    {
                        selectedCharacterIndex--;
                    }
                    else selectedCharacterIndex = characters.Count - 1;
                    FPAudio.PlayMenuSfx(1);
                }
                //Bottom buttons
                else if (menuSelection <= 2)
                {
                    menuSelection--;
                    FPAudio.PlayMenuSfx(1);
                }
            }
            if (genericTimer > 0f)
            {
                genericTimer -= FPStage.deltaTime;
            }
            //Switch Character
            else if (FPStage.menuInput.confirm && menuSelection == 1)
            {
                if (switchBlocked)
                {
                    cursor.optionSelected = true;
                    FPAudio.PlayMenuSfx(21);
                }
                else
                {

                    genericTimer = 0f;

                    if (FPSaveManager.gameMode == FPGameMode.ADVENTURE)
                        state = new FPObjectState(State_Transition_Adventure);
                    else
                        state = new FPObjectState(State_Transition_Classic);

                    cursor.optionSelected = true;
                    FPAudio.PlayMenuSfx(2);
                }
            }
            //Exit
            else if (menuSelection == buttonCount - 1 && (FPStage.menuInput.confirm || FPStage.menuInput.cancel))
            {
                Object.Destroy(gameObject);
                FPAudio.PlayMenuSfx(2);
            }
            UpdateMenu();
        }

        private void State_Transition_Adventure()
        {
            if (genericTimer < 30f)
            {
                genericTimer += FPStage.deltaTime;
            }
            else
            {
                if (!transitionStarted)
                {
                    FPScreenTransition component = GameObject.Find("Screen Transition").GetComponent<FPScreenTransition>();
                    component.transitionType = FPTransitionTypes.LOCAL_WIPE;
                    component.transitionSpeed = 48f;
                    component.SetTransitionColor(0f, 0f, 0f);
                    component.BeginTransition();
                    FPAudio.PlayMenuSfx(3);
                    transitionStarted = true;
                    fadeTimer = 0f;
                }
                else
                {
                    if (fadeTimer < 24f)
                    {
                        fadeTimer += FPStage.deltaTime;
                    }
                    else
                    {
                        //Set the character proper
                        if (currentCharacter.id > FPCharacterID.NEERA)
                        {
                            PlayableChara newCharacter = PlayerHandler.GetPlayableCharaByRuntimeId((int)currentCharacter.id);
                            if (newCharacter != null)
                            {
                                PlayerHandler.SwitchToCharacterByUID(newCharacter.uid);
                            }
                        }
                        else
                        {
                            FPSaveManager.character = currentCharacter.id;
                            PlayerHandler.currentCharacter = null;
                        }
                        FPStage.LoadScene("AdventureMenu");
                    }
                }
            }
        }

        private void State_Transition_Classic()
        {
            if (genericTimer < 30f)
            {
                genericTimer += FPStage.deltaTime;
            }
            else
            {
                if (!transitionStarted)
                {
                    FPScreenTransition component = GameObject.Find("Screen Transition").GetComponent<FPScreenTransition>();
                    component.transitionType = FPTransitionTypes.LOCAL_WIPE;
                    component.transitionSpeed = 48f;
                    component.SetTransitionColor(0f, 0f, 0f);
                    component.BeginTransition();
                    FPAudio.PlayMenuSfx(3);
                    transitionStarted = true;
                    fadeTimer = 0f;
                }
                else
                {
                    if (fadeTimer < 24f)
                    {
                        fadeTimer += FPStage.deltaTime;
                    }
                    else
                    {
                        //Set the character proper
                        if (currentCharacter.id > FPCharacterID.NEERA)
                        {
                            PlayableChara newCharacter = PlayerHandler.GetPlayableCharaByRuntimeId((int)currentCharacter.id);
                            if (newCharacter != null)
                            {
                                PlayerHandler.SwitchToCharacterByUID(newCharacter.uid);
                            }
                        }
                        else
                        {
                            FPSaveManager.character = currentCharacter.id;
                            PlayerHandler.currentCharacter = null;
                        }
                        FPStage.LoadScene("ClassicMenu");
                    }
                }
            }
        }
    }
}

