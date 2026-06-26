# **Elaine's Quest: Complete Godot Setup Guide**

This document will take you from a totally blank Godot project to a running, playable prototype featuring Elaine, the Hub world, UI, dialogue, collectibles, and a fully functional game manager. Follow these steps sequentially.

## **Phase 1: Project Creation & Fundamentals**

1. **Download Godot:** Ensure you have the **Godot 4 .NET version** downloaded from the official website. You will also need an IDE like Visual Studio, Visual Studio Code, or JetBrains Rider installed to edit the C\# scripts.  
2. **Create Project:** Open Godot, click "New Project", name it ElainesQuest, and select "Forward+" as the renderer.  
3. **Build Folder Structure:** Run the provided build\_project.py script on your desktop to generate the folder structure and initial files, and manually add the newly generated C\# scripts to their appropriate folders (Scripts/UI/, Scripts/Core/, etc).  
4. **Compile C\#:** In Godot, go to the bottom panel, click the MSBuild tab, and click the "Build" button (or press Alt+B). This compiles the C\# code so Godot recognizes your scripts.

## **Phase 2: Project Settings**

1. **Input Map (Controls):**  
   * Go to Project \-\> Project Settings \-\> Input Map.  
   * Add the following new actions exactly as written:  
     * move\_left (Assign: A key or Left Arrow)  
     * move\_right (Assign: D key or Right Arrow)  
     * move\_forward (Assign: W key or Up Arrow)  
     * move\_backward (Assign: S key or Down Arrow)  
     * jump (Assign: Spacebar)  
     * interact (Assign: E key)  
2. **Autoloads (GameManager):**  
   * Go to Project \-\> Project Settings \-\> Autoload.  
   * Click the file folder icon, navigate to Scripts/Core/GameManager.cs, and add it. Make sure the Node Name is GameManager. This makes it track rescues and currency universally.

## **Phase 3: Building Elaine (The Player Scene)**

1. Click Scene \-\> New Scene. Choose **CharacterBody3D** as the root node. Name it Elaine.  
2. Add child nodes to Elaine:  
   * **CollisionShape3D:** Assign a CapsuleShape3D to it in the inspector.  
   * **Node3D:** Name this Visuals. Drag your imported Elaine model as a child here.  
   * **AnimationTree:** Set Tree Root to AnimationNodeStateMachine. Set Anim Player to Elaine's AnimationPlayer.  
3. **Set Up Animations:**  
   * Open the AnimationTree editor (bottom panel). Right-click to add states: Idle, Run, Jump, Hover. Connect them with arrows.  
4. **Attach Script:**  
   * Drag ElaineController.cs onto the root Elaine node. Drag the Visuals and AnimationTree nodes into the script's inspector slots.  
5. Save as Elaine.tscn in Scenes/Characters/.

## **Phase 4: The HUD and Dialogue Box**

1. **Main HUD:**  
   * Create a new Scene \-\> **CanvasLayer**, name it HUD. Add Control \-\> Two Labels (Glimmers & Family). Attach HUD.cs to the root and assign the labels. Save as HUD.tscn.  
2. **Dialogue Box:**  
   * Create a new Scene \-\> **CanvasLayer**, name it DialogueUI.  
   * Add a **Panel** (size it to the bottom of the screen). Add two **Labels** inside it (one for Name, one for Dialogue text).  
   * Attach DialogueUI.cs to the root. Assign the Panel and Labels in the inspector. Save as DialogueUI.tscn.

## **Phase 5: The Test Platforming Level**

1. Create a new Scene \-\> **Node3D**. Name it World\_1\_Forest.  
2. Add a **StaticBody3D** for the floor (with a Box CollisionShape3D and MeshInstance3D).  
3. **Instance the Core Elements:** Click the link icon (Instantiate Child Scene). Instance Elaine.tscn, HUD.tscn, and DialogueUI.tscn into the level.  
4. **The Camera:**  
   * Add a **Camera3D**. Attach PlatformerCamera.cs to it. In the inspector, click "Target" and select the Elaine node.  
5. **Collectibles & Hazards:**  
   * Create a new Area3D scene named Collectible. Add a Sphere Mesh/Collision. Attach Collectible.cs. Save and instance a few into your level.  
   * Add a massive Area3D under your map. Attach Hazard.cs. Add a Box collision shape. If Elaine falls, she resets.  
6. Save and press **F6** to test\! You should be able to run, jump, collect Glimmers, and have the camera follow you smoothly.

## **Phase 6: The Cozy Hub Village**

1. Create a new Scene \-\> **Node3D**. Name it HubWorld.  
2. Build the ground. Instance Elaine, HUD, DialogueUI, and a Camera3D (with PlatformerCamera.cs attached) just like before.  
3. **The Family Tree:**  
   * Create a Node3D named FamilyTree. Attach FamilyTree.cs. Add child meshes for the tree stages and Marker3D nodes for where the family will stand.  
4. **The Level Portal:**  
   * Add an Area3D named PortalToWorld1. Attach LevelPortal.cs. Assign World\_1\_Forest.tscn in the inspector.  
5. **Animal Spawner:**  
   * Add a Node3D named AnimalSpawner. Attach HubAnimalSpawner.cs. Add several Marker3D nodes as children.  
6. Save as HubWorld.tscn.

## **Phase 7: Building the Wildlife (NPCs)**

1. Create a new Scene \-\> **CharacterBody3D**. Name it Toby.  
2. Add CollisionShape3D, Visuals (Mesh), and AnimationTree. Attach AnimalBehavior.cs to the root.  
3. Add an Area3D as a child with a large sphere collision (interaction zone). Attach NPCDialogueTrigger.cs. In the inspector, set Name to "Toby" and type a nice translated dialogue.  
4. Save as Toby.tscn.  
5. Back in HubWorld.tscn, click the AnimalSpawner, go to the inspector, add a Dictionary element: Key "Toby", Value Toby.tscn.

**Final Test\!** Open GameManager.cs and temporarily change CanTranslateAnimals \= true;.

Open HubWorld.tscn, press **F6**. Talk to Toby using 'E' to see the dialogue box pop up, watch the Family Tree, and walk into the portal to begin your platforming adventure\!