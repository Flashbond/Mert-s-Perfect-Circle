using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;
using System;

namespace MertsToolBox.Settings
{
    [FileLocation("ModsSettings/MertsToolBox/MertsToolBox")]
    [SettingsUIKeyboardAction(OpenCircleTool, ActionType.Button, usages: new string[] { "Tool" })]
    [SettingsUIKeyboardAction(OpenHelixTool, ActionType.Button, usages: new string[] { "Tool" })]
    [SettingsUIKeyboardAction(OpenSoftBlockTool, ActionType.Button, usages: new string[] { "Tool" })]
    [SettingsUIKeyboardAction(OpenGridTool, ActionType.Button, usages: new string[] { "Tool" })]
    [SettingsUIKeyboardAction(UndoToolParameter, ActionType.Button, usages: new string[] { "Tool" })]
    [SettingsUIKeyboardAction(RedoToolParameter, ActionType.Button, usages: new string[] { "Tool" })]
    [SettingsUITabOrder(
        TAB_GENERAL,
        TAB_CIRCLE,
        TAB_HELIX,
        TAB_SOFTBLOCK,
        TAB_GRID
    )]
    [SettingsUIGroupOrder(GROUP_KEYBINDS, GROUP_DEFAULTS, GROUP_CONTROLS)]
    public class ToolBoxSettings : ModSetting
    {
        public const string TAB_GENERAL = "General";
       
        public const string TAB_CIRCLE = "Perfect Circle";
        public const string TAB_HELIX = "Procedural Helix";
        public const string TAB_SOFTBLOCK = "Soft Block";
        public const string TAB_GRID = "Smart Grid";

        public const string GROUP_KEYBINDS = "Key Bindings";
        public const string GROUP_DEFAULTS = "Defaults";
        public const string GROUP_CONTROLS = "Controls";

        public const string OpenCircleTool = "OpenCircleTool";
        public const string OpenHelixTool = "OpenHelixTool";
        public const string OpenSoftBlockTool = "OpenSoftBlockTool";
        public const string OpenGridTool = "OpenGridTool";
        public const string UndoToolParameter = "UndoToolParameter";
        public const string RedoToolParameter = "RedoToolParameter";

        private int m_DefaultCircleDiameter = 96;
        private bool m_UseCtrlWheelForCircleDiameterAdjustment = false;

        private int m_DefaultHelixDiameter = 96;
        private float m_DefaultTurns = 3f;
        private float m_DefaultClearance = 8f;
        private bool m_UseCtrlWheelForHelixTurnAdjustment = false;

        private int m_DefaultSoftBlockWidth = 96;
        private int m_DefaultSoftBlockLength = 192;
        private bool m_UseCtrlWheelForSoftBlockBorderRadius = false;

        private int m_BlockWidthU = 6;
        private int m_BlockLengthU = 6;
        private int m_Columns = 2;
        private int m_Rows = 2;
        private bool m_EnableGridSnap = false;

        public static event Action OnOptionsChanged;

        public ToolBoxSettings(IMod mod) : base(mod)
        {
            SetDefaults();
        }
        // -------------------------
        // General
        // -------------------------
        [SettingsUISection(TAB_GENERAL, GROUP_KEYBINDS)]
        [SettingsUIKeyboardBinding(BindingKeyboard.C, OpenCircleTool, ctrl: true)]
        public ProxyBinding OpenCircleToolKey { get; set; }

        [SettingsUISection(TAB_GENERAL, GROUP_KEYBINDS)]
        [SettingsUIKeyboardBinding(BindingKeyboard.H, OpenHelixTool, ctrl: true)]
        public ProxyBinding OpenHelixToolKey { get; set; }

        [SettingsUISection(TAB_GENERAL, GROUP_KEYBINDS)]
        [SettingsUIKeyboardBinding(BindingKeyboard.S, OpenSoftBlockTool, ctrl: true)]
        public ProxyBinding OpenSoftBlockToolKey { get; set; }

