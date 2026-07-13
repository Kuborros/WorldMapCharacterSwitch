using System;
using UnityEngine;

namespace WorldMapCharacterSwitch.Objects
{
    internal class CharacterInfo
    {
        internal int id;
        internal String name;
        internal bool modded;
        internal bool enabledInAdventure;
        internal bool brokenInAdventure;
        internal Sprite profilePic;
        internal Sprite[] mapIdle;

        public CharacterInfo(int id, string name, bool enabledInAdventure, bool brokenInAdventure, Sprite profilePic, Sprite[] mapIdle)
        {
            this.id = id;
            this.name = name;
            this.enabledInAdventure = enabledInAdventure;
            this.brokenInAdventure = brokenInAdventure;
            this.profilePic = profilePic;
            this.mapIdle = mapIdle;
        }

        public CharacterInfo() { }
    }
}
