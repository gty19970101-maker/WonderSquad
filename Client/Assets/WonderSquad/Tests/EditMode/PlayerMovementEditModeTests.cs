using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WonderSquad.Player.Movement;

namespace WonderSquad.Tests.EditMode
{
    public sealed class PlayerMovementEditModeTests
    {
        private const string MovementSettingsPath =
            "Assets/WonderSquad/ScriptableObjects/Configuration/MovementSettings.asset";

        [Test]
        public void MovementSettings_DefaultAssetIsValid()
        {
            var settings =
                AssetDatabase.LoadAssetAtPath<MovementSettings>(
                    MovementSettingsPath);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.IsValid, Is.True);
            Assert.That(settings.WalkingSpeed, Is.GreaterThan(0f));
            Assert.That(
                settings.RotationSpeedDegreesPerSecond,
                Is.GreaterThan(0f));
        }

        [Test]
        public void CalculateWorldDirection_WhenInputIsDiagonal_NormalizesInput()
        {
            var direction =
                PlayerMovementMath.CalculateWorldDirection(Vector2.one);

            Assert.That(direction.y, Is.EqualTo(0f));
            Assert.That(direction.x, Is.GreaterThan(0f));
            Assert.That(direction.z, Is.GreaterThan(0f));
            Assert.That(direction.magnitude, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void CalculateHorizontalVelocity_LimitsWalkingSpeed()
        {
            var settings = LoadSettings();
            var velocity =
                PlayerMovementMath.CalculateHorizontalVelocity(
                    new Vector2(10f, 10f),
                    settings.WalkingSpeed);

            Assert.That(
                velocity.magnitude,
                Is.LessThanOrEqualTo(settings.WalkingSpeed));
        }

        [Test]
        public void MovementSettings_GravityParametersAreValid()
        {
            var settings = LoadSettings();

            Assert.That(settings.GravityAcceleration, Is.LessThan(0f));
            Assert.That(settings.MaximumFallSpeed, Is.GreaterThan(0f));
            Assert.That(settings.GroundedVerticalSpeed, Is.LessThanOrEqualTo(0f));
            Assert.That(
                settings.GroundedVerticalSpeed,
                Is.GreaterThanOrEqualTo(-settings.MaximumFallSpeed));
        }

        private static MovementSettings LoadSettings()
        {
            var settings =
                AssetDatabase.LoadAssetAtPath<MovementSettings>(
                    MovementSettingsPath);

            Assert.That(settings, Is.Not.Null);
            return settings;
        }
    }
}