        [SettingsUISection(TAB_GENERAL, GROUP_KEYBINDS)]
        [SettingsUIKeyboardBinding(BindingKeyboard.G, OpenGridTool, ctrl: true)]
        public ProxyBinding OpenGridToolKey { get; set; }

        [SettingsUISection(TAB_GENERAL, GROUP_KEYBINDS)]
        [SettingsUIKeyboardBinding(BindingKeyboard.Z, UndoToolParameter, ctrl: true)]
        public ProxyBinding UndoToolParameterKey { get; set; }

        [SettingsUISection(TAB_GENERAL, GROUP_KEYBINDS)]
        [SettingsUIKeyboardBinding(BindingKeyboard.Y, RedoToolParameter, ctrl: true)]
        public ProxyBinding RedoToolParameterKey { get; set; }

        // -------------------------
        // Circle
        // -------------------------
        [SettingsUISection(TAB_CIRCLE, GROUP_DEFAULTS)]
        [SettingsUISlider(min = 48, max = 320, step = 1)]
        public int DefaultCircleDiameter
        {
            get => m_DefaultCircleDiameter;
            set
            {
                int clamped = Math.Clamp(value, 48, 320);
                if (m_DefaultCircleDiameter == clamped) return;

                m_DefaultCircleDiameter = clamped;
                OnOptionsChanged?.Invoke();
            }
        }

        [SettingsUISection(TAB_CIRCLE, GROUP_CONTROLS)]
        public bool UseCtrlWheelForCircleDiameterAdjustment
        {
            get => m_UseCtrlWheelForCircleDiameterAdjustment;
            set
            {
                if (m_UseCtrlWheelForCircleDiameterAdjustment == value) return;

                m_UseCtrlWheelForCircleDiameterAdjustment = value;
                OnOptionsChanged?.Invoke();
            }
        }

        // -------------------------
        // Helix
        // -------------------------
        [SettingsUISection(TAB_HELIX, GROUP_DEFAULTS)]
        [SettingsUISlider(min = 48, max = 320, step = 1)]
        public int DefaultHelixDiameter
        {
            get => m_DefaultHelixDiameter;
            set
            {
                int clamped = Math.Clamp(value, 48, 320);
                if (m_DefaultHelixDiameter == clamped) return;

                m_DefaultHelixDiameter = clamped;
                OnOptionsChanged?.Invoke();
            }
        }

        [SettingsUISection(TAB_HELIX, GROUP_DEFAULTS)]
        [SettingsUISlider(min = 1, max = 12, step = 1f)]
        public float DefaultTurns
        {
            get => m_DefaultTurns;
            set
            {
                float clamped = Math.Clamp(value, 1f, 12f);
                if (Math.Abs(m_DefaultTurns - clamped) < 0.0001f) return;

                m_DefaultTurns = clamped;
                OnOptionsChanged?.Invoke();
            }
        }

        [SettingsUISection(TAB_HELIX, GROUP_DEFAULTS)]
        [SettingsUISlider(min = 8, max = 14, step = 1f)]
        public float DefaultClearance
        {
            get => m_DefaultClearance;
            set
            {
                float clamped = Math.Clamp(value, 8f, 14f);
                if (Math.Abs(m_DefaultClearance - clamped) < 0.0001f) return;

                m_DefaultClearance = clamped;
                OnOptionsChanged?.Invoke();
            }
        }

        [SettingsUISection(TAB_HELIX, GROUP_CONTROLS)]
        public bool UseCtrlWheelForHelixTurnAdjustment
        {
            get => m_UseCtrlWheelForHelixTurnAdjustment;
            set
            {
                if (m_UseCtrlWheelForHelixTurnAdjustment == value) return;

                m_UseCtrlWheelForHelixTurnAdjustment = value;
                OnOptionsChanged?.Invoke();
            }
        }

        // -------------------------
        // Soft Block
        // -------------------------
        [SettingsUISection(TAB_SOFTBLOCK, GROUP_DEFAULTS)]
        [SettingsUISlider(min = 48, max = 320, step = 1)]
        public int DefaultSoftBlockWidth
        {
            get => m_DefaultSoftBlockWidth;
            set
            {
                int clamped = Math.Clamp(value, 48, 320);
                if (m_DefaultSoftBlockWidth == clamped) return;

                m_DefaultSoftBlockWidth = clamped;
                OnOptionsChanged?.Invoke();
            }
        }

