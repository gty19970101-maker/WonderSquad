using UnityEngine;
using WonderSquad.Bootstrap.Logging;
using WonderSquad.Core.Configuration;
using WonderSquad.Core.Logging;

namespace WonderSquad.Bootstrap
{
    public static class BootstrapEntry
    {
        private static bool isInitialized;

        public static bool IsInitialized => isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            isInitialized = false;
            ProjectLog.Configure(null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            ProjectLog.Configure(new UnityProjectLogger());

            var root = GameObject.Find(ProjectConstants.BootstrapRootName);
            if (root == null)
            {
                root = new GameObject(ProjectConstants.BootstrapRootName);
            }

            if (root.GetComponent<BootstrapLifetime>() == null)
            {
                root.AddComponent<BootstrapLifetime>();
            }

            Object.DontDestroyOnLoad(root);
            isInitialized = true;
            ProjectLog.Information("P0 Bootstrap initialized. Gameplay systems are not registered.");
        }
    }
}
