namespace EyeAuras.Shared
{
    public interface IAuraAction : IAuraModel
    {
        string ActionName { get; }

        string ActionDescription { get; }
        
        string Error { get; }

        void Execute();
    }
}