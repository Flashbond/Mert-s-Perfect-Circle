using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Input;
using Game.Prefabs;
using Game.Tools;
using Game.UI;
using MertsToolBox.Core;
using MertsToolBox.Management;
using MertsToolBox.Settings;
using MertsToolBox.Systems;
using MertsToolBox.Utilities.Preset;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEngine.Scripting;

namespace MertsToolBox
{
    public partial class MertToolBoxUISystem : UISystemBase
    {
        #region Constants & Fields
        private const string ModId = "MertsToolBox";
        private ToolSystem m_ToolSystem;

        private ShapeToolSystem m_Shape;
        private HelixToolSystem m_Helix;
        private SoftBlockToolSystem m_SoftBlock;
        private GridToolSystem m_Grid;

        private MertMutableBinding<string> m_ToolListBinding;
        private NetPrefab m_LastToolListPrefab;
        private bool m_LastIsToolBoxAllowed;

        private static readonly float[] s_DefaultElevationSteps = new float[] { 10f, 5f, 2.5f, 1.25f };
        private string m_LastActiveToolPipe = "None|None";
        private MertMutableBinding<string> m_PresetListBinding;
        private string m_LastToolListPipe = "";

        private ProxyAction m_OpenShapeToolAction;
        private ProxyAction m_OpenHelixToolAction;
        private ProxyAction m_OpenSoftBlockToolAction;
        private ProxyAction m_OpenGridToolAction;
        private ProxyAction m_UndoToolParameterAction;
        private ProxyAction m_RedoToolParameterAction;
        #endregion

        #region Nested Types
        /// <summary>
        /// A generic polling binding class used to synchronize backend values with the UI automatically.
        /// </summary>
        public class MertPolledBinding<T> : ValueBinding<T>, IUpdateBinding
        {
            private readonly Func<T> m_Getter;

            public MertPolledBinding(string group, string name, Func<T> getter, T initialValue = default)
                : base(group, name, initialValue)
            {
                m_Getter = getter;
            }
            public bool Update()
            {
                base.Update(m_Getter());
                return true;
            }
        }
        public class MertMutableBinding<T> : ValueBinding<T>
        {
            public MertMutableBinding(string group, string name, T initialValue = default)
                : base(group, name, initialValue)
            {
            }

            public void SetValue(T value)
            {
                base.Update(value);
            }
        }
        private void RefreshToolListBinding()
        {
            if (m_ToolListBinding == null)
                return;

            m_ToolListBinding.Update(GetToolListPipe());
        }
        #endregion

        #region Lifecycle Methods
        /// <summary>
        /// Initializes system references, sets up event listeners, and registers UI bindings upon creation.
        /// </summary>
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();

            m_Shape = World.GetOrCreateSystemManaged<ShapeToolSystem>();
            m_Helix = World.GetOrCreateSystemManaged<HelixToolSystem>();
            m_SoftBlock = World.GetOrCreateSystemManaged<SoftBlockToolSystem>();
            m_Grid = World.GetOrCreateSystemManaged<GridToolSystem>();

            if (m_ToolSystem != null)
                m_ToolSystem.EventToolChanged += OnToolChanged;

            RegisterBindings();
            MertToolState.OnToolAbortedByUI += HandleToolAbortedByUi;

            m_OpenShapeToolAction = Mod.settings?.GetAction(ToolBoxSettings.OpenShapeTool);
            m_OpenHelixToolAction = Mod.settings?.GetAction(ToolBoxSettings.OpenHelixTool);
            m_OpenSoftBlockToolAction = Mod.settings?.GetAction(ToolBoxSettings.OpenSoftBlockTool);
            m_OpenGridToolAction = Mod.settings?.GetAction(ToolBoxSettings.OpenGridTool);
            m_UndoToolParameterAction = Mod.settings?.GetAction(ToolBoxSettings.UndoToolParameter);
            m_RedoToolParameterAction = Mod.settings?.GetAction(ToolBoxSettings.RedoToolParameter);
            EnableAction(m_OpenShapeToolAction);
            EnableAction(m_OpenHelixToolAction);
            EnableAction(m_OpenSoftBlockToolAction);
            EnableAction(m_OpenGridToolAction);
            EnableAction(m_UndoToolParameterAction);
            EnableAction(m_RedoToolParameterAction);

            RefreshToolListBinding();
            
        }

        /// <summary>
        /// Continuously updates the global UI state and synchronizes individual tool bindings every frame.
        /// </summary>
        protected override void OnUpdate()
        {
            base.OnUpdate();

            MertToolState.HasReleasedStaleObjectToolThisFrame = false;

            TryReleaseStaleStampAfterReload();
            TryProcessPendingRestore();

            ProcessHotkeys();
        }

        /// <summary>
        /// Cleans up event listeners and system references to prevent memory leaks upon destruction.
        /// </summary>
        [Preserve]
        protected override void OnDestroy()
        {
            if (m_ToolSystem != null)
            {
                m_ToolSystem.EventToolChanged -= OnToolChanged;
            }

            MertToolState.OnToolAbortedByUI -= HandleToolAbortedByUi;
            base.OnDestroy();
        }
        #endregion

