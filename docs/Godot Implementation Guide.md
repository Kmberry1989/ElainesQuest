# **Elaine's Quest: Complete Godot Setup Guide**

This document will take you from a totally blank Godot project to a running, playable prototype featuring Elaine, the Hub world, UI, collectibles, and the game manager. Follow these steps sequentially.

## **Phase 1: Project Creation & Fundamentals**

1. **Download Godot:** Ensure you have the **Godot 4 .NET version** downloaded from the official website. You will also need an IDE like Visual Studio, Visual Studio Code, or JetBrains Rider installed to edit the C\# scripts.  
2. **Create Project:** Open Godot, click "New Project", name it ElainesQuest, and select "Forward+" as the renderer.  
3. **Build Folder Structure:** Run the provided build\_project.py script on your desktop to generate the exact folder structure and code files, or manually create folders matching Assets/, Scenes/, and Scripts/. Move all provided .cs files into their respective Scripts/ subfolders.  
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
2. **Autoload (The GameManager):**  
   * Go to Project \-\> Project Settings \-\> Autoload.  
   * Click the file folder icon, navigate to Scripts/Core/GameManager.cs, and add it. Make sure the Node Name is GameManager. This makes the GameManager run constantly in the background.

## **Phase 3: Building Elaine (The Player Scene)**

1. Click Scene \-\> New Scene. Choose **CharacterBody3D** as the root node. Name it Elaine.  
2. Add the following child nodes to Elaine:  
   * **CollisionShape3D:** In the Inspector, assign a new CapsuleShape3D to it. Adjust the height to match your character model.  
   * **Node3D:** Name this Visuals. Drag your imported T-Pose Elaine 3D model (e.g., from Mixamo) as a child of this Visuals node.  
   * **AnimationTree:** In the inspector, set Tree Root to AnimationNodeStateMachine. Set Anim Player to the AnimationPlayer that came with your 3D model.  
3. **Set Up Animations:**  
   * Open the AnimationTree editor (bottom panel).  
   * Right-click to add Animations. Name them EXACTLY: Idle, Run, Jump, Hover.  
   * Connect them with arrows (e.g., Idle \<-\> Run).  
4. **Attach Script:**  
   * Drag ElaineController.cs onto the root Elaine node.  
   * In the Inspector for Elaine, drag the Visuals node into the Visuals script slot, and the AnimationTree node into the Anim Tree script slot.  
5. Save the scene as Elaine.tscn in Scenes/Characters/.

## **Phase 4: The User Interface (HUD)**

1. Click Scene \-\> New Scene. Choose **CanvasLayer** as the root node. Name it HUD.  
2. Add a **Control** node as a child. In the 2D layout view, set its anchors to "Full Rect" (takes up the whole screen).  
3. Add two **Label** nodes as children of the Control.  
   * Name one GlimmersLabel and place it top-left. Type "Glimmers: 0" in its text.  
   * Name the other FamilyLabel and place it top-right. Type "Family Rescued: 0 / 5" in its text.  
4. Attach HUD.cs to the root HUD node.  
5. In the inspector, assign the GlimmersLabel and FamilyLabel nodes to their respective slots in the script.  
6. Save the scene as HUD.tscn in Scenes/UI/.

## **Phase 5: The Test Platforming Level**

1. Click Scene \-\> New Scene. Choose **Node3D** as the root node. Name it TestLevel.  
2. Add a **StaticBody3D** for the floor. Give it a **CollisionShape3D** (BoxShape) and a **MeshInstance3D** (BoxMesh) so you have ground to stand on.  
3. **Instance Elaine:** Click the link icon (Instantiate Child Scene) and select Elaine.tscn. Move her above the floor.  
4. **Instance the HUD:** Click the link icon and select HUD.tscn.  
5. **Add a Collectible:**  
   * Create a new Scene (Root: Area3D). Name it Collectible.  
   * Add a CollisionShape3D (Sphere) and a MeshInstance3D (Sphere, scaled small like a coin).  
   * Attach Collectible.cs. Save it as Collectible.tscn.  
   * Instance a few of these in your TestLevel.  
6. **Add a Hazard (Death Pit):**  
   * Add an **Area3D** under the floor in TestLevel. Attach Hazard.cs. Add a wide CollisionShape3D (Box). If Elaine falls here, she respawns.  
7. Save the level as World\_1\_Forest.tscn in Scenes/Levels/. Press **F6** to test playing this specific scene. You should be able to run, jump, hover, collect Glimmers, and see the UI update\!

## **Phase 6: The Cozy Hub World**

1. Click Scene \-\> New Scene. Choose **Node3D**. Name it HubWorld.  
2. Build a floor (like you did in the test level). Instance Elaine.tscn and HUD.tscn here as well.  
3. **The Family Tree:**  
   * Create an empty Node3D in the center of town. Name it FamilyTree. Attach FamilyTree.cs.  
   * Add child mesh nodes for different tree sizes, and Marker3D nodes for family spawn points. Assign them all in the Inspector.  
4. **The Level Portal:**  
   * Add an **Area3D** named PortalToWorld1. Attach LevelPortal.cs.  
   * Add a CollisionShape3D (Box) and a MeshInstance3D (maybe a glowing doorway).  
   * In the Inspector for the script, click Level Scene Path and select your World\_1\_Forest.tscn.  
5. **The Animal Spawner:**  
   * Add a **Node3D** named AnimalSpawner. Attach HubAnimalSpawner.cs.  
   * Add several **Marker3D** nodes as children of it (these are where Toby, Sasha, etc., will spawn).  
6. Save the scene as HubWorld.tscn in Scenes/Hub/.

## **Phase 7: Building the Wildlife (NPCs)**

1. Create a new Scene (Root: CharacterBody3D). Name it Animal\_Toby.  
2. Add a **CollisionShape3D** (Capsule), a **Visuals** node (with Toby the Turtle's mesh), and an **AnimationTree** (just like Elaine).  
3. Attach AnimalBehavior.cs to the root node to let Toby wander around.  
4. Add an **Area3D** as a child of Toby. Give it a large CollisionShape (Sphere) for interaction range. Attach NPC.cs (or NPCDialogueTrigger.cs).  
5. In the NPC inspector, set the Character Name to "Toby" and Translated Dialogue to "Nice to be out of those caves\!".  
6. Save as Toby.tscn in Scenes/Characters/.  
7. **Crucial Link:** Go back to your HubWorld.tscn, select the AnimalSpawner node. In the inspector under Animal Prefabs, click "Add Element", type the key "Toby", and load the Toby.tscn file as the value.

**Final Test:**

In GameManager.cs, temporarily add BefriendAnimal("Toby"); inside the \_Ready() function.

Open HubWorld.tscn and press **F6**. You should spawn in, see Toby wandering around, be able to talk to him using 'E', and walk into the glowing portal to instantly teleport to your platforming level\!