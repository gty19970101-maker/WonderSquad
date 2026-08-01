using UnityEngine;

namespace WonderSquad.Player.Camera
{
    /// <summary>
    /// Marks the presentation-only anchor followed by the local player camera.
    /// It owns no camera, movement, input, or networking behavior.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCameraTarget : MonoBehaviour
    {
        public Transform TargetTransform => transform;

        public bool IsConfigurationValid => transform.parent != null;
    }
}
