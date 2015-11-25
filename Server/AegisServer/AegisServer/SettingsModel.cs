using System;

namespace AegisServer
{
    public class SettingsModel
    {
        public volatile int EyePort;
        public volatile int ControllerPort;
        public string Token
        {
            get { return m_token; }
            set
            {
                if (value.Length < Program.TOKEN_SIZE)
                    for (int i = value.Length; i < Program.TOKEN_SIZE; ++i)
                        value += " ";
                else if (value.Length > Program.TOKEN_SIZE)
                    value = value.Substring(0, Program.TOKEN_SIZE);
                m_token = value;
            }
        }
        public volatile int Treshold;
        public volatile bool ArmedAlarm;
        public volatile bool Silent;

        private volatile string m_token;

        public SettingsModel()
        {
            EyePort = 8081;
            ControllerPort = 8082;
            Token = Guid.NewGuid().ToString();
            Treshold = 0;
            ArmedAlarm = true;
            Silent = false;
        }
    }
}
