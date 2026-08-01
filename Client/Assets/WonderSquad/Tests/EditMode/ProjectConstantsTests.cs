using NUnit.Framework;
using WonderSquad.Core.Configuration;
using WonderSquad.Core.Input;

namespace WonderSquad.Tests.EditMode
{
    public sealed class ProjectConstantsTests
    {
        [Test]
        public void RequiredProjectPaths_AreUnityAssetPaths()
        {
            Assert.That(ProjectConstants.BootstrapScenePath, Does.StartWith("Assets/"));
            Assert.That(ProjectConstants.PlayerSandboxScenePath, Does.StartWith("Assets/"));
            Assert.That(ProjectConstants.GameplaySandboxScenePath, Does.StartWith("Assets/"));
            Assert.That(ProjectConstants.RecoverySandboxScenePath, Does.StartWith("Assets/"));
            Assert.That(ProjectConstants.InputActionsAssetPath, Does.StartWith("Assets/"));
        }

        [Test]
        public void InputActions_ExposeTheP0SemanticSet()
        {
            var actionNames = new[]
            {
                InputActionNames.Move,
                InputActionNames.Look,
                InputActionNames.Jump,
                InputActionNames.Interact,
                InputActionNames.Ability,
                InputActionNames.Marker,
                InputActionNames.QuickIntent,
                InputActionNames.Emote,
                InputActionNames.Rescue,
                InputActionNames.Pause
            };

            Assert.That(actionNames, Is.Unique);
            Assert.That(actionNames, Has.Length.EqualTo(10));
        }
    }
}
