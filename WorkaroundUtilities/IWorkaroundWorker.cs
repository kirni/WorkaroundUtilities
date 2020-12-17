namespace WorkaroundUtilities
{
    public interface IWorkaroundWorker
    {
        bool hasActions { get; }
        bool hasEvents { get; }

        void Run();
        string ToString();
        void Stop();
    }
}