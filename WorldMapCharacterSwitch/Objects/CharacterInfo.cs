using System;
using UnityEngine;

namespace WorldMapCharacterSwitch.Objects
{
    internal class CharacterInfo
    {
        internal FPCharacterID id;
        internal String name;
        internal bool modded;
        internal bool enabledInAdventure;
        internal bool brokenInAdventure;
        internal Sprite profilePic;
        internal Sprite[] mapIdle;

        public CharacterInfo(int id, string name, bool enabledInAdventure, bool brokenInAdventure, Sprite profilePic, Sprite[] mapIdle)
        {
            this.id = (FPCharacterID)id;
            this.name = name;
            this.enabledInAdventure = enabledInAdventure;
            this.brokenInAdventure = brokenInAdventure;
            this.profilePic = profilePic;
            this.mapIdle = mapIdle;
        }

        public CharacterInfo() { }
    }
}
