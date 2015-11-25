
namespace AegisServer
{
    public static class Commands
    {
        public class ControlAlarm : Program.Command
        {
            private Program m_owner;
            private bool m_mode;

            public ControlAlarm(Program owner, bool mode)
            {
                m_owner = owner;
                m_mode = mode;
            }

            public void PerformAction()
            {
                if (m_owner != null)
                {
                    if (m_mode)
                        m_owner.StartAlarm();
                    else
                        m_owner.StopAlarm();
                }
            }
        }

        public class ArmDisarmAlarm : Program.Command
        {
            private Program m_owner;
            private bool m_mode;

            public ArmDisarmAlarm(Program owner, bool mode)
            {
                m_owner = owner;
                m_mode = mode;
            }

            public void PerformAction()
            {
                if (m_owner != null)
                {
                    if (m_mode)
                        m_owner.ArmAlarm();
                    else
                        m_owner.DisarmAlarm();
                }
            }
        }

        public class SendImage : Program.Command
        {
            private Program m_owner;
            private string m_eyeToken;
            private byte[] m_data;

            public SendImage(Program owner, string eyeToken, byte[] data)
            {
                m_owner = owner;
                m_eyeToken = eyeToken;
                m_data = data;
            }

            public void PerformAction()
            {
                if (m_owner != null && m_eyeToken != null)
                    m_owner.SendImage(m_eyeToken, m_data);
            }
        }

        public class ObtainCamerasList : Program.Command
        {
            private Program m_owner;
            private string m_dstToken;

            public ObtainCamerasList(Program owner, string dstToken)
            {
                m_owner = owner;
                m_dstToken = dstToken;
            }

            public void PerformAction()
            {
                if (m_owner != null)
                    m_owner.SendCamerasList(m_dstToken);
            }
        }

        public class ChangeSourceCamera : Program.Command
        {
            private Program m_owner;
            private string m_dstToken;
            private string m_srcToken;

            public ChangeSourceCamera(Program owner, string dstToken, string srcToken)
            {
                m_owner = owner;
                m_dstToken = dstToken;
                m_srcToken = srcToken;
            }

            public void PerformAction()
            {
                if (m_owner != null && m_dstToken != null && m_srcToken != null)
                    m_owner.ChangeSourceCamera(m_dstToken, m_srcToken);
            }
        }
    }
}
