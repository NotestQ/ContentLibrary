# ContentLibrary

## Dependencies
This depends on [Mycelium](https://github.com/RugbugRedfern/Mycelium-Networking-For-Content-Warning)  

## How do I use this
Download the [latest dll](https://github.com/NotestQ/ContentLibrary/releases/latest) from the releases tab or clone and build the repo, then add it as a reference to the project. After adding it as a reference you can add it as a dependency:  
```cs
[BepInDependency(ContentLibrary.MyPluginInfo.PLUGIN_GUID)] // Make sure to specify if it's a soft or a hard dependency! BepInEx sets dependencies to hard by default.
public class YourMod : BaseUnityPlugin { // ...
```  

And for in-depth documentation, check out the [library documentation](https://github.com/NotestQ/ContentLibrary/wiki/Library-Documentation) or one of the demos! Demos available are the [manual replication branch](https://github.com/NotestQ/KeypressEvent-LibraryDemo/tree/master) and the [quick replication branch](https://github.com/NotestQ/KeypressEvent-LibraryDemo/tree/feat_QuickReplication)

## Content class documentation
If you're curious how the Content classes work, check out my [wiki for them](https://github.com/NotestQ/ContentLibrary/wiki/Content-Class-Documentation)!  

### Credits
[Steven4547466](https://github.com/steven4547466) helped to documentate the content class! If it wasn't for them I probably wouldn't have delved deeper into the code at the time that I did, they also did a lot of research themselves.

## Does this add something?
By itself, no — this is a library for mod developers to avoid conflicts when making content events and optionally utilize helper methods the library provides.  
Though this does patch ContentEventIDMapper, so it might conflict with other Content Event mods that don't utilize the library (there is a 1 in 63535 chance one mod without a library would conflict with other content event mods per event added!)

## It doesn't work
If the mod is throwing an error use [the github issues page](https://github.com/NotestQ/ContentLibrary//issues) and copy-paste the error in there, with a description of what is happening and what you expected to happen if applicable. Or just ping me at the Content Warning Modding Discord server! There's two threads about the library currently open.
