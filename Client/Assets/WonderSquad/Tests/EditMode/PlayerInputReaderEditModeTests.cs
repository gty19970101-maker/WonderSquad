using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using WonderSquad.Core.Configuration;
using WonderSquad.Core.Input;
using WonderSquad.Player.Input;

namespace WonderSquad.Tests.EditMode
{
    public sealed class PlayerInputReaderEditModeTests
    {
        private const string PlayerPrefabPath =
            "Assets/WonderSquad/Prefabs/Player/Player.prefab";

        [Test]
        public void Clamp_WhenInputIsZero_ReturnsZero()
        {
            Assert.That(MoveInputValue.Clamp(Vector2.zero), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Clamp_WhenInputExceedsUnitLength_LimitsMagnitude()
        {
            var result = MoveInputValue.Clamp(new Vector2(2f, 0f));

            Assert.That(result, Is.EqualTo(Vector2.right));
            Assert.That(result.magnitude, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void Clamp_WhenInputIsDiagonal_NormalizesToUnitLength()
        {
            var result = MoveInputValue.Clamp(Vector2.one);

            Assert.That(result.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(result.x, Is.EqualTo(result.y).Within(0.0001f));
        }

        [Test]
        public void Clamp_WhenInputIsNotFinite_ReturnsZero()
        {
            var result = MoveInputValue.Clamp(
                new Vector2(float.NaN, float.PositiveInfinity));

            Assert.That(result, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void TryValidateMoveAction_WhenReferenceIsMissing_ReturnsFalse()
        {
            var isValid =
                PlayerInputConfigurationValidator.TryValidateMoveAction(
                    null,
                    out var error);

            Assert.That(isValid, Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void TryValidateMoveAction_WhenActionContractIsInvalid_ReturnsFalse()
        {
            var actionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            var actionMap = actionAsset.AddActionMap("Test");
            var action = actionMap.AddAction(
                "InvalidMove",
                InputActionType.Button,
                "<Keyboard>/space");
            var actionReference = InputActionReference.Create(action);

            try
            {
                var isValid =
                    PlayerInputConfigurationValidator.TryValidateMoveAction(
                        actionReference,
                        out var error);

                Assert.That(isValid, Is.False);
                Assert.That(error, Is.Not.Empty);
            }
            finally
            {
                Object.DestroyImmediate(actionReference);
                Object.DestroyImmediate(actionAsset);
            }
        }

        [Test]
        public void ApprovedMoveAction_HasValidContractAndKeyboardBindings()
        {
            var moveActionReference = LoadMoveActionReference();

            try
            {
                Assert.That(
                    PlayerInputConfigurationValidator.TryValidateMoveAction(
                        moveActionReference,
                        out var error),
                    Is.True,
                    error);

                var bindingPaths = moveActionReference.action.bindings
                    .Where(binding => !binding.isComposite)
                    .Select(binding => binding.path)
                    .ToArray();

                Assert.That(
                    bindingPaths,
                    Is.SupersetOf(new[]
                    {
                        "<Keyboard>/w",
                        "<Keyboard>/a",
                        "<Keyboard>/s",
                        "<Keyboard>/d",
                        "<Keyboard>/upArrow",
                        "<Keyboard>/downArrow",
                        "<Keyboard>/leftArrow",
                        "<Keyboard>/rightArrow"
                    }));
            }
            finally
            {
                Object.DestroyImmediate(moveActionReference);
            }
        }

        [Test]
        public void PlayerPrefab_HasValidInputReaderAndDebugLogger()
        {
            var playerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            Assert.That(playerPrefab, Is.Not.Null);
            var inputReader = playerPrefab.GetComponent<PlayerInputReader>();

            Assert.That(inputReader, Is.Not.Null);
            Assert.That(inputReader.HasValidConfiguration, Is.True);
            Assert.That(
                playerPrefab.GetComponent<PlayerInputDebugLogger>(),
                Is.Not.Null);
        }

        private static InputActionReference LoadMoveActionReference()
        {
            var actionAsset =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    ProjectConstants.InputActionsAssetPath);

            Assert.That(actionAsset, Is.Not.Null);
            var moveAction =
                actionAsset.FindAction(InputActionNames.Move, true);
            return InputActionReference.Create(moveAction);
        }
    }
}
