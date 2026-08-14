using UnityEngine;

namespace AetherArk.Runtime
{
    public static class AetherArkBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindFirstObjectByType<GameController>() != null) return;
            var gameObject = new GameObject("AetherArkGame");
            Object.DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<GameController>();
        }
    }
}

