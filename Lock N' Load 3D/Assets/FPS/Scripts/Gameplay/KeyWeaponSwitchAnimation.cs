using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Handles the circular arc weapon switch animation.
    /// Put-away: weapon arcs backward and UP (like raising over shoulder)
    /// Pull-out: weapon starts below screen, arcs forward and up into ready position
    /// </summary>
    public class KeyWeaponSwitchAnimation : MonoBehaviour
    {
        public enum SwitchState
        {
            Idle,           // Weapon is up and ready
            PuttingAway,    // Animating weapon backward/up (NO shooting allowed)
            PullingOut,     // Animating weapon from below into ready position
        }

        [Header("Animation Timing")]
        [Tooltip("Duration to put weapon away")]
        public float PutAwayDuration = 0.25f;

        [Tooltip("Duration to pull weapon out")]
        public float PullOutDuration = 0.3f;

        [Header("Put-Away Arc Settings (backward + up)")]
        [Tooltip("How far backward the weapon goes at its farthest point")]
        public float BehindOffset = 0.6f;

        [Tooltip("How far UP the weapon goes at its highest point (off screen)")]
        public float UpOffset = 0.7f;

        [Header("Pull-Out Arc Settings (from below)")]
        [Tooltip("How far below the screen the weapon starts")]
        public float BelowOffset = 1.0f;

        [Tooltip("How far forward the weapon comes during pull-out arc")]
        public float ForwardOffset = 0.25f;

        [Header("Rotation")]
        [Tooltip("Maximum tilt angle during put-away (tilts back significantly, like flipping over shoulder)")]
        public float PutAwayTiltAngle = -110f;

        [Tooltip("Maximum tilt angle during pull-out (tilts forward initially)")]
        public float PullOutTiltAngle = 40f;

        [Header("Shooting Threshold")]
        [Tooltip("Progress (0-1) at which shooting becomes available during pull-out")]
        [Range(0f, 1f)]
        public float ShootableProgressThreshold = 0.5f;

        [Header("Animation Curves")]
        [Tooltip("Easing curve for put-away motion")]
        public AnimationCurve PutAwayCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Easing curve for pull-out motion")]
        public AnimationCurve PullOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // Current state
        public SwitchState CurrentState { get; private set; } = SwitchState.Idle;

        // Animation progress (0-1)
        public float Progress { get; private set; } = 0f;

        // Position offset to be applied to weapon
        public Vector3 SwitchPositionOffset { get; private set; } = Vector3.zero;

        // Rotation offset to be applied to weapon
        public Quaternion SwitchRotationOffset { get; private set; } = Quaternion.identity;

        // Can the player shoot? Only when idle OR during pull-out past threshold
        public bool CanShoot => CurrentState == SwitchState.Idle || 
                                (CurrentState == SwitchState.PullingOut && Progress >= ShootableProgressThreshold);

        // Is a switch animation currently in progress?
        public bool IsSwitching => CurrentState != SwitchState.Idle;

        // Is currently in pull-out phase and past shoot threshold?
        public bool CanSnapToReady => CurrentState == SwitchState.PullingOut && Progress >= ShootableProgressThreshold;

        // Events for KeyWeaponController to hook into
        public System.Action OnPutAwayComplete;
        public System.Action OnPullOutComplete;

        private float m_AnimationStartTime;

        void Update()
        {
            if (CurrentState == SwitchState.Idle)
            {
                SwitchPositionOffset = Vector3.zero;
                SwitchRotationOffset = Quaternion.identity;
                return;
            }

            UpdateSwitchAnimation();
        }

        /// <summary>
        /// Start the weapon put-away animation (arc backward + up)
        /// </summary>
        public void StartPutAway()
        {
            if (CurrentState != SwitchState.Idle) return;

            CurrentState = SwitchState.PuttingAway;
            Progress = 0f;
            m_AnimationStartTime = Time.time;
        }

        /// <summary>
        /// Start pulling out the new weapon (from below)
        /// </summary>
        public void StartPullOut()
        {
            CurrentState = SwitchState.PullingOut;
            Progress = 0f;
            m_AnimationStartTime = Time.time;

            // Start from the "below" position
            CalculatePullOutPosition(0f);
        }

        /// <summary>
        /// Snap weapon to ready position (called when shooting during pull-out)
        /// Like CSGO - shooting interrupts the animation and snaps weapon ready
        /// </summary>
        public void SnapToReady()
        {
            if (CurrentState == SwitchState.PullingOut && Progress >= ShootableProgressThreshold)
            {
                CurrentState = SwitchState.Idle;
                Progress = 0f;
                SwitchPositionOffset = Vector3.zero;
                SwitchRotationOffset = Quaternion.identity;
            }
        }

        /// <summary>
        /// Force-complete the animation and go to idle
        /// </summary>
        public void ForceComplete()
        {
            CurrentState = SwitchState.Idle;
            Progress = 0f;
            SwitchPositionOffset = Vector3.zero;
            SwitchRotationOffset = Quaternion.identity;
        }

        void UpdateSwitchAnimation()
        {
            float elapsed = Time.time - m_AnimationStartTime;
            float duration;

            switch (CurrentState)
            {
                case SwitchState.PuttingAway:
                    duration = PutAwayDuration;
                    Progress = Mathf.Clamp01(elapsed / duration);
                    float easedPutAway = PutAwayCurve.Evaluate(Progress);
                    CalculatePutAwayPosition(easedPutAway);

                    if (Progress >= 1f)
                    {
                        OnPutAwayComplete?.Invoke();
                    }
                    break;

                case SwitchState.PullingOut:
                    duration = PullOutDuration;
                    Progress = Mathf.Clamp01(elapsed / duration);
                    float easedPullOut = PullOutCurve.Evaluate(Progress);
                    CalculatePullOutPosition(easedPullOut);

                    if (Progress >= 1f)
                    {
                        CurrentState = SwitchState.Idle;
                        Progress = 0f;
                        SwitchPositionOffset = Vector3.zero;
                        SwitchRotationOffset = Quaternion.identity;
                        OnPullOutComplete?.Invoke();
                    }
                    break;
            }
        }

        /// <summary>
        /// Calculate position for put-away animation (arc backward + UP, going off screen)
        /// </summary>
        void CalculatePutAwayPosition(float progress)
        {
            // Arc backward and UP using sine curve - weapon rotates back over shoulder
            float angle = progress * Mathf.PI * 0.5f;

            // Z: goes backward (negative) - more aggressive curve
            float zOffset = -Mathf.Sin(angle) * BehindOffset;

            // Y: goes UP (positive) and off screen - use squared progress for acceleration
            float yOffset = Mathf.Pow(progress, 1.5f) * UpOffset;

            // X: slight sway for natural motion, more pronounced
            float xOffset = Mathf.Sin(angle * 2f) * 0.06f;

            SwitchPositionOffset = new Vector3(xOffset, yOffset, zOffset);

            // Tilt weapon back significantly as it goes up/behind (like flipping over shoulder)
            // Also add slight roll for more natural feel
            float tiltAngle = progress * PutAwayTiltAngle;
            float rollAngle = Mathf.Sin(angle) * 10f; // slight roll
            SwitchRotationOffset = Quaternion.Euler(tiltAngle, 0f, rollAngle);
        }

        /// <summary>
        /// Calculate position for pull-out animation (arc from below)
        /// </summary>
        void CalculatePullOutPosition(float progress)
        {
            // Inverse progress for position (starts far, ends at rest)
            float inverseProgress = 1f - progress;
            float angle = inverseProgress * Mathf.PI * 0.5f;

            // Y: starts below, comes up
            float yOffset = -Mathf.Sin(angle) * BelowOffset;

            // Z: slight forward motion during the arc
            float zOffset = Mathf.Sin(angle * 2f) * ForwardOffset * 0.5f;

            // X: slight sway
            float xOffset = -Mathf.Sin(angle * 2f) * 0.02f;

            SwitchPositionOffset = new Vector3(xOffset, yOffset, zOffset);

            // Tilt weapon forward at start, level out at end
            float tiltAngle = inverseProgress * PullOutTiltAngle;
            SwitchRotationOffset = Quaternion.Euler(tiltAngle, 0f, 0f);
        }
    }
}

