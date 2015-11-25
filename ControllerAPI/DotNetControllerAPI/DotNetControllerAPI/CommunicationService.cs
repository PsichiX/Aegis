using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aegis
{
    namespace DotNetControllerAPI
    {
        public class CommunicationService
        {
            internal const int TOKEN_SIZE = 64;

            [Flags]
            public enum ControllerPermissions
            {
                None = 0,
                ReceiveEyeImages = 1 << 0,
                EnableDisableAlarm = 1 << 1,
                ArmDisarmAlarm = 1 << 2,
                All = -1
            }

            public class ImageData
            {
                public int Width { get { return m_width; } }
                public int Height { get { return m_height; } }
                public int Channels { get { return m_channels; } }
                public byte[] Data { get { return m_data; } }

                private int m_width;
                private int m_height;
                private int m_channels;
                private byte[] m_data;

                internal ImageData(int w, int h, int c, byte[] d)
                {
                    m_width = w;
                    m_height = h;
                    m_channels = c;
                    m_data = d;
                }
            }

            private enum MessageType
            {
                Unknown,
                ControlAlarm,
                ArmDisarmAlarm,
                ReceiveImage,
                ObtainCamerasList,
                ChangeSourceCamera
            }

            public bool Connected
            {
                get
                {
                    lock (m_lock)
                    {
                        return m_client != null ? m_client.Connected : false;
                    }
                }
            }

            public ControllerPermissions Permissions
            {
                get
                {
                    lock (m_lock)
                    {
                        return m_permissions;
                    }
                }
            }

            public List<string> CamerasList
            {
                get
                {
                    lock (m_camerasListLock)
                    {
                        return m_camerasList;
                    }
                }
            }

            public ImageData Image
            {
                get
                {
                    lock (m_imageLock)
                    {
                        return m_image;
                    }
                }
            }

            public event EventHandler<EventArgs> OnObtainCamerasList;
            public event EventHandler<EventArgs> OnReceiveImage;

            private TcpClient m_client;
            private string m_token;
            private Object m_lock = new Object();
            private ControllerPermissions m_permissions = ControllerPermissions.None;
            private volatile bool m_running = false;
            private Queue<byte[]> m_dataQueue;
            private Object m_dataQueueLock = new Object();
            private List<string> m_camerasList;
            private Object m_camerasListLock = new Object();
            private Object m_OnObtainCamerasListLock = new Object();
            private ImageData m_image;
            private Object m_imageLock = new Object();
            private Object m_OnReceiveImageLock = new Object();

            public bool Connect(string address, int port, string token, ControllerPermissions permissions, int receiveTimeout = 1000)
            {
                Disconnect();
                lock (m_dataQueueLock)
                {
                    m_dataQueue = new Queue<byte[]>();
                }
                try
                {
                    lock (m_lock)
                    {
                        m_client = new TcpClient();
                        m_client.ReceiveTimeout = receiveTimeout;
                        m_client.NoDelay = true;
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
                        stream.Write(BitConverter.GetBytes((Int32)permissions), 0, 4);
                        tokenBytes = new byte[TOKEN_SIZE];
                        var tokenSize = stream.Read(tokenBytes, 0, TOKEN_SIZE);
                        if (tokenSize == TOKEN_SIZE)
                        {
                            lock (m_camerasListLock)
                            {
                                m_camerasList = new List<string>();
                            }
                            m_token = ascii.GetString(tokenBytes);
                            m_permissions = permissions;
                            SpawnCommandsReceiverTask();
                            SpawnCommandsExecutorTask();
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
                m_running = false;
                lock (m_lock)
                {
                    m_token = null;
                    if (m_client != null)
                        m_client.Close();
                    m_client = null;
                    m_permissions = ControllerPermissions.None;
                }
                lock (m_dataQueueLock)
                {
                    m_dataQueue = null;
                }
                lock (m_camerasListLock)
                {
                    m_camerasList = null;
                }
                lock (m_imageLock)
                {
                    m_image = null;
                }
                OnObtainCamerasList = null;
                OnReceiveImage = null;
            }

            public void ControlAlarm(bool mode)
            {
                lock (m_lock)
                {
                    if (!m_permissions.HasFlag(ControllerPermissions.EnableDisableAlarm))
                        return;
                    using (var stream = new MemoryStream())
                    using (var bs = new BinaryWriter(stream))
                    {
                        bs.Write((int)MessageType.ControlAlarm);
                        bs.Write(mode);
                        Send(stream.GetBuffer());
                    }
                }
            }

            public void StartAlarm() { ControlAlarm(true); }
            public void StopAlarm() { ControlAlarm(false); }

            public void ArmDisarmAlarm(bool mode)
            {
                lock (m_lock)
                {
                    if (!m_permissions.HasFlag(ControllerPermissions.EnableDisableAlarm))
                        return;
                    using (var stream = new MemoryStream())
                    using (var bs = new BinaryWriter(stream))
                    {
                        bs.Write((int)MessageType.ArmDisarmAlarm);
                        bs.Write(mode);
                        Send(stream.GetBuffer());
                    }
                }
            }

            public void ArmAlarm() { ArmDisarmAlarm(true); }
            public void DisarmAlarm() { ArmDisarmAlarm(false); }

            public void ObtainCamerasList()
            {
                lock (m_lock)
                {
                    if (!m_permissions.HasFlag(ControllerPermissions.ReceiveEyeImages))
                        return;
                    Send(BitConverter.GetBytes((int)MessageType.ObtainCamerasList));
                }
            }

            public void ChangeSourceCamera(string id)
            {
                lock (m_lock)
                {
                    if (id == null || !m_permissions.HasFlag(ControllerPermissions.ReceiveEyeImages))
                        return;
                    var ascii = new ASCIIEncoding();
                    var ib = ascii.GetBytes(id);
                    if (ib == null || ib.Length != TOKEN_SIZE)
                        return;
                    using (var stream = new MemoryStream())
                    using (var bs = new BinaryWriter(stream))
                    {
                        bs.Write((int)MessageType.ChangeSourceCamera);
                        bs.Write(ib);
                        Send(stream.GetBuffer());
                    }
                }
            }

            private void Send(byte[] msgData)
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

            private void SpawnCommandsReceiverTask()
            {
                if (m_running)
                    return;
                m_running = true;
                Task.Run(() =>
                {
                    var sb = new byte[4];
                    TcpClient c = m_client;
                    if (c == null)
                        return;
                    try
                    {
                        while (m_running && c.Connected)
                        {
                            if (c.Available > 0)
                            {
                                var s = c.GetStream();
                                if (s.Read(sb, 0, 4) == 4)
                                {
                                    var mb = new byte[BitConverter.ToInt32(sb, 0)];
                                    s.Read(mb, 0, mb.Length);
                                    lock (m_dataQueueLock)
                                    {
                                        m_dataQueue.Enqueue(mb);
                                    }
                                }
                            }
                            else
                                Thread.Sleep(100);
                        }
                    }
                    catch { }
                });
            }

            private void SpawnCommandsExecutorTask()
            {
                Task.Run(() =>
                {
                    while (m_running)
                    {
                        byte[] data = null;
                        lock (m_dataQueueLock)
                        {
                            data = m_dataQueue.Count > 0 ? m_dataQueue.Dequeue() : null;
                        }
                        if (data != null)
                        {
                            using (var stream = new MemoryStream(data))
                            using (var bs = new BinaryReader(stream))
                            {
                                try
                                {
                                    var type = (MessageType)bs.ReadInt32();
                                    if (type == MessageType.ReceiveImage)
                                    {
                                        var w = bs.ReadInt32();
                                        var h = bs.ReadInt32();
                                        var c = bs.ReadInt32();
                                        var d = bs.ReadBytes(w * h * c);
                                        lock (m_imageLock)
                                        {
                                            m_image = new ImageData(w, h, c, d);
                                        }
                                        lock (m_OnReceiveImageLock)
                                        {
                                            if (OnReceiveImage != null)
                                                OnReceiveImage(this, new EventArgs());
                                        }
                                    }
                                    else if (type == MessageType.ObtainCamerasList)
                                    {
                                        var ascii = new ASCIIEncoding();
                                        var count = bs.ReadInt32();
                                        lock (m_camerasListLock)
                                        {
                                            m_camerasList = new List<string>();
                                            for (var i = 0; i < count; ++i)
                                                m_camerasList.Add(ascii.GetString(bs.ReadBytes(TOKEN_SIZE)));
                                        }
                                        lock (m_OnObtainCamerasListLock)
                                        {
                                            if (OnObtainCamerasList != null)
                                                OnObtainCamerasList(this, new EventArgs());
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                        else
                            Thread.Sleep(50);
                    }
                    lock (m_lock)
                    {
                        lock (m_dataQueueLock)
                        {
                            m_dataQueue = null;
                        }
                    }
                });
            }
        }
    }
}
