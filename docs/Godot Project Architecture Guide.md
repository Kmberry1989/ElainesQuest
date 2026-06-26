# **Godot .NET Project Architecture: Cozy Platformer**

When dealing with 27 unique NPCs, multiple platforming levels, and a central Hub, a modular folder structure is crucial. Godot thrives when everything is broken down into self-contained "Scenes" (.tscn).

Here is the recommended folder hierarchy for your res:// directory:

## **1\. /Assets**

This is where your raw, imported files live before they are turned into Godot scenes.

* /Assets/Models/ (Your raw .glb or .fbx files for Elaine, Family, Wildlife, and Environment)  
* /Assets/Animations/ (The separated Mixamo animation files, categorized by character)  
* /Assets/Audio/ (Music, SFX, UI sounds)  
* /Assets/UI/ (Icons, fonts, dialogue box textures)

## **2\. /Scenes**

This folder holds the actual game levels and UI overlays.

* /Scenes/Hub/ (The cozy Animal Crossing style village)  
  * HubWorld.tscn (The main scene)  
* /Scenes/Levels/ (The Crash Bandicoot/Mario style linear levels)  
  * World\_1\_Forest.tscn  
  * World\_2\_Caves.tscn  
* /Scenes/UI/  
  * MainMenu.tscn  
  * DialogueOverlay.tscn (Used for talking to the wildlife)

## **3\. /Prefabs (or /Entities)**

In Godot, characters and interactive objects are saved as their own scenes so you can easily drop them into any level.

* /Prefabs/Player/  
  * Elaine.tscn (Contains her Mesh, AnimationTree, CollisionShape, and PhantomCamera target)  
* /Prefabs/NPCs/  
  * /Family/ (Matthew.tscn, Ann.tscn, Granny.tscn, etc.)  
  * /Wildlife/ (Sasha.tscn, Toby.tscn, Odessa.tscn, etc.)  
* /Prefabs/Interactables/  
  * Collectibles.tscn  
  * MagicGrappleNode.tscn  
  * BouncePad.tscn

## **4\. /Scripts**

All your C\# files go here, mirroring the structure of your Prefabs and Scenes.

* /Scripts/Player/ (ElaineController.cs, MagicRing.cs)  
* /Scripts/NPCs/ (NPCDialogueTrigger.cs, AnimalRoutine.cs)  
* /Scripts/Core/ (GameManager.cs, SceneFader.cs \- these will likely be your Global Singletons/Autoloads)

## **Getting Started Tip**

Start by creating the /Prefabs/Player/Elaine.tscn scene as a CharacterBody3D. Add your imported Elaine model as a child, set up a capsule collision shape, and attach the C\# script provided in the next file.