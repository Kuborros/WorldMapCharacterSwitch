using BepInEx.Logging;
using FP2Lib.Player;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WorldMapCharacterSwitch.Objects
{
    internal class MenuCharacterSwitch : MonoBehaviour
    {
        private static readonly ManualLogSource MenuLogSource = WorldMapCharacterSwitch.Logger;

        private FPObjectState state;
        private float genericTimer;
        private readonly int buttonCount = 3;
        private float[] startX;
        private float[] targetX;
        private SpriteRenderer[] menuButtons;

        private int lastCharacterIndex = -1;
        private int selectedCharacterIndex = 0;

        private FPCharacterID targetCharacterID = FPCharacterID.LILAC;

        [HideInInspector]
        public int menuSelection;
        public GameObject menuOptions;
        public float xOffsetRegular;
        public float xOffsetSelected;
        public GameObject targetMenu;
        private List<CharacterInfo> characters = [];
        private CharacterInfo currentCharacter;

        [Header("Prefabs")]
        public MenuCursor cursor;
        public GameObject[] pfButtons;
        public Sprite[] menuSpritesRegular;
        public Sprite[] menuSpritesSelected;
        public GameObject parentMenu;
        public Sprite[] playerSprites = new Sprite[50];

        private void Start()
        {
            FPStage.currentStage.SetRequestDisablePausing(this);
            menuOptions = gameObject.transform.GetChild(3).gameObject;

            if (SceneManager.GetActiveScene().name == "AdventureMenu")
            {
                playerSprites = GameObject.Find("WorldMap").GetComponent<MenuWorldMap>().playerSprite;
            }

            //Add base game characters
            CharacterInfo lilac = new CharacterInfo
            {
                id = 0,
                name = "Lilac",
                brokenInAdventure = false,
                enabledInAdventure = true,
                modded = false,
                mapIdle = [playerSprites[0], playerSprites[4], playerSprites[8], playerSprites[12], playerSprites[16], playerSprites[20], playerSprites[24], playerSprites[28], playerSprites[32], playerSprites[36], playerSprites[40], playerSprites[44]]

            };

            CharacterInfo carol = new CharacterInfo
            {
                id = 1,
                name = "Carol",
                brokenInAdventure = false,
                enabledInAdventure = true,
                modded = false,
                mapIdle = [playerSprites[1], playerSprites[5], playerSprites[9], playerSprites[13], playerSprites[17], playerSprites[21], playerSprites[25], playerSprites[29], playerSprites[33], playerSprites[37], playerSprites[41], playerSprites[45]]
            };

            CharacterInfo milla = new CharacterInfo
            {
                id = 3,
                name = "Milla",
                brokenInAdventure = false,
                enabledInAdventure = true,
                modded = false,
                mapIdle = [playerSprites[2], playerSprites[6], playerSprites[10], playerSprites[14], playerSprites[18], playerSprites[22], playerSprites[26], playerSprites[30], playerSprites[34], playerSprites[38], playerSprites[42], playerSprites[46]]
            };

            CharacterInfo neera = new CharacterInfo
            {
                id = 4,
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
                    id = character.id,
                    name = character.Name,
                    modded = true,
                    profilePic = character.profilePic,
                    enabledInAdventure = character.enabledInAventure,
                };

                if (FPSaveManager.gameMode == FPGameMode.ADVENTURE)
                {
                    //Even with the option on, this *WILL* crash the game, so we make sure to not let the player select them.
                    if (character.worldMapIdle == null || character.worldMapWalk == null) data.brokenInAdventure = true;
                    //If we do not have the option enabled, we exclude characters with no sprites set
                    if (!WorldMapCharacterSwitch.allowAllInAdventureMode.Value)
                    {
                        if (character.worldMapIdle[0] == null || character.worldMapWalk[0] == null)
                        {
                            MenuLogSource.LogDebug("Skipping character with no world map sprites: " + character.Name);
                            data.brokenInAdventure = true;
                        }
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

            cursor = menuOptions.transform.GetChild(0).GetComponent<MenuCursor>();

            //Setup buttons
            pfButtons = new GameObject[] {
                menuOptions.transform.GetChild(1).gameObject,
                menuOptions.transform.GetChild(2).gameObject
            };

            menuSpritesRegular = new Sprite[]
            {
                WorldMapCharacterSwitch.menuAssets.LoadAsset<Sprite>("lock"),
                WorldMapCharacterSwitch.menuAssets.LoadAsset<Sprite>("play_off"),
                WorldMapCharacterSwitch.menuAssets.LoadAsset<Sprite>("stop_off")
            };

            menuSpritesSelected = new Sprite[]
            {
                WorldMapCharacterSwitch.menuAssets.LoadAsset<Sprite>("lock"),
                WorldMapCharacterSwitch.menuAssets.LoadAsset<Sprite>("play_on"),
                WorldMapCharacterSwitch.menuAssets.LoadAsset<Sprite>("stop_on")
            };

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
            //Set current character
            selectedCharacterIndex = (int)FPSaveManager.character;

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
                        cursor.transform.position = new Vector3(200, y, z);
                    }
                }
            }
            //Update button visuals
            switch (menuSelection)
            {
                case 0:
                    pfButtons[1].GetComponent<SpriteRenderer>().sprite = menuSpritesRegular[1];
                    pfButtons[2].GetComponent<SpriteRenderer>().sprite = menuSpritesRegular[2];
                    break;
                case 1:
                    pfButtons[1].GetComponent<SpriteRenderer>().sprite = menuSpritesSelected[1];
                    pfButtons[2].GetComponent<SpriteRenderer>().sprite = menuSpritesRegular[2];
                    break;
                case 2:
                    pfButtons[1].GetComponent<SpriteRenderer>().sprite = menuSpritesRegular[1];
                    pfButtons[2].GetComponent<SpriteRenderer>().sprite = menuSpritesSelected[2];
                    break;
            }

            if (currentCharacter == null) pfButtons[1].GetComponent<SpriteRenderer>().sprite = menuSpritesRegular[0];



            //Character info
            //Update only when needed
            if (lastCharacterIndex != selectedCharacterIndex)
            {
                GameObject characterMapSpriteBox = gameObject.transform.GetChild(1).gameObject;
                GameObject characterProfileBox = gameObject.transform.GetChild(2).gameObject;
                GameObject characterSelectBox = gameObject.transform.GetChild(4).gameObject;
                GameObject characterDescriptionBox = gameObject.transform.GetChild(5).gameObject;

                //Drop out if things broke.
                if (characterProfileBox == null) return;

                currentCharacter = characters[selectedCharacterIndex];

                //Render current character
                FPCharacterID characterID = (FPCharacterID)currentCharacter.id;

                //Make sure we did not get a cursed broken stage
                if (currentCharacter.id <= 0)
                {
                    
                }

                MenuLogSource.LogDebug("Set character to ID: " + characterID);
                targetCharacterID = characterID;
                lastCharacterIndex = selectedCharacterIndex;
            }
        }

        private void State_Main()
        {
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
            //Handle Play and Exit buttons.
            else if (FPStage.menuInput.confirm && menuSelection > 0)
            {
                genericTimer = 0f;
                state = new FPObjectState(State_Transition);
                cursor.optionSelected = true;
                FPAudio.PlayMenuSfx(2);
            }
            UpdateMenu();
        }

        private void State_Transition()
        {
            if (genericTimer < 30f)
            {
                genericTimer += FPStage.deltaTime;
            }
            else
            {

            }
        }

        private void State_WaitForMenu()
        {
            float num = 5f * FPStage.frameScale;
            transform.localPosition = new Vector3(transform.localPosition.x, (transform.localPosition.y * (num - 1f) + 360f) / num, transform.localPosition.z);
            if (genericTimer < 20f)
            {
                genericTimer += FPStage.deltaTime;
            }
            else if (targetMenu.transform.localPosition.y < -100f)
            {
                genericTimer = 0f;
                state = State_Main;
            }
        }
    }
}

