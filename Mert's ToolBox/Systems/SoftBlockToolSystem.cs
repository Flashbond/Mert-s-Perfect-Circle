using Colossal.Mathematics;
using Game.Prefabs;
using MertsToolBox.Core;
using System.Collections.Generic;
using Unity.Mathematics;

namespace MertsToolBox.Systems
{
    public partial class SoftBlockToolSystem : MertBaseToolSystem
    {
        #region Fields & Properties
        private int m_CurrentSessionWidth = -1;
        public readonly int[] m_WidthSteps = new int[] { 8, 4, 2, 1 };
        private int m_CurrentWidthStepIndex = 0;

        private int m_CurrentSessionLength = -1;
        public readonly int[] m_LengthSteps = new int[] { 8, 4, 2, 1 };
        private int m_CurrentLengthStepIndex = 0;

        private float m_BorderRadius = 5f;

        private int m_PendingWidthChange = 0;
        private int m_TargetWidthStep = -1;
        private int m_PendingLengthChange = 0;
        private int m_TargetLengthStep = -1;
        private float m_PendingBorderRadiushange = 0f;

        private enum BorderRadiusSnapState
        {
            Armed,
            Locked,
            Disarmed
        }

        private BorderRadiusSnapState m_BorderRadiusSnapState = BorderRadiusSnapState.Armed;

        private const float BorderRadiusSnapTarget = 8f;
        private const float BorderRadiusSnapThreshold = 0.4f;

        /// <summary>
        /// Standard cubic Bézier circle approximation.
        /// </summary>
        private static readonly float Kappa = 4f * (math.sqrt(2f) - 1f) / 3f;
        /// <summary>
        /// Gets the name of the tool.
        /// </summary>
        public override string ToolId => "SoftBlock";
        public override string ToolName => "Soft Block";

        /// <summary>
        /// Indicates whether this tool requires snap enforcement.
        /// </summary>
        protected override bool RequiresSnapEnforcement => true;
        protected override bool HandlesOwnElevationInput => true;
        #endregion

        #region Input Queuing & State
        public void BeginBorderRadiusDrag()
        {
            // If user starts dragging while already near 8,
            // allow fine adjustment away from 8.
            m_BorderRadiusSnapState =
                math.abs(m_BorderRadius - BorderRadiusSnapTarget) <= BorderRadiusSnapThreshold
                    ? BorderRadiusSnapState.Disarmed
                    : BorderRadiusSnapState.Armed;
        }

        public void EndBorderRadiusDrag()
        {
            m_BorderRadiusSnapState = BorderRadiusSnapState.Armed;
        }
   
        protected override void OnSettingsChanged()
        {
            if (Mod.settings != null)
            {
                m_CurrentSessionWidth = Mod.settings.DefaultSoftBlockWidth;
                m_CurrentSessionLength = Mod.settings.DefaultSoftBlockLength;
            }

            base.OnSettingsChanged();
        }
        /// <summary>
        /// Queues a change in the width based on the given direction.
        /// </summary>
        public void QueueWidthChange(int direction) => m_PendingWidthChange += direction;

        /// <summary>
        /// Queues a step cycle for the width adjustment.
        /// </summary>
        public void QueueSetWidthStep(int value) => m_TargetWidthStep = value;

        /// <summary>
        /// Queues a change in the length based on the given direction.
        /// </summary>
        public void QueueLengthChange(int direction) => m_PendingLengthChange += direction;

        /// <summary>
        /// Queues a step cycle for the length adjustment.
        /// </summary>
        public void QueueSetLengthStep(int value) => m_TargetLengthStep = value;

        /// <summary>
        /// Sets the current N value directly from the UI slider.
        /// </summary>
        public void SetBorderRadiusFromUi(float value)
        {
            SetCurrentBorderRadius(value, useSnap: true);
        }
        #endregion

        #region Metrics & Data Retrieval
        /// <summary>
        /// Retrieves the step size for width adjustments.
        /// </summary>
        public int GetWidthStepSize() => GetCurrentStepValue(m_CurrentWidthStepIndex, m_WidthSteps);

        /// <summary>
        /// Retrieves the step size for length adjustments.
        /// </summary>
        public int GetLengthStepSize() => GetCurrentStepValue(m_CurrentLengthStepIndex, m_LengthSteps);

        /// <summary>
        /// Retrieves the current session width, applying default settings if uninitialized.
        /// </summary>
        public int GetCurrentWidth() { if (m_CurrentSessionWidth < 0) m_CurrentSessionWidth = Mod.settings != null ? Mod.settings.DefaultSoftBlockWidth : 96; return m_CurrentSessionWidth; }