        [SettingsUISection(TAB_SOFTBLOCK, GROUP_DEFAULTS)]
        [SettingsUISlider(min = 48, max = 320, step = 1)]
        public int DefaultSoftBlockLength
        {
            get => m_DefaultSoftBlockLength;
            set
            {
                int clamped = Math.Clamp(value, 48, 320);
                if (m_DefaultSoftBlockLength == clamped) return;

                m_DefaultSoftBlockLength = clamped;
                OnOptionsChanged?.Invoke();
            }
        }

        [SettingsUISection(TAB_SOFTBLOCK, GROUP_CONTROLS)]
        public bool UseCtrlWheelForSoftBlockBorderRadius
        {
            get => m_UseCtrlWheelForSoftBlockBorderRadius;
            set
            {
                if (m_UseCtrlWheelForSoftBlockBorderRadius == value) return;

                m_UseCtrlWheelForSoftBlockBorderRadius = value;
                OnOptionsChanged?.Invoke();
            }
        }

        // -------------------------
        // Grid
        // -------------------------
        [SettingsUISection(TAB_GRID, GROUP_DEFAULTS)]
        [SettingsUISlider(min = 1, max = 24, step = 1)]
        public int BlockWidthU
        {
            get => m_BlockWidthU;
            set
            {
                int clamped = Math.Clamp(value, 1, 24);
                if (m_BlockWidthU == clamped) return;

                m_BlockWidthU = clamped;
                OnOptionsChanged?.Invoke();
            }
        }

        [SettingsUISection(TAB_GRID, GROUP_DEFAULTS)]
        [SettingsUISlider(min = 1, max = 24, step = 1)]
        public int BlockLengthU
        {
            get => m_BlockLengthU;
            set
            {
                int clamped = Math.Clamp(value, 1, 24);
                if (m_BlockLengthU == clamped) return;

                m_BlockLengthU = clamped;
                OnOptionsChanged?.Invoke();
            }
        }

        [SettingsUISection(TAB_GRID, GROUP_DEFAULTS)]
        [SettingsUISlider(min = 1, max = 24, step = 1)]
        public int Columns
        {
            get => m_Columns;
            set
            {
                int clamped = Math.Clamp(value, 1, 24);
                if (m_Columns == clamped) return;

                m_Columns = clamped;
                OnOptionsChanged?.Invoke();
            }
        }

        [SettingsUISection(TAB_GRID, GROUP_DEFAULTS)]
        [SettingsUISlider(min = 1, max = 24, step = 1)]
        public int Rows
        {
            get => m_Rows;
            set
            {
                int clamped = Math.Clamp(value, 1, 24);
                if (m_Rows == clamped) return;

                m_Rows = clamped;
                OnOptionsChanged?.Invoke();
            }
        }
        [SettingsUISection(TAB_GRID, GROUP_CONTROLS)]
        public bool EnableGridSnap
        {
            get => m_EnableGridSnap;
            set
            {
                if (m_EnableGridSnap == value) return;

                m_EnableGridSnap = value;
                OnOptionsChanged?.Invoke();
            }
        }
        public override void SetDefaults()
        {
            m_DefaultCircleDiameter = 96;
            m_UseCtrlWheelForCircleDiameterAdjustment = false;

            m_DefaultHelixDiameter = 96;
            m_DefaultTurns = 3f;
            m_DefaultClearance = 8f;
            m_UseCtrlWheelForHelixTurnAdjustment = false;

            m_DefaultSoftBlockWidth = 96;
            m_DefaultSoftBlockLength = 192;
            m_UseCtrlWheelForSoftBlockBorderRadius = false;

            m_BlockWidthU = 6;
            m_BlockLengthU = 6;
            m_Columns = 2;
            m_Rows = 2;
            m_EnableGridSnap = false;
        }
    }
}