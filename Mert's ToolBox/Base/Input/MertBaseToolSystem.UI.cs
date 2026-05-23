using Game.Prefabs;
using MertsToolBox.Management;

namespace MertsToolBox
{
    public abstract partial class MertBaseToolSystem
    {
        #region Tool Session Variables
        protected float m_CurrentRoadWidth = 8f;
        #endregion

        #region UI State Management
        /// <summary>
        /// Enables or disables the tool state, triggering context captures, activations, or graceful exits accordingly.
        /// </summary>
        public virtual void SetToolState(bool isEnabled)
        {
            if (ToolEnabled == isEnabled) return;

            ToolEnabled = isEnabled;

            if (isEnabled)
            {
                MertToolState.ActiveTool = this;
                DisableVanillaElevation();

                CaptureLaunchRestoreContext();

                NetPrefab currentRoad = TryGetCurrentSelectedRoadPrefab();
                m_CurrentRoadWidth = currentRoad != null ? GetCachedRoadWidth(currentRoad) : 8f;

                ClearUndoHistory();

                OnToolActivated();
                PrimeAndShowPreviewOnEnable();
            }
        }
      
        /// <summary>
        /// Requests the active tool to disable and executes a graceful exit using the specified mode.
        /// </summary>
        public virtual void RequestDisable(ToolExitMode exitMode)
        {
            if (!ToolEnabled) return;

            OnToolDeactivated();
            ExecuteGracefulExit(exitMode);
        }
        #endregion
    }
}