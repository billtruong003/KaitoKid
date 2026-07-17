namespace GameSystem.Interaction
{
    public interface IInteractable
    {
        void Interact();
        string GetInteractionPrompt();
    }
}