        /// <summary>
        /// Retrieves the current session length, applying default settings if uninitialized.
        /// </summary>
        public int GetCurrentLength() { if (m_CurrentSessionLength < 0) m_CurrentSessionLength = Mod.settings != null ? Mod.settings.DefaultSoftBlockLength : 192; return m_CurrentSessionLength; }

        /// <summary>
        /// Calculates and retrieves the current N slider value mapped to a 1-15 scale.
        /// </summary>
        public float GetCurrentBorderRadius()
        {
            return m_BorderRadius;
        }
        #endregion

        #region Core Tool Processing
        /// <summary>
        /// Processes user inputs and applies queued changes to the tool state.
        /// </summary>
        protected override void ProcessToolInput()
        {
            if (!ToolEnabled) return;

            if (m_TargetWidthStep != -1)
            {
                m_CurrentWidthStepIndex = GetIndexFromValue(
                    m_TargetWidthStep,
                    m_WidthSteps,
                    m_CurrentWidthStepIndex
                );
                m_TargetWidthStep = -1;
            }

            if (m_PendingWidthChange != 0) { ChangeWidth(m_PendingWidthChange); m_PendingWidthChange = 0; }
            if (m_TargetLengthStep != -1)
            {
                m_CurrentLengthStepIndex = GetIndexFromValue(
                    m_TargetLengthStep,
                    m_LengthSteps,
                    m_CurrentLengthStepIndex
                );
                m_TargetLengthStep = -1;
            }

            if (m_PendingLengthChange != 0) { ChangeLength(m_PendingLengthChange); m_PendingLengthChange = 0; }

            if (math.abs(m_PendingBorderRadiushange) > 0.001f)
            {
                SetCurrentBorderRadius(GetCurrentBorderRadius() + m_PendingBorderRadiushange);
                m_PendingBorderRadiushange = 0f;
            }
            if (Mod.settings != null && Mod.settings.UseCtrlWheelForSoftBlockBorderRadius &&
                UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.ctrlKey.isPressed)
            {
                int scrollDir = GetScrollDirection();
                if (scrollDir != 0) SetCurrentBorderRadiusFromWheel(scrollDir * 0.1f);
            }
        }
        private void SetCurrentBorderRadiusFromWheel(float delta)
        {
            float current = m_BorderRadius;
            float raw = math.clamp(current + delta, 1f, 10f);

            float currentDist = math.abs(current - BorderRadiusSnapTarget);
            float rawDist = math.abs(raw - BorderRadiusSnapTarget);

            // Wheel behavior:
            // If user enters the pill boundary from outside, snap once to 8.
            // Then disarm so the next wheel tick allows fine adjustment.
            bool enteredSnapZoneFromOutside =
                currentDist > BorderRadiusSnapThreshold &&
                rawDist <= BorderRadiusSnapThreshold;

            if (enteredSnapZoneFromOutside)
            {
                m_BorderRadiusSnapState = BorderRadiusSnapState.Disarmed;
                SetCurrentBorderRadius(BorderRadiusSnapTarget, useSnap: false);
                return;
            }

            // Once user moves clearly away, re-arm snap.
            if (rawDist > BorderRadiusSnapThreshold)
            {
                m_BorderRadiusSnapState = BorderRadiusSnapState.Armed;
            }

            SetCurrentBorderRadius(raw, useSnap: false);
        }
        /// <summary>
        /// Changes the width by aligning it to the next step value.
        /// </summary>
        public void ChangeWidth(int direction)
        {
            int stepSize = GetCurrentStepValue(m_CurrentWidthStepIndex, m_WidthSteps);
            int nextValue = GetNextStepAlignedInt(GetCurrentWidth(), stepSize, direction);
            SetCurrentWidth(nextValue);
        }

        /// <summary>
        /// Changes the length by aligning it to the next step value.
        /// </summary>
        public void ChangeLength(int direction)
        {
            int stepSize = GetCurrentStepValue(m_CurrentLengthStepIndex, m_LengthSteps);
            int nextValue = GetNextStepAlignedInt(GetCurrentLength(), stepSize, direction);
            SetCurrentLength(nextValue);
        }

        /// <summary>
        /// Calculates the minimum allowed dimension based on the road prefab width.
        /// </summary>
        private int GetMinimumAllowedSize()
        {
            // Parametre ve hesaplama silindi, doğrudan miras alınan değişken kullanılıyor
            return (int)math.ceil(m_CurrentRoadWidth * 3f);
        }

        /// <summary>
        /// Safely sets the current width within bounds and queues a preview rebuild.
        /// </summary>
        private void SetCurrentWidth(int width)
        {
            int dynamicMinBound = GetMinimumAllowedSize(); // Parametre silindi

            int clamped = math.clamp(width, dynamicMinBound, 940);
            if (m_CurrentSessionWidth == clamped) return;

            m_CurrentSessionWidth = clamped;
            QueuePreviewRebuild();
        }

