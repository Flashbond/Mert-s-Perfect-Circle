using MertsToolBox.Utilities.Preset;
using System.Collections.Generic;

namespace MertsToolBox.Utilities.Undo
{
    public sealed class MertToolUndoHistory
    {
        private readonly List<MertToolPreset> m_Undo = new();
        private readonly List<MertToolPreset> m_Redo = new();

        public bool IsApplyingUndoRedo { get; private set; }
        private bool m_WheelBurstActive;
        private bool m_SliderDragActive;

        public void Clear()
        {
            m_Undo.Clear();
            m_Redo.Clear();
            m_WheelBurstActive = false;
            m_SliderDragActive = false;
            IsApplyingUndoRedo = false;
        }

        public void RegisterButton(System.Func<MertToolPreset> snapshotFactory)
        {
            m_WheelBurstActive = false;
            m_SliderDragActive = false;
            Push(snapshotFactory);
        }

        public void RegisterWheel(System.Func<MertToolPreset> snapshotFactory)
        {
            if (m_WheelBurstActive)
                return;

            m_WheelBurstActive = true;
            m_SliderDragActive = false;
            Push(snapshotFactory);
        }

        public void BeginSlider(System.Func<MertToolPreset> snapshotFactory)
        {
            if (m_SliderDragActive)
                return;

            m_SliderDragActive = true;
            m_WheelBurstActive = false;
            Push(snapshotFactory);
        }

        public void EndSlider()
        {
            m_SliderDragActive = false;
        }

        private void Push(System.Func<MertToolPreset> snapshotFactory)
        {
            if (IsApplyingUndoRedo)
                return;

            MertToolPreset snapshot = snapshotFactory?.Invoke();
            if (snapshot == null)
                return;

            m_Undo.Add(snapshot);
            m_Redo.Clear();
        }

        public void Undo(System.Func<MertToolPreset> snapshotFactory, System.Action<MertToolPreset> apply)
        {
            if (m_Undo.Count == 0)
                return;

            MertToolPreset current = snapshotFactory?.Invoke();
            MertToolPreset previous = PopLast(m_Undo);

            if (current != null)
                m_Redo.Add(current);

            Apply(previous, apply);
        }

        public void Redo(System.Func<MertToolPreset> snapshotFactory, System.Action<MertToolPreset> apply)
        {
            if (m_Redo.Count == 0)
                return;

            MertToolPreset current = snapshotFactory?.Invoke();
            MertToolPreset next = PopLast(m_Redo);

            if (current != null)
                m_Undo.Add(current);

            Apply(next, apply);
        }

        private static MertToolPreset PopLast(List<MertToolPreset> list)
        {
            int index = list.Count - 1;
            MertToolPreset value = list[index];
            list.RemoveAt(index);
            return value;
        }

        private void Apply(MertToolPreset preset, System.Action<MertToolPreset> apply)
        {
            IsApplyingUndoRedo = true;
            try
            {
                apply?.Invoke(preset);
            }
            finally
            {
                IsApplyingUndoRedo = false;
                m_WheelBurstActive = false;
                m_SliderDragActive = false;
            }
        }
    }
}