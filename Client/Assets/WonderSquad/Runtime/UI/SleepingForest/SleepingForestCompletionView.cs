using System;
using UnityEngine;
using UnityEngine.UI;

namespace WonderSquad.UI.SleepingForest
{
    /// <summary>
    /// Displays scene-local completion feedback without evaluating gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SleepingForestCompletionView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Visual content toggled by immutable view data.")]
        private GameObject contentRoot;

        [SerializeField]
        [Tooltip("uGUI text containing the current fallback message.")]
        private Text messageText;

        private SleepingForestCompletionViewData displayedData;
        private bool hasDisplayedData;
        private int refreshCount;

        public event Action BecameAvailable;

        public SleepingForestCompletionViewData DisplayedData =>
            displayedData;

        public bool IsVisible =>
            contentRoot != null && contentRoot.activeSelf;

        public string DisplayedMessage =>
            messageText == null ? string.Empty : messageText.text;

        public int RefreshCount => refreshCount;

        public bool HasValidConfiguration =>
            contentRoot != null && messageText != null;

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

        public bool Configure(
            GameObject localContentRoot,
            Text localMessageText)
        {
            contentRoot = localContentRoot;
            messageText = localMessageText;
            ResetPresentation();
            return HasValidConfiguration;
        }

        public bool Render(SleepingForestCompletionViewData viewData)
        {
            if (hasDisplayedData && displayedData == viewData)
            {
                return false;
            }

            displayedData = viewData;
            hasDisplayedData = true;
            refreshCount++;
            if (!viewData.IsVisible)
            {
                HideContent();
                return true;
            }

            messageText.text = viewData.FallbackMessage;
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
            if (messageText != null)
            {
                messageText.text = string.Empty;
            }

            if (contentRoot != null)
            {
                contentRoot.SetActive(false);
            }
        }
    }
}
