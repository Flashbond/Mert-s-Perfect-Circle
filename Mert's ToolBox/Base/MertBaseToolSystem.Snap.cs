using Game.Tools;
using MertsToolBox.Management;

namespace MertsToolBox
{
    public abstract partial class MertBaseToolSystem
    {
        #region Fields & State
        protected bool m_HasStoredSnapMask;
        protected Snap m_StoredSnapMask;
        #endregion

        #region State Retrieval
        /// <summary>
        /// Gets a value indicating whether geometry snapping is currently enabled.
        /// </summary>
        public bool IsSnapGeometryEnabled() => MertToolState.SnapGeometryEnabled;
        #endregion

        #region Input Queuing & Toggling
        /// <summary>
        /// Queues a toggle action for the specified snap type.
        /// </summary>
        public void QueueSnapToggle() => ToggleSnap();

        /// <summary>
        /// Toggles the specified snap setting and applies the updated mask to the active tool.
        /// </summary>
        public void ToggleSnap()
        {
            MertToolState.SnapGeometryEnabled = !MertToolState.SnapGeometryEnabled;

            if (ToolEnabled)
                QueuePreviewRebuild();
        }
        #endregion

        #region Mask Computation & Application
        /// <summary>
        /// Builds and returns the current combined snap mask based on active settings.
        /// </summary>
        protected virtual Snap GetObjectToolSnapMask()
        {
            return GetGlobalUserSnapMask();
        }
        private Snap GetGlobalUserSnapMask()
        {
            return MertToolState.BuildGlobalSnapMask();
        }
        /// <summary>
        /// Applies the calculated snap mask directly to the active tool system.
        /// </summary>
        private void ApplySnapMaskToActiveTool()
        {
            if (m_ToolSystem?.activeTool == null)
                return;

            Snap targetSnap = GetObjectToolSnapMask();

            if (m_ToolSystem.activeTool == m_ObjectToolSystem)
                m_ObjectToolSystem.selectedSnap = targetSnap;
            else
                m_ToolSystem.activeTool.selectedSnap = targetSnap;
        }
        #endregion
    }
}