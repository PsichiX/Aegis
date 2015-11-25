using Leap;
using System.Diagnostics;

namespace AegisEye
{
    public class LeapImagesService : ImagesService
    {
        private LeapListener m_listener;
        private Controller m_controller;
        private CommunicationService m_communication;
        private Stopwatch m_timer;
        private int m_interval;

        public void OnInitialize(CommunicationService com, int interval)
        {
            OnRelease();
            m_communication = com;
            m_interval = interval;
            m_timer = new Stopwatch();
            m_timer.Start();
            m_listener = new LeapListener(this);
            m_controller = new Controller();
            m_controller.SetPolicy(m_controller.PolicyFlags | Controller.PolicyFlag.POLICY_IMAGES | Controller.PolicyFlag.POLICY_BACKGROUND_FRAMES);
            m_controller.AddListener(m_listener);
        }

        public void OnRelease()
        {
            if (m_controller != null)
            {
                m_controller.RemoveListener(m_listener);
                m_controller.Dispose();
                m_controller = null;
            }
            m_listener = null;
            m_communication = null;
            if (m_timer != null)
            {
                m_timer.Stop();
                m_timer = null;
            }
        }

        public void NotifyImageAvailable()
        {
            if (m_communication != null && m_timer != null && m_timer.ElapsedMilliseconds > m_interval)
            {
                m_communication.Send(m_listener.Image.ToBytes());
                m_timer.Restart();
            }
        }
    }
}