        /// <summary>
        /// Safely sets the current length within bounds and queues a preview rebuild.
        /// </summary>
        private void SetCurrentLength(int length)
        {
            int dynamicMinBound = GetMinimumAllowedSize(); // Parametre silindi

            int clamped = math.clamp(length, dynamicMinBound, 940);
            if (m_CurrentSessionLength == clamped) return;

            m_CurrentSessionLength = clamped;
            QueuePreviewRebuild();
        }

        /// <summary>
        /// Converts the UI slider value to the mathematical N parameter and updates the state.
        /// </summary>
        private void SetCurrentBorderRadius(float targetSlider, bool useSnap = true)
        {
            float raw = math.clamp(targetSlider, 1f, 10f);
            float nextSlider = raw;

            if (useSnap)
            {
                float dist = math.abs(raw - BorderRadiusSnapTarget);

                if (m_BorderRadiusSnapState == BorderRadiusSnapState.Armed &&
                    dist <= BorderRadiusSnapThreshold)
                {
                    m_BorderRadiusSnapState = BorderRadiusSnapState.Locked;
                    nextSlider = BorderRadiusSnapTarget;
                }
                else if (m_BorderRadiusSnapState == BorderRadiusSnapState.Locked)
                {
                    if (dist <= BorderRadiusSnapThreshold)
                    {
                        nextSlider = BorderRadiusSnapTarget;
                    }
                    else
                    {
                        m_BorderRadiusSnapState = BorderRadiusSnapState.Armed;
                        nextSlider = raw;
                    }
                }
                else if (m_BorderRadiusSnapState == BorderRadiusSnapState.Disarmed)
                {
                    nextSlider = raw;

                    if (dist > BorderRadiusSnapThreshold)
                        m_BorderRadiusSnapState = BorderRadiusSnapState.Armed;
                }
            }

            if (math.abs(m_BorderRadius - nextSlider) < 0.01f)
                return;

            m_BorderRadius = nextSlider;
            QueuePreviewRebuild();
        }
        #endregion

        #region Geometry Generation
        /// <summary>
        /// Attempts to generate the sub-networks and cells for the super ellipse geometry.
        /// </summary>
        protected override bool TryGenerateGeometry(NetPrefab roadPrefab, out ObjectSubNetInfo[] subNets, out int widthCells, out int depthCells, out float costElevation)
        {
            int minAllowed = GetMinimumAllowedSize();
            if (m_CurrentSessionWidth < minAllowed) m_CurrentSessionWidth = minAllowed;
            if (m_CurrentSessionLength < minAllowed) m_CurrentSessionLength = minAllowed;
            subNets = null; widthCells = 0; depthCells = 0; costElevation = 0f;
            float buildRx = (m_CurrentSessionWidth - m_CurrentRoadWidth) * 0.5f;
            float buildRy = (m_CurrentSessionLength - m_CurrentRoadWidth) * 0.5f;
            if (buildRx < m_CurrentRoadWidth || buildRy < m_CurrentRoadWidth) return false;
            costElevation = GetCurrentNetToolElevation();

            subNets = BuildAdaptiveShapeSubNets(roadPrefab, buildRx, buildRy, costElevation );

            widthCells = (int)math.ceil(m_CurrentSessionWidth / 8f);
            depthCells = (int)math.ceil(m_CurrentSessionLength / 8f);
            return true;
        }

