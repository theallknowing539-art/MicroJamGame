// ================================================================
// Interact.cs
// Attach to: any interactable object (rum bottle, door, etc.)
// This is the component other scripts subscribe to
// ================================================================
using UnityEngine;
using System;

public class Interact : MonoBehaviour
{
    public class InteractEvent
    {
        public event Action HasInteracted;
        public void Invoke() => HasInteracted?.Invoke();
    }

    public InteractEvent GetInteractEvent { get; private set; } = new InteractEvent();

    public void CallInteract()
    {
        GetInteractEvent.Invoke();
    }
}