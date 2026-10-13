using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EcoSortQuest
{
    public sealed class PointerInput : MonoBehaviour
    {
        public event Action<Vector2> Selected;
        public event Action<Vector2> Dragged;
        public event Action<Vector2, bool> Released;
        public event Action PausePressed;
        InputAction select, position, pause;
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        void OnEnable()
        {
            select = new InputAction("Select", InputActionType.Button, "<Pointer>/press");
            position = new InputAction("Point", InputActionType.PassThrough, "<Pointer>/position");
            pause = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            select.Enable(); position.Enable(); pause.Enable();
        }
        void Update()
        {
            if (pause.WasPressedThisFrame()) PausePressed?.Invoke();
            Vector2 point = position.ReadValue<Vector2>();
            if (select.WasReleasedThisFrame()) Released?.Invoke(point, IsOverUI(point));
            if (select.IsPressed() && !select.WasPressedThisFrame()) Dragged?.Invoke(point);
            if (!select.WasPressedThisFrame()) return;
            if (IsOverUI(point)) return;
            Selected?.Invoke(point);
        }
        bool IsOverUI(Vector2 point)
        {
            if (EventSystem.current != null)
            {
                uiHits.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, uiHits);
                if (uiHits.Count > 0) return true;
            }
            return false;
        }
        void OnDisable()
        { select?.Dispose(); position?.Dispose(); pause?.Dispose(); }
    }
}
