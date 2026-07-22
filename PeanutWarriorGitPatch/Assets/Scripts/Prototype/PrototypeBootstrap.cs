using PeanutWarrior.Combat;
using PeanutWarrior.Core;
using PeanutWarrior.Stage;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    public static class PrototypeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreatePrototype()
        {
            if (Object.FindFirstObjectByType<PrototypeGame>() != null) return;

            if (Camera.main == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.backgroundColor = new Color(0.12f, 0.09f, 0.06f);
            }

            GameManager manager = Object.FindFirstObjectByType<GameManager>();
            if (manager == null) manager = new GameObject("GameManager").AddComponent<GameManager>();

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                GameObject playerObject = new GameObject("Peanut Warrior");
                SpriteRenderer renderer = playerObject.AddComponent<SpriteRenderer>();
                renderer.sprite = PrototypeVisuals.WhiteSprite;
                renderer.color = new Color(0.8f, 0.52f, 0.22f);
                playerObject.transform.position = new Vector3(-2.4f, 0.2f, 0f);
                playerObject.transform.localScale = new Vector3(1.2f, 1.6f, 1f);
                player = playerObject.AddComponent<PlayerController>();
            }

            StageManager stage = Object.FindFirstObjectByType<StageManager>();
            if (stage == null)
            {
                GameObject stageObject = new GameObject("StageManager");
                stage = stageObject.AddComponent<StageManager>();
                typeof(StageManager)
                    .GetField("player", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(stage, player);
            }

            new GameObject("PrototypeGame").AddComponent<PrototypeGame>();
        }
    }
}
