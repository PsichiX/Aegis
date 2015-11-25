using System;
using System.Net.Sockets;
using System.Text;

namespace AegisEye
{
    public class CommunicationService
    {
        public const int TOKEN_SIZE = 64;

        private TcpClient m_client;
        private string m_token;
        private Object m_lock = new Object();

        public bool Connect(string address, int port, string token)
        {
            Disconnect();
            try
            {
                lock (m_lock)
                {
                    m_client = new TcpClient();
                    m_client.Connect(address, port);
                    if (token.Length < TOKEN_SIZE)
                        for (int i = token.Length; i < TOKEN_SIZE; ++i)
                            token += " ";
                    else if (token.Length > TOKEN_SIZE)
                        token = token.Substring(0, TOKEN_SIZE);
                    var stream = m_client.GetStream();
                    var ascii = new ASCIIEncoding();
                    var tokenBytes = ascii.GetBytes(token);
                    stream.Write(tokenBytes, 0, tokenBytes.Length);
                    tokenBytes = new byte[TOKEN_SIZE];
                    var tokenSize = stream.Read(tokenBytes, 0, TOKEN_SIZE);
                    if (tokenSize == TOKEN_SIZE)
                    {
                        m_token = ascii.GetString(tokenBytes);
                        return true;
                    }
                }
            }
            catch { }
            Disconnect();
            return false;
        }

        public void Disconnect()
        {
            lock (m_lock)
            {
                m_token = null;
                if (m_client != null)
                    m_client.Close();
                m_client = null;
            }
        }

        public void Send(byte[] msgData)
        {
            lock (m_lock)
            {
                if (msgData != null && m_client != null && m_token != null)
                {
                    try
                    {
                        var ascii = new ASCIIEncoding();
                        var tokenBytes = ascii.GetBytes(m_token);
                        var stream = m_client.GetStream();
                        stream.Write(tokenBytes, 0, tokenBytes.Length);
                        stream.Write(BitConverter.GetBytes(msgData.Length), 0, 4);
                        stream.Write(msgData, 0, msgData.Length);
                    }
                    catch { }
                }
            }
        }
    }
}
