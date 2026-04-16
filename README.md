# World Map Character Switcher

This mod lets you switch characters on the world map - both Adventure and Classic!

Change can be performed while standing on any tile, and pressing button bound to "Special". The map should then reload with the next character in order.

Its achieved by changing your character id, thus effectively convincing the game you always were playing this character.

Custom characters are now supported, in both Classic and Adventure modes!
---
This mod contains the following configuration options, available trough Configuration Manager or by opening the config file in the text editor:

```ini
[General]
## Setting this option will include even characters which might not work due to lacking code or assets
Allow incompatible characters in Adventure = true

## Setting this option will include even characters whose authors disabled them in Adventure Mode
Ignore Disabled In Adventure flag = true
```
