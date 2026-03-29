// ================================================================
// IInteractable.cs
// Not a MonoBehaviour — just an interface, no GameObject needed
// ================================================================
public interface IInteractable
{
    string InteractionHint { get; }
    void Interact();
}