        #region UI Bindings Registration
        private void ProcessHotkeys()
        {
            if (MertToolState.SuppressUiAbortDuringRestore)
                return;

            MertBaseToolSystem activeTool = GetActiveCustomTool();

            if (activeTool != null)
            {
                if (m_UndoToolParameterAction != null && m_UndoToolParameterAction.WasPressedThisFrame())
                    activeTool.UndoToolParameter();

                if (m_RedoToolParameterAction != null && m_RedoToolParameterAction.WasPressedThisFrame())
                    activeTool.RedoToolParameter();

                return;
            }

            if (m_OpenShapeToolAction != null && m_OpenShapeToolAction.WasPressedThisFrame())
                OpenToolFromHotkey(m_Shape?.ToolId);

            if (m_OpenHelixToolAction != null && m_OpenHelixToolAction.WasPressedThisFrame())
                OpenToolFromHotkey(m_Helix?.ToolId);

            if (m_OpenSoftBlockToolAction != null && m_OpenSoftBlockToolAction.WasPressedThisFrame())
                OpenToolFromHotkey(m_SoftBlock?.ToolId);

            if (m_OpenGridToolAction != null && m_OpenGridToolAction.WasPressedThisFrame())
                OpenToolFromHotkey(m_Grid?.ToolId);
        }
        private static void EnableAction(ProxyAction action)
        {
            if (action != null)
                action.shouldBeEnabled = true;
        }
        /// <summary>
        /// Registers all initial ValueBindings and TriggerBindings connecting the C# backend to the Cohtml/TSX frontend.
        /// </summary>
        private void RegisterBindings()
        {
            m_ToolListBinding = new MertMutableBinding<string>(
                ModId,
                "ToolList",
                GetToolListPipe()
            );

            AddBinding(m_ToolListBinding);

            AddUpdateBinding(new MertPolledBinding<string>( ModId, "ActiveTool", GetActiveToolPipe, "None|None"));

            AddUpdateBinding(new MertPolledBinding<bool>( ModId, "IsToolBoxAllowed", GetIsToolBoxAllowed, false));

            // Shape Bindings
            AddUpdateBinding(new MertPolledBinding<float>(ModId, "ShapeDimension",
                () => m_Shape?.GetCurrentDimension() ?? 96));
            AddUpdateBinding(new MertPolledBinding<int>(ModId, "ShapeDimensionStepValue",
                () => m_Shape?.GetDimensionStepSize() ?? 8));
            AddUpdateBinding(new MertPolledBinding<string>(ModId, "ShapeShapeName",
                () => m_Shape != null ? m_Shape.GetShapeName(m_Shape.GetCurrentSides()) : "Circle"));
            AddBinding(new ValueBinding<int[]>(
                ModId,
                "ShapeDimensionStepArray",
                m_Shape?.m_DimensionSteps ?? Array.Empty<int>(),
                new ArrayWriter<int>()
            ));
            AddUpdateBinding(new MertPolledBinding<int>(ModId, "ShapeSides",
                () => m_Shape?.GetCurrentSides() ?? 0));
            AddBinding(new ValueBinding<int[]>(
                ModId,
                "ShapeAllowedSidesArray",
                m_Shape?.m_AllowedSides ?? Array.Empty<int>(),
                new ArrayWriter<int>()
            ));

            // Helix Bindings
            AddUpdateBinding(new MertPolledBinding<float>(ModId, "HelixDiameter",
                () => m_Helix?.GetCurrentDiameter() ?? 96));
            AddUpdateBinding(new MertPolledBinding<int>(ModId, "HelixDiameterStepValue",
                () => m_Helix?.GetDiameterStepSize() ?? 8));
            AddBinding(new ValueBinding<int[]>(
                ModId,
                "HelixDiameterStepArray",
                m_Helix?.m_DiameterSteps,
                new ArrayWriter<int>()
            ));
            AddUpdateBinding(new MertPolledBinding<float>(ModId, "HelixTurns",
                () => m_Helix?.GetCurrentTurns() ?? 3f));
            AddUpdateBinding(new MertPolledBinding<float>(ModId, "HelixTurnStepValue",
                () => m_Helix?.GetTurnStepSize() ?? 2));
            AddBinding(new ValueBinding<float[]>(
                ModId,
                "HelixTurnStepArray",
                m_Helix?.m_TurnSteps,
                new ArrayWriter<float>()
            ));
            AddUpdateBinding(new MertPolledBinding<float>(ModId, "HelixClearance",
                () => m_Helix?.GetCurrentClearance() ?? 8f));
            AddUpdateBinding(new MertPolledBinding<float>(ModId, "HelixClearanceStepValue",
                () => m_Helix?.GetClearanceStepSize() ?? 2));
            AddBinding(new ValueBinding<float[]>(
                ModId,
                "HelixClearanceStepArray",
                m_Helix?.m_ClearanceSteps,
                new ArrayWriter<float>()
            ));
            AddUpdateBinding(new MertPolledBinding<bool>(ModId, "HelixIsClockwise",
                () => m_Helix?.GetIsClockwise() ?? true));

            // SoftBlock Bindings
            AddUpdateBinding(new MertPolledBinding<float>(ModId, "SoftBlockWidth",
                () => m_SoftBlock?.GetCurrentWidth() ?? 96));
            AddUpdateBinding(new MertPolledBinding<int>(ModId, "SoftBlockWidthStepValue",
                () => m_SoftBlock?.GetWidthStepSize() ?? 8));
            AddBinding(new ValueBinding<int[]>(
                ModId,
                "SoftBlockWidthStepArray",
                m_SoftBlock?.m_WidthSteps,
                new ArrayWriter<int>()
            ));
            AddUpdateBinding(new MertPolledBinding<float>(ModId, "SoftBlockLength",
                () => m_SoftBlock?.GetCurrentLength() ?? 192));
            AddUpdateBinding(new MertPolledBinding<int>(ModId, "SoftBlockLengthStepValue",
                () => m_SoftBlock?.GetLengthStepSize() ?? 8));
            AddBinding(new ValueBinding<int[]>(
                ModId,
                "SoftBlockLengthStepArray",
                m_SoftBlock?.m_LengthSteps,
                new ArrayWriter<int>()
            ));
            AddUpdateBinding(new MertPolledBinding<float>(ModId, "GetBorderRadius",
                         () => m_SoftBlock?.GetCurrentBorderRadius() ?? 5f));
            AddUpdateBinding(new MertPolledBinding<bool>(ModId, "SoftBlockStraightCorners",
                () => m_SoftBlock != null && m_SoftBlock.GetUseStraightCorners()));
            AddUpdateBinding(new MertPolledBinding<bool>(ModId, "SoftBlockStraightCornersSupported",
                () => m_SoftBlock != null && m_SoftBlock.IsStraightCornersSupported()));

            // Grid Bindings
            AddUpdateBinding(new MertPolledBinding<int>(ModId, "GridBlockWidth",
                () => m_Grid?.GetCurrentBlockWidthU() ?? 12));
            AddUpdateBinding(new MertPolledBinding<int>(ModId, "GridBlockLength",
                () => m_Grid?.GetCurrentBlockLengthU() ?? 12));
            AddUpdateBinding(new MertPolledBinding<int>(ModId, "GridColumns",
                () => m_Grid?.GetCurrentColumns() ?? 2));
            AddUpdateBinding(new MertPolledBinding<int>(ModId, "GridRows",
                () => m_Grid?.GetCurrentRows() ?? 2));
            AddUpdateBinding(new MertPolledBinding<bool>(ModId, "GridAlternating",
                () => m_Grid?.GetIsAlternating() ?? false));
            AddUpdateBinding(new MertPolledBinding<bool>(ModId, "GridOrientationLeftBottom",
                () => m_Grid?.GetIsOrientationLeftBottom() ?? false));
            AddUpdateBinding(new MertPolledBinding<bool>(ModId, "GridIsOneWaySupported",
                () => m_Grid?.IsCurrentPrefabValidForOneWayPattern() ?? false));

            // Elevation Bindings
            AddUpdateBinding(new MertPolledBinding<float>(ModId, "ElevationStepValue", () =>
            {
                if (m_Shape != null && m_Shape.ToolEnabled) return m_Shape.GetElevationStepValue();
                if (m_Helix != null && m_Helix.ToolEnabled) return m_Helix.GetElevationStepValue();
                if (m_SoftBlock != null && m_SoftBlock.ToolEnabled) return m_SoftBlock.GetElevationStepValue();
                if (m_Grid != null && m_Grid.ToolEnabled) return m_Grid.GetElevationStepValue();

                return 10f;
            }));
            AddBinding(new ValueBinding<float[]>(
                ModId,
                "ElevationStepArray",
                GetCurrentElevationSteps(),
                new ArrayWriter<float>()
            ));
            AddUpdateBinding(new MertPolledBinding<float>(ModId, "ElevationValue", () =>
            {
                if (m_Shape != null && m_Shape.ToolEnabled) return m_Shape.GetCurrentNetToolElevation();
                if (m_Helix != null && m_Helix.ToolEnabled) return m_Helix.GetCurrentNetToolElevation();
                if (m_SoftBlock != null && m_SoftBlock.ToolEnabled) return m_SoftBlock.GetCurrentNetToolElevation();
                if (m_Grid != null && m_Grid.ToolEnabled) return m_Grid.GetCurrentNetToolElevation();

                return 0f;
            }));

            // Shared Snap & Toggle Bindings
            AddUpdateBinding(new MertPolledBinding<bool>(ModId, "ShowSnapRow", () =>
            {
                if (m_Grid != null && m_Grid.ToolEnabled)
                    return Mod.settings != null && Mod.settings.EnableGridSnap;
                return true;
            }));

            AddUpdateBinding(new MertPolledBinding<bool>(ModId, "IsSnapGeometryActive", () =>
            {
                if (m_Shape != null && m_Shape.ToolEnabled) return m_Shape.IsSnapGeometryEnabled();
                if (m_SoftBlock != null && m_SoftBlock.ToolEnabled) return m_SoftBlock.IsSnapGeometryEnabled();
                if (m_Grid != null && m_Grid.ToolEnabled) return m_Grid.IsSnapGeometryEnabled();
                return false;
            }));
            AddUpdateBinding(new MertPolledBinding<bool>(ModId, "IsSnapNetSideActive", () =>
            {
                if (m_Shape != null && m_Shape.ToolEnabled) return m_Shape.IsSnapNetSideEnabled();
                if (m_SoftBlock != null && m_SoftBlock.ToolEnabled) return m_SoftBlock.IsSnapNetSideEnabled();
                if (m_Grid != null && m_Grid.ToolEnabled) return m_Grid.IsSnapNetSideEnabled();
                return false;
            }));
            AddUpdateBinding(new MertPolledBinding<bool>(ModId, "IsSnapNetAreaActive", () =>
            {
                if (m_Shape != null && m_Shape.ToolEnabled) return m_Shape.IsSnapNetAreaEnabled();
                if (m_SoftBlock != null && m_SoftBlock.ToolEnabled) return m_SoftBlock.IsSnapNetAreaEnabled();
                if (m_Grid != null && m_Grid.ToolEnabled) return m_Grid.IsSnapNetAreaEnabled();
                return false;
            }));

            // Action Hints
            AddUpdateBinding(new MertPolledBinding<bool>(ModId, "ShowShapeCtrlWheelHint",
                () => Mod.settings != null && Mod.settings.UseCtrlWheelForShapeDimensionAdjustment));
            AddUpdateBinding(new MertPolledBinding<bool>(ModId, "ShowHelixCtrlWheelHint",
                () => Mod.settings != null && Mod.settings.UseCtrlWheelForHelixTurnAdjustment));
            AddUpdateBinding(new MertPolledBinding<bool>(ModId, "ShowSoftBlockCtrlWheelHint",
                () => Mod.settings != null && Mod.settings.UseCtrlWheelForSoftBlockBorderRadius));
            AddUpdateBinding(new MertPolledBinding<string>(ModId, "ActionStatusText",
                () => GetActionStatusText(), ""));

            AddUpdateBinding(new MertPolledBinding<string>(ModId, "PresetList", () =>
            {
                MertBaseToolSystem tool = GetActiveCustomTool();
                if (tool == null)
                    return "";

                NetPrefab prefab = tool.GetCurrentSelectedNetPrefabForUi();
                if (prefab == null)
                    return "";

                List<MertToolPreset> presets =
                    MertToolPresetStorage.LoadPresets(tool.ToolId, prefab.name);

                return string.Join(";", presets.Select(p => p.DisplayName));
            }, ""));

            // Preset Bindings
            m_PresetListBinding = new MertMutableBinding<string>(ModId, "PresetList", "");
            AddBinding(m_PresetListBinding);

            // Global Triggers
            AddBinding(new TriggerBinding<string>(ModId, "ToggleTool", (toolId) =>
            {
                MertToolState.ClearToolbarNavigation();
                MertToolState.ClearPendingRestore();

                CloseTools(ToolExitMode.UserSelectionClose);

                if (toolId == m_Shape?.ToolId)
                    m_Shape.SetToolState(true);
                else if (toolId == m_Helix?.ToolId)
                    m_Helix.SetToolState(true);
                else if (toolId == m_SoftBlock?.ToolId)
                    m_SoftBlock.SetToolState(true);
                else if (toolId == m_Grid?.ToolId)
                    m_Grid.SetToolState(true);
            }));

            AddBinding(new TriggerBinding<float>(ModId, "ElevationStep", (val) =>
            {
                if (m_Shape != null && m_Shape.ToolEnabled) m_Shape.SetElevationStepFromUi(val);
                else if (m_Helix != null && m_Helix.ToolEnabled) m_Helix.SetElevationStepFromUi(val);
                else if (m_SoftBlock != null && m_SoftBlock.ToolEnabled) m_SoftBlock.SetElevationStepFromUi(val);
                else if (m_Grid != null && m_Grid.ToolEnabled) m_Grid.SetElevationStepFromUi(val);
            }));

            AddBinding(new TriggerBinding(ModId, "ElevationUp", () =>
            {
                if (m_Shape != null && m_Shape.ToolEnabled) m_Shape.QueueElevationChangeFromUi(+1);
                else if (m_Helix != null && m_Helix.ToolEnabled) m_Helix.QueueElevationChangeFromUi(+1);
                else if (m_SoftBlock != null && m_SoftBlock.ToolEnabled) m_SoftBlock.QueueElevationChangeFromUi(+1);
                else if (m_Grid != null && m_Grid.ToolEnabled) m_Grid.QueueElevationChangeFromUi(+1);
            }));

            AddBinding(new TriggerBinding(ModId, "ElevationDown", () =>
            {
                if (m_Shape != null && m_Shape.ToolEnabled) m_Shape.QueueElevationChangeFromUi(-1);
                else if (m_Helix != null && m_Helix.ToolEnabled) m_Helix.QueueElevationChangeFromUi(-1);
                else if (m_SoftBlock != null && m_SoftBlock.ToolEnabled) m_SoftBlock.QueueElevationChangeFromUi(-1);
                else if (m_Grid != null && m_Grid.ToolEnabled) m_Grid.QueueElevationChangeFromUi(-1);
            }));

            // Shape Triggers
            AddBinding(new TriggerBinding(ModId, "ShapeDimensionUp", () => m_Shape?.QueueDimensionChange(+1)));
            AddBinding(new TriggerBinding(ModId, "ShapeDimensionDown", () => m_Shape?.QueueDimensionChange(-1)));
            AddBinding(new TriggerBinding<int>(ModId, "ShapeDimensionStep", (val) => m_Shape?.QueueSetDimensionStep(val)));
            AddBinding(new TriggerBinding(ModId, "ShapeSidesUp", () => m_Shape?.QueueSidesChange(+1)));
            AddBinding(new TriggerBinding(ModId, "ShapeSidesDown", () => m_Shape?.QueueSidesChange(-1)));
            AddBinding(new TriggerBinding<string>(ModId, "ToggleShapeSnap", (snapType) => m_Shape?.QueueSnapToggle(snapType)));

            // Helix Triggers
            AddBinding(new TriggerBinding(ModId, "HelixDiameterUp", () => m_Helix?.QueueDiameterChange(+1)));
            AddBinding(new TriggerBinding(ModId, "HelixDiameterDown", () => m_Helix?.QueueDiameterChange(-1)));
            AddBinding(new TriggerBinding<int>(ModId, "HelixDiameterStep", (val) => m_Helix?.QueueSetDiameterStep(val)));
            AddBinding(new TriggerBinding(ModId, "HelixTurnsUp", () => m_Helix?.QueueTurnChange(+1)));
            AddBinding(new TriggerBinding(ModId, "HelixTurnsDown", () => m_Helix?.QueueTurnChange(-1)));
            AddBinding(new TriggerBinding<float>(ModId, "HelixTurnStep", (val) => m_Helix?.QueueSetTurnStep(val)));
            AddBinding(new TriggerBinding(ModId, "HelixClearanceUp", () => m_Helix?.QueueClearanceChange(+1)));
            AddBinding(new TriggerBinding(ModId, "HelixClearanceDown", () => m_Helix?.QueueClearanceChange(-1)));
            AddBinding(new TriggerBinding<float>(ModId, "HelixClearanceStep", (val) => m_Helix?.QueueSetClearanceStep(val)));
            AddBinding(new TriggerBinding(ModId, "HelixToggleDirection", () => m_Helix?.QueueToggleDirection()));

            // SoftBlock Triggers
            AddBinding(new TriggerBinding(ModId, "SoftBlockWidthUp", () => m_SoftBlock?.QueueWidthChange(+1)));
            AddBinding(new TriggerBinding(ModId, "SoftBlockWidthDown", () => m_SoftBlock?.QueueWidthChange(-1)));
            AddBinding(new TriggerBinding<int>(ModId, "SoftBlockWidthStep", (val) => m_SoftBlock?.QueueSetWidthStep(val)));
            AddBinding(new TriggerBinding(ModId, "SoftBlockLengthUp", () => m_SoftBlock?.QueueLengthChange(+1)));
            AddBinding(new TriggerBinding(ModId, "SoftBlockLengthDown", () => m_SoftBlock?.QueueLengthChange(-1)));
            AddBinding(new TriggerBinding<int>(ModId, "SoftBlockLengthStep", (val) => m_SoftBlock?.QueueSetLengthStep(val)));
            AddBinding(new TriggerBinding<float>(ModId, "SetBorderRadius", (value) => m_SoftBlock?.SetBorderRadiusFromUi(value)));
            AddBinding(new TriggerBinding(ModId, "BeginBorderRadiusDrag", () => m_SoftBlock?.BeginBorderRadiusDrag()));
            AddBinding(new TriggerBinding(ModId, "EndBorderRadiusDrag", () => m_SoftBlock?.EndBorderRadiusDrag()));
            AddBinding(new TriggerBinding<string>(ModId, "SoftBlockToggleSnap", (snapType) => m_SoftBlock?.QueueSnapToggle(snapType)));
            AddBinding(new TriggerBinding(ModId, "SoftBlockToggleStraightCorners", () => m_SoftBlock?.QueueToggleStraightCorners()));

            // Grid Triggers
            AddBinding(new TriggerBinding(ModId, "GridBlockWidthUp", () => m_Grid?.QueueBlockWidthChange(+1)));
            AddBinding(new TriggerBinding(ModId, "GridBlockWidthDown", () => m_Grid?.QueueBlockWidthChange(-1)));
            AddBinding(new TriggerBinding(ModId, "GridBlockLengthUp", () => m_Grid?.QueueBlockLengthChange(+1)));
            AddBinding(new TriggerBinding(ModId, "GridBlockLengthDown", () => m_Grid?.QueueBlockLengthChange(-1)));
            AddBinding(new TriggerBinding(ModId, "GridColumnsUp", () => m_Grid?.QueueColsChange(+1)));
            AddBinding(new TriggerBinding(ModId, "GridColumnsDown", () => m_Grid?.QueueColsChange(-1)));
            AddBinding(new TriggerBinding(ModId, "GridRowsUp", () => m_Grid?.QueueRowsChange(+1)));
            AddBinding(new TriggerBinding(ModId, "GridRowsDown", () => m_Grid?.QueueRowsChange(-1)));
            AddBinding(new TriggerBinding(ModId, "GridToggleAlternating", () => m_Grid?.QueueToggleAlternating()));
            AddBinding(new TriggerBinding(ModId, "GridToggleOrientation", () => m_Grid?.QueueToggleOrientation()));
            AddBinding(new TriggerBinding<string>(ModId, "GridToggleSnap", (snapType) => m_Grid?.QueueSnapToggle(snapType)));

            // Preset Triggers
            AddBinding(new CallBinding<string, bool>(ModId, "SavePreset", (toolId) =>
            {
                try
                {
                    MertBaseToolSystem tool = GetToolById(toolId);
                    MertToolPreset preset = tool?.CreatePresetSnapshot();

                    if (preset != null)
                    {
                        return MertToolPresetStorage.SavePreset(preset);
                    }

                    return false;
                }
                catch (Exception e)
                {
                    ModRuntime.Warn($"[CallBinding] Save preset binding error: {e.Message}");
                    return false;
                }
            }));

            AddBinding(new TriggerBinding<string>(ModId, "LoadPreset", (presetDisplayName) =>
            {
                MertBaseToolSystem tool = GetActiveCustomTool();
                if (tool == null)
                    return;

                NetPrefab prefab = tool.GetCurrentSelectedNetPrefabForUi();
                if (prefab == null)
                    return;

                List<MertToolPreset> presets =
                    MertToolPresetStorage.LoadPresets(tool.ToolId, prefab.name);

                MertToolPreset preset = presets.FirstOrDefault(p => p.DisplayName == presetDisplayName);
                if (preset == null)
                    return;

                tool.ApplyPresetSnapshot(preset);
            }));
            AddBinding(new TriggerBinding(ModId, "RefreshPresetList", () => RefreshPresetListBinding()));
            AddBinding(new TriggerBinding<string>(ModId, "DeletePreset", (presetDisplayName) =>
            {
                MertBaseToolSystem tool = GetActiveCustomTool();
                if (tool == null) return;

                NetPrefab prefab = tool.GetCurrentSelectedNetPrefabForUi();
                if (prefab == null) return;

                bool deleted = MertToolPresetStorage.DeletePreset(
                    tool.ToolId,
                    prefab.name,
                    presetDisplayName
                );

                if (deleted)
                    RefreshPresetListBinding();
            }));
        }
        #endregion

