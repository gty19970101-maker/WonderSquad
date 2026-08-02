using System;
using UnityEngine;
using UnityEngine.UI;
using WonderSquad.Interaction.Prompt;

namespace WonderSquad.UI.Interaction
{
    /// <summary>
    /// Displays immutable prompt data without finding or modifying gameplay
    /// targets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Visual container toggled by the prompt state.")]
        private GameObject contentRoot;

        [SerializeField]
        [Tooltip("uGUI text showing the active device binding.")]
        private Text bindingText;

        [SerializeField]
        [Tooltip("uGUI text showing the local action description.")]
        private Text actionText;

        private InteractionPromptData displayedData;
        private bool hasDisplayedData;
        private int refreshCount;

        public event Action BecameAvailable;

        public InteractionPromptData DisplayedData =>
            displayedData;

        public bool IsVisible =>
            contentRoot != null &&
            contentRoot.activeSelf;

        public string DisplayedBindingText =>
            bindingText == null
                ? string.Empty
                : bindingText.text;

        public string DisplayedActionText =>
            actionText == null
                ? string.Empty
                : actionText.text;

        public int RefreshCount => refreshCount;

        public bool HasValidConfiguration =>
            contentRoot != null &&
            bindingText != null &&
            actionText != null;

        private void Awake()
        {
            ResetPresentation();
        }

        private void OnEnable()
        {
            ResetPresentation();
            BecameAvailable?.Invoke();
        }

        private void OnDisable()
        {
            ResetPresentation();
        }

        /// <summary>
        /// Supplies the presentation-only references before rendering.
        /// </summary>
        public bool Configure(
            GameObject localContentRoot,
            Text localBindingText,
            Text localActionText)
        {
            contentRoot = localContentRoot;
            bindingText = localBindingText;
            actionText = localActionText;
            ResetPresentation();
            return HasValidConfiguration;
        }

        /// <summary>
        /// Applies data only when its immutable value differs from the
        /// currently rendered state.
        /// </summary>
        public bool Render(InteractionPromptData promptData)
        {
            if (hasDisplayedData &&
                displayedData == promptData)
            {
                return false;
            }

            displayedData = promptData;
            hasDisplayedData = true;
            refreshCount++;

            if (!promptData.IsDisplayable)
            {
                HideContent();
                return true;
            }

            bindingText.text =
                promptData.BindingDisplayText;
            actionText.text =
                promptData.ActionText;
            contentRoot.SetActive(true);
            return true;
        }

        public void ResetPresentation()
        {
            displayedData = default;
            hasDisplayedData = false;
            HideContent();
        }

        private void HideContent()
        {
            if (bindingText != null)
            {
                bindingText.text = string.Empty;
            }

            if (actionText != null)
            {
                actionText.text = string.Empty;
            }

            if (contentRoot != null)
            {
                contentRoot.SetActive(false);
            }
        }
    }
}
