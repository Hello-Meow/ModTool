# ModTool mod support for Unity

<a href="http://u3d.as/Diq">Asset Store</a>  | <a href="https://www.youtube.com/watch?v=9w_WlBPtclg">Youtube</a> | <a href="https://forum.unity3d.com/threads/modtool-mod-support-for-unity.442185/">Forum Thread</a>

ModTool makes it easy to add mod support to your project. It enables modders to use the Unity Editor to create scenes, prefabs and code and export them as mods for your game.

See the included examples, the [Wiki](https://github.com/Hello-Meow/ModTool/wiki) or the [Documentation](http://hellomeow.net/modtool/documentation) for more info on how to use ModTool.

## Features

- Let modders use the Unity editor to create scenes, prefabs and code for your game
- Scripts and assemblies are fully supported
- Code validation
- Supports Windows, OS X, Linux and Android
- Mod conflict detection
- Automatic Mod discovery
- Asynchronous discovery and loading of mods

## Limitations

- ModTool relies heavily on AssetBundles, which means mods can only be created with the same version of Unity that was used for building the game. The exporter will check if the correct version is used and inform the user if that's not the case
- Unity can't deserialize fields of serializable types that have been loaded at runtime. This means that a Mod can't use its own serializable Types in the inspector. Serializable types that aren't loaded at runtime and are part of the game do work
- Mods have to rely on the game's project settings. This means mods can not define their own new tags, layers and input axes. The created Mod exporter includes the game's project settings
- Supports Unity 5.3.1 and up

## Acknowledgements

- [Mono.Cecil](https://github.com/jbevain/cecil) by Jb Evain