        /// <summary>
        /// Builds the bezier curves representing the super ellipse's sub-networks based on the Lamé curve equation.
        /// </summary>
        private ObjectSubNetInfo[] BuildAdaptiveShapeSubNets(NetPrefab roadPrefab,float rx,float ry,float elevation)
        {
            
            const float MinLineLength = 0.5f;

            float shortest = math.min(rx, ry);

            // Empirical engine-safe minimum:
            float safeMinRadius = m_CurrentRoadWidth * Kappa;

            // Convert real radius into normalized radius relative to the shortest build side.
            float safeMin01 = math.saturate(safeMinRadius / math.max(0.1f, shortest));

            // UI artık 1..10
            float ui = math.clamp(m_BorderRadius, 1f, 10f);

            float cornerX;
            float cornerY;

            float SmoothStep01(float x)
            {
                x = math.saturate(x);
                return x * x * (3f - 2f * x);
            }

            if (ui <= 8f)
            {
                // 1..8 = safe rounded rectangle -> capsule/pill
                //
                // Important:
                // Linear radius growth makes the short straight edges too short too early.
                // The game then stops treating them as proper zoning-generating straight roads.
                // Squared easing keeps the straight sections longer until the shape is close to pill.
                float rawT = math.saturate((ui - 1f) / 7f);
                float t = rawT * rawT;

                float safeRadius = shortest * safeMin01;
                float r = math.lerp(safeRadius, shortest, t);

                cornerX = r;
                cornerY = r;

                // Preserve enough straight edge for zoning until we approach full pill.
                // From UI 7 -> 8 this protection fades out, so 8 can still become a true pill.
                float pillFade = SmoothStep01((ui - 7f) / 1f);
                float minStraightLength = m_CurrentRoadWidth * 1.25f * (1f - pillFade);

                if (minStraightLength > 0.01f)
                {
                    float maxCornerXForStraight = rx - minStraightLength * 0.5f;
                    float maxCornerYForStraight = ry - minStraightLength * 0.5f;

                    // Do not push below the empirical safe radius.
                    if (maxCornerXForStraight > safeRadius)
                        cornerX = math.min(cornerX, maxCornerXForStraight);

                    if (maxCornerYForStraight > safeRadius)
                        cornerY = math.min(cornerY, maxCornerYForStraight);
                }
            }
            else
            {
                // 8..10 = capsule/pill -> ellipse
                float blend = math.saturate((ui - 8f) / 2f);

                cornerX = math.lerp(shortest, rx, blend);
                cornerY = math.lerp(shortest, ry, blend);
            }

            cornerX = math.clamp(cornerX, 0.01f, rx);
            cornerY = math.clamp(cornerY, 0.01f, ry);

            float left = -rx;
            float right = rx;
            float top = ry;
            float bottom = -ry;

            float innerLeft = left + cornerX;
            float innerRight = right - cornerX;
            float innerTop = top - cornerY;
            float innerBottom = bottom + cornerY;

            List<Bezier4x3> curves = new();

            void AddLine(float3 a, float3 d)
            {
                float3 delta = d - a;

                if (math.length(delta) < MinLineLength)
                    return;

                curves.Add(new Bezier4x3(
                    a,
                    a + delta / 3f,
                    a + delta * 2f / 3f,
                    d
                ));
            }

            void AddCorner(float3 a, float3 b, float3 c, float3 d)
            {
                curves.Add(new Bezier4x3(a, b, c, d));
            }

            // TOP
            AddLine(
                new float3(innerRight, elevation, top),
                new float3(innerLeft, elevation, top)
            );

            // TOP LEFT
            AddCorner(
                new float3(innerLeft, elevation, top),
                new float3(innerLeft - cornerX * Kappa, elevation, top),
                new float3(left, elevation, innerTop + cornerY * Kappa),
                new float3(left, elevation, innerTop)
            );

            // LEFT
            AddLine(
                new float3(left, elevation, innerTop),
                new float3(left, elevation, innerBottom)
            );

            // BOTTOM LEFT
            AddCorner(
                new float3(left, elevation, innerBottom),
                new float3(left, elevation, innerBottom - cornerY * Kappa),
                new float3(innerLeft - cornerX * Kappa, elevation, bottom),
                new float3(innerLeft, elevation, bottom)
            );

            // BOTTOM
            AddLine(
                new float3(innerLeft, elevation, bottom),
                new float3(innerRight, elevation, bottom)
            );

            // BOTTOM RIGHT
            AddCorner(
                new float3(innerRight, elevation, bottom),
                new float3(innerRight + cornerX * Kappa, elevation, bottom),
                new float3(right, elevation, innerBottom - cornerY * Kappa),
                new float3(right, elevation, innerBottom)
            );

            // RIGHT
            AddLine(
                new float3(right, elevation, innerBottom),
                new float3(right, elevation, innerTop)
            );

            // TOP RIGHT
            AddCorner(
                new float3(right, elevation, innerTop),
                new float3(right, elevation, innerTop + cornerY * Kappa),
                new float3(innerRight + cornerX * Kappa, elevation, top),
                new float3(innerRight, elevation, top)
            );
            RotateCurveStart(curves, 2);
            ObjectSubNetInfo[] result = new ObjectSubNetInfo[curves.Count];

            for (int i = 0; i < curves.Count; i++)
            {
                result[i] = new ObjectSubNetInfo
                {
                    m_NetPrefab = roadPrefab,
                    m_BezierCurve = curves[i],
                    m_NodeIndex = new int2(i, (i + 1) % curves.Count),
                    m_ParentMesh = new int2(-1, -1)
                };
            }

            return result;
        }
        private void RotateCurveStart(List<Bezier4x3> curves, int startIndex)
        {
            if (curves == null || curves.Count == 0)
                return;

            startIndex %= curves.Count;
            if (startIndex < 0)
                startIndex += curves.Count;

            if (startIndex == 0)
                return;

            List<Bezier4x3> rotated = new();

            for (int i = 0; i < curves.Count; i++)
            {
                rotated.Add(curves[(startIndex + i) % curves.Count]);
            }

            curves.Clear();
            curves.AddRange(rotated);
        }
        #endregion
    }
}