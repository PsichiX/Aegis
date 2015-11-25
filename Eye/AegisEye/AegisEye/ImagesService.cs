namespace AegisEye
{
    public interface ImagesService
    {
        void OnInitialize(CommunicationService com, int interval);
        void OnRelease();
        void NotifyImageAvailable();
    }
}
