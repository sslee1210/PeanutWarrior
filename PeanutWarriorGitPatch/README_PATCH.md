# PeanutWarrior first playable prototype patch

Copy the `Assets` folder into the Unity project root and merge it with the existing `Assets` folder.

This prototype automatically creates a playable test scene at runtime:

- automatic monster spawning and basic attacks
- 100 monster kills required per stage
- manual boss challenge button
- automatic boss challenge toggle
- full HP restoration on boss entry
- boss death advances the stage
- death while hunting moves to the previous stage
- death during a boss battle stays on the same stage and resets progress to 0/100
- infinite worlds generated in groups of 30 stages

After copying, open any scene in Unity and press Play.