        #region Tool Event Handling
        private float[] GetCurrentElevationSteps()
        {
            if (m_Shape != null && m_Shape.ToolEnabled) return m_Shape.GetElevationStepArray();
            if (m_SoftBlock != null && m_SoftBlock.ToolEnabled) return m_SoftBlock.GetElevationStepArray();
            if (m_Grid != null && m_Grid.ToolEnabled) return m_Grid.GetElevationStepArray();

            return s_DefaultElevationSteps;
        }
        /// <summary>
        /// Handles forceful tool abort requests originating from the UI system.
        /// </summary>
        private void HandleToolAbortedByUi(ToolExitMode exitMode)
        {
            if (MertToolState.SuppressUiAbortDuringRestore) return;
            CloseTools(exitMode);
        }

        /// <summary>
        /// Disables all custom tools securely based on the provided exit mode.
        /// </summary>
        private void CloseTools(ToolExitMode exitMode)
        {
            m_Shape?.RequestDisable(exitMode);
            m_Helix?.RequestDisable(exitMode);
            m_SoftBlock?.RequestDisable(exitMode);
            m_Grid?.RequestDisable(exitMode);
        }

        /// <summary>
        /// Listens to global tool changes and safely closes custom tools if standard game tools take over.
        /// </summary>
        private void OnToolChanged(ToolBaseSystem tool)
        {
            if (MertToolState.SuppressToolChangedDuringColdstart)
                return;

            if (MertToolState.SuppressUiAbortDuringRestore)
                return;

            var objectTool = World.GetOrCreateSystemManaged<ObjectToolSystem>();
            var netTool = World.GetOrCreateSystemManaged<NetToolSystem>();
            var defaultTool = World.GetOrCreateSystemManaged<DefaultToolSystem>();

            bool isRoadBuilderTool = IsRoadBuilderTool(tool);

            if (tool == defaultTool &&
                MertToolState.ToolbarNavigationInProgress)
            {
                MertToolState.ClearPendingRestore();
                MertToolState.ClearToolbarNavigation();
                return;
            }

            if (MertToolState.ToolbarNavigationInProgress)
                return;

            if (isRoadBuilderTool)
            {
                CloseTools(ToolExitMode.UserSelectionClose);
                return;
            }

            if (tool == defaultTool)
            {
                CloseTools(ToolExitMode.RestoreFromEscape);
                return;
            }

            if (tool == netTool)
                return;

            if (tool != objectTool && tool != netTool)
            {
                if (TryClassifyExternalToolChange(out ToolExitMode externalExitMode))
                {
                    CloseTools(externalExitMode);
                    return;
                }

                CloseTools(ToolExitMode.UserSelectionClose);
            }
        }
        private MertBaseToolSystem GetToolById(string toolId)
        {
            if (m_Shape != null && m_Shape.ToolId == toolId) return m_Shape;
            if (m_Helix != null && m_Helix.ToolId == toolId) return m_Helix;
            if (m_SoftBlock != null && m_SoftBlock.ToolId == toolId) return m_SoftBlock;
            if (m_Grid != null && m_Grid.ToolId == toolId) return m_Grid;
            return null;
        }
        private MertBaseToolSystem GetActiveCustomTool()
        {
            if (m_Shape != null && m_Shape.ToolEnabled) return m_Shape;
            if (m_Helix != null && m_Helix.ToolEnabled) return m_Helix;
            if (m_SoftBlock != null && m_SoftBlock.ToolEnabled) return m_SoftBlock;
            if (m_Grid != null && m_Grid.ToolEnabled) return m_Grid;

            return null;
        }
        private void OpenToolFromHotkey(string toolId)
        {
            if (string.IsNullOrEmpty(toolId))
                return;

            MertBaseToolSystem target = GetToolById(toolId);
            if (target == null)
                return;

            if (target.ToolEnabled)
                return;

            if (!target.IsCurrentPrefabValid())
                return;

            MertToolState.UserJustChangedAssetCategory = false;

            target.SetToolState(true);
        }
        #endregion

        #region External Selection Normalization
        /// <summary>
        /// Determines whether an asset is currently selected through the standard vanilla toolbar UI.
        /// </summary>
        private bool HasVanillaSelectedAsset()
        {
            try
            {
                var toolbarSystem = World.GetExistingSystemManaged<Game.UI.InGame.ToolbarUISystem>();
                if (toolbarSystem == null)
                    return false;

                var bindingField = typeof(Game.UI.InGame.ToolbarUISystem)
                    .GetField("m_SelectedAssetBinding", BindingFlags.Instance | BindingFlags.NonPublic);

                if (bindingField?.GetValue(toolbarSystem) is not object bindingObj)
                    return false;

                var valueProperty = bindingObj.GetType().GetProperty("value");
                if (valueProperty == null)
                    return false;

                Entity selectedAssetEntity = (Entity)valueProperty.GetValue(bindingObj);
                return selectedAssetEntity != Entity.Null;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Evaluates external prefab selections to determine the appropriate tool exit mode based on category synchronization.
        /// </summary>
        private bool TryClassifyExternalToolChange(out ToolExitMode exitMode)
        {
            exitMode = ToolExitMode.UserSelectionClose;

            if (HasVanillaSelectedAsset())
                return false;

            var netTool = World.GetExistingSystemManaged<NetToolSystem>();
            var prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();

            if (netTool == null || prefabSystem == null)
                return false;

            if (netTool.GetPrefab() is not NetPrefab currentRoad || currentRoad == null)
                return false;

            Entity roadEntity = prefabSystem.GetEntity(currentRoad);
            if (roadEntity == Entity.Null || !World.EntityManager.Exists(roadEntity))
                return false;

            if (!MertToolbarHandoffMemory.IsSupportedNetPrefab(roadEntity, out var resolvedRoad) || resolvedRoad == null)
                return false;

            Entity newCategory = Entity.Null;
            if (World.EntityManager.TryGetComponent(roadEntity, out UIObjectData uiObject) &&
                uiObject.m_Group != Entity.Null)
            {
                newCategory = uiObject.m_Group;
            }

            Entity baselineCategory = MertToolState.LastResolvedCategory != Entity.Null
                ? MertToolState.LastResolvedCategory
                : MertToolState.LaunchCategory;

            bool sameCategory =
                newCategory != Entity.Null &&
                baselineCategory != Entity.Null &&
                newCategory == baselineCategory;

            exitMode = sameCategory
                ? ToolExitMode.UserSelectionClose
                : ToolExitMode.SilentCategoryClose;

            return true;
        }

        /// <summary>
        /// Checks whether the specified tool system corresponds to the external Road Builder mod.
        /// </summary>
        public static bool IsRoadBuilderTool(ToolBaseSystem tool)
        {
            if (tool == null)
                return false;

            string typeName = tool.GetType().Name;
            return string.Equals(typeName, "RoadBuilderToolSystem", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Stale Stamp Recovery
        /// <summary>
        /// Recovers the tool state by forcing a silent exit if the active object tool is stuck holding a destroyed or invalid stamp entity.
        /// </summary>
        private void TryReleaseStaleStampAfterReload()
        {
            if (m_ToolSystem == null)
                return;

            if (MertToolState.HasReleasedStaleObjectToolThisFrame)
                return;

            if (MertToolState.PendingRestore)
                return;

            var objectTool = World.GetExistingSystemManaged<ObjectToolSystem>();
            if (objectTool == null)
                return;

            if (m_ToolSystem.activeTool != objectTool)
                return;

            PrefabBase currentPrefab = null;
            try
            {
                currentPrefab = objectTool.GetPrefab();
            }
            catch
            {
                return;
            }

            if (currentPrefab == null)
                return;

            if (!MertToolbarHandoffMemory.IsCurrentStamp(currentPrefab))
                return;

            var prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
            if (prefabSystem == null)
                return;

            Entity stampEntity = prefabSystem.GetEntity(currentPrefab);
            bool entityAlive = stampEntity != Entity.Null && World.EntityManager.Exists(stampEntity);

            if (!entityAlive)
            {
                MertToolState.HasReleasedStaleObjectToolThisFrame = true;
                ModRuntime.Log("TryReleaseStaleStampAfterReload | stale stamp detected -> SilentTabClose");
                CloseTools(ToolExitMode.SilentCategoryClose);
            }
        }

        #endregion

        #region Handoff & Restore
        /// <summary>
        /// Processes any pending restore operations to re-establish previous tool states and selections.
        /// </summary>
        private void TryProcessPendingRestore()
        {
            if (!MertToolState.PendingRestore)
                return;

            if (MertToolState.ToolbarNavigationInProgress ||
                MertToolState.UserJustChangedAssetMenu ||
                MertToolState.UserJustChangedAssetCategory)
            {
                MertToolState.ClearPendingRestore();
                return;
            }

            if (MertToolState.PendingRestoreMode == ToolExitMode.None)
            {
                MertToolState.ClearPendingRestore();
                return;
            }

            if (MertToolState.PendingRestoreMode == ToolExitMode.RestoreFromPlacement)
            {
                MertToolState.ClearPendingRestore();
                return;
            }

            if (m_ToolSystem == null)
                return;

            NetPrefab road = MertToolState.PendingRestoreRoad;
            Entity category = MertToolState.PendingRestoreCategory;

            var toolbarUISystem = World.GetExistingSystemManaged<Game.UI.InGame.ToolbarUISystem>();
            var prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
            var netTool = World.GetOrCreateSystemManaged<NetToolSystem>();

            if (toolbarUISystem == null || prefabSystem == null || netTool == null)
                return;

            MertToolState.SuppressUiAbortDuringRestore = true;
            MertToolState.SuppressLiveUiCapture = true;
            MertToolState.SuppressUiMemoryCapture = true;
            MertToolState.SuppressCategoryCapture = true;

            try
            {
                var flags = System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Public;

                var selectCategory = toolbarUISystem.GetType()
                    .GetMethod("SelectAssetCategory", flags, null, new Type[] { typeof(Entity) }, null);

                var selectAsset = toolbarUISystem.GetType()
                    .GetMethod("SelectAsset", flags, null, new Type[] { typeof(Entity), typeof(bool) }, null);

                bool categoryLooksValid =
                    category != Entity.Null &&
                    World != null &&
                    World.EntityManager.Exists(category) &&
                    World.EntityManager.HasComponent<UIAssetCategoryData>(category);

                if (!categoryLooksValid && road != null)
                {
                    Entity roadEntity = prefabSystem.GetEntity(road);
                    if (roadEntity != Entity.Null &&
                        World.EntityManager.Exists(roadEntity) &&
                        World.EntityManager.TryGetComponent(roadEntity, out UIObjectData uiObject) &&
                        uiObject.m_Group != Entity.Null &&
                        World.EntityManager.Exists(uiObject.m_Group) &&
                        World.EntityManager.HasComponent<UIAssetCategoryData>(uiObject.m_Group))
                    {
                        category = uiObject.m_Group;
                        categoryLooksValid = true;
                    }
                }

                if (categoryLooksValid)
                {
                    try
                    {
                        selectCategory?.Invoke(toolbarUISystem, new object[] { category });
                    }
                    catch (TargetInvocationException tie)
                    {
                        ModRuntime.Warn(
                            $"Restore category failed: {tie.InnerException?.Message ?? tie.Message}");
                    }
                    catch (Exception e)
                    {
                        ModRuntime.Warn(
                            $"Restore category failed: {e.Message}");
                    }
                }

                if (road != null)
                {
                    Entity roadEntity = prefabSystem.GetEntity(road);
                    if (roadEntity != Entity.Null && World.EntityManager.Exists(roadEntity))
                    {
                        try
                        {
                            selectAsset?.Invoke(toolbarUISystem, new object[] { roadEntity, true });
                        }
                        catch (TargetInvocationException tie)
                        {
                            ModRuntime.Warn(
                                $"Restore asset failed: {tie.InnerException?.Message ?? tie.Message}");
                        }
                        catch (Exception e)
                        {
                            ModRuntime.Warn($"Restore asset failed: {e.Message}");
                        }
                    }
                }

                m_ToolSystem.activeTool = netTool;
                string activeAfter =
                    m_ToolSystem?.activeTool == null ? "NULL" :
                    m_ToolSystem.activeTool.GetType().Name;

                if (road != null)
                {
                    try
                    {
                        m_ToolSystem.ActivatePrefabTool(road);
                    }
                    catch (Exception e)
                    {
                        ModRuntime.Warn($"ActivatePrefabTool failed during restore: {e.Message}");
                    }
                }
            }
            finally
            {
                MertToolState.SuppressCategoryCapture = false;
                MertToolState.SuppressUiMemoryCapture = false;
                MertToolState.SuppressLiveUiCapture = false;
                MertToolState.SuppressUiAbortDuringRestore = false;
                MertToolState.ClearPendingRestore();
            }
        }
        #endregion

        #region Helpers & UI Formatting Utilities
        private string GetActiveToolPipe()
        {
            string next = "None|None";

            if (m_Shape != null && m_Shape.ToolEnabled)
                next = $"{m_Shape.ToolId}|{m_Shape.ToolName}";
            else if (m_Helix != null && m_Helix.ToolEnabled)
                next = $"{m_Helix.ToolId}|{m_Helix.ToolName}";
            else if (m_SoftBlock != null && m_SoftBlock.ToolEnabled)
                next = $"{m_SoftBlock.ToolId}|{m_SoftBlock.ToolName}";
            else if (m_Grid != null && m_Grid.ToolEnabled)
                next = $"{m_Grid.ToolId}|{m_Grid.ToolName}";

            if (next == m_LastActiveToolPipe)
                return m_LastActiveToolPipe;

            m_LastActiveToolPipe = next;
            return next;
        }
        private string GetToolListPipe()
        {
            bool isTrackLike =
                m_SoftBlock != null && m_SoftBlock.IsCurrentTrackLikePrefab();

            bool isPierLike =
                m_Helix != null && m_Helix.IsCurrentPierLikePrefab();

            string next;

            if (isTrackLike)
            {
                next = m_SoftBlock != null
                    ? $"{m_SoftBlock.ToolId}|{m_SoftBlock.ToolName}|{m_SoftBlock.ToolId}"
                    : "";
            }
            else if (isPierLike)
            {
                next = m_Helix != null
                    ? $"{m_Helix.ToolId}|{m_Helix.ToolName}|{m_Helix.ToolId}"
                    : "";
            }
            else
            {
                List<string> parts = new();

                if (m_Shape != null)
                    parts.Add($"{m_Shape.ToolId}|{m_Shape.ToolName}|{m_Shape.ToolId}");
                if (m_Helix != null)
                    parts.Add($"{m_Helix.ToolId}|{m_Helix.ToolName}|{m_Helix.ToolId}");
                if (m_SoftBlock != null)
                    parts.Add($"{m_SoftBlock.ToolId}|{m_SoftBlock.ToolName}|{m_SoftBlock.ToolId}");
                if (m_Grid != null)
                    parts.Add($"{m_Grid.ToolId}|{m_Grid.ToolName}|{m_Grid.ToolId}");

                next = string.Join(";", parts);
            }

            m_LastToolListPipe = next;
            return next;
        }
        private bool GetIsToolBoxAllowed()
        {
            NetPrefab currentPrefab =
                m_Helix?.GetCurrentSelectedNetPrefabForUi() ??
                m_SoftBlock?.GetCurrentSelectedNetPrefabForUi();

            if (currentPrefab != m_LastToolListPrefab)
            {
                m_LastToolListPrefab = currentPrefab;
                RefreshToolListBinding();
            }

            bool prefabValid =
                (m_Shape != null && m_Shape.IsCurrentPrefabValid()) ||
                (m_Helix != null && m_Helix.IsCurrentPrefabValid()) ||
                (m_SoftBlock != null && m_SoftBlock.IsCurrentPrefabValid()) ||
                (m_Grid != null && m_Grid.IsCurrentPrefabValid());

            if (!prefabValid)
            {
                m_LastIsToolBoxAllowed = false;
                return false;
            }

            bool anyToolEnabled =
                (m_Shape != null && m_Shape.ToolEnabled) ||
                (m_Helix != null && m_Helix.ToolEnabled) ||
                (m_SoftBlock != null && m_SoftBlock.ToolEnabled) ||
                (m_Grid != null && m_Grid.ToolEnabled);

            bool toolContextValid =
                m_ToolSystem != null &&
                (m_ToolSystem.activeTool is NetToolSystem || anyToolEnabled);

            m_LastIsToolBoxAllowed = prefabValid && toolContextValid;
            return m_LastIsToolBoxAllowed;
        }

        /// <summary>
        /// Generates the formatted status text displaying current metrics for the active tool.
        /// </summary>
        private string GetActionStatusText()
        {
            if (m_Shape != null && m_Shape.ToolEnabled)
            {
                ShapeMetrics m = m_Shape.GetCurrentShapeMetrics();
                return $"Outer: {FormatSmart(m.OuterDimensionUnits)}U ({FormatSmart(m.OuterDimensionMeters)}m) - " +
                       $"Inner: {FormatSmart(m.InnerDimensionUnits)}U ({FormatSmart(m.InnerDimensionMeters)}m)";
            }
            if (m_Helix != null && m_Helix.ToolEnabled)
            {
                return $"Diameter: {FormatSmart(m_Helix.GetCurrentDiameter())}m - " +
                       $"Turns: {FormatSmart(m_Helix.GetCurrentTurns())} - " +
                       $"Clearance: {FormatSmart(m_Helix.GetCurrentClearance())}m";
            }
            if (m_SoftBlock != null && m_SoftBlock.ToolEnabled)
            {
                return $"Width: {FormatSmart(m_SoftBlock.GetCurrentWidth())}m - " +
                       $"Length: {FormatSmart(m_SoftBlock.GetCurrentLength())}m - " +
                       $"Radius: {FormatSmart(m_SoftBlock.GetCurrentBorderRadius())}";
            }
            if (m_Grid != null && m_Grid.ToolEnabled)
            {
                return $"Width: {FormatSmart(m_Grid.GetCurrentBlockWidthU())}U - " +
                       $"Length: {FormatSmart(m_Grid.GetCurrentBlockLengthU())}U - " +
                       $"Rows: {FormatSmart(m_Grid.GetCurrentRows())} - " +
                       $"Columns: {FormatSmart(m_Grid.GetCurrentColumns())}";
            }
            return string.Empty;
        }

        /// <summary>
        /// Formats a floating-point value to a concise string representation.
        /// </summary>
        private static string FormatSmart(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats an integer value to a string representation.
        /// </summary>
        private static string FormatSmart(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        #endregion

        #region Preset System
        
        private void RefreshPresetListBinding()
        {
            MertBaseToolSystem tool = GetActiveCustomTool();
            if (tool == null)
            {
                m_PresetListBinding?.SetValue("");
                return;
            }

            NetPrefab prefab = tool.GetCurrentSelectedNetPrefabForUi();
            if (prefab == null)
            {
                m_PresetListBinding?.SetValue("");
                return;
            }

            List<MertToolPreset> presets =
                MertToolPresetStorage.LoadPresets(tool.ToolId, prefab.name);

            string value = string.Join(";", presets.Select(p => p.DisplayName));
            m_PresetListBinding?.SetValue(value);
        }
        #endregion

    }
}