using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AegisServer
{
    public class Program
    {
        [Flags]
        public enum ControllerPermissions
        {
            None = 0,
            ReceiveEyeImages = 1 << 0,
            EnableDisableAlarm = 1 << 1,
            ArmDisarmAlarm = 1 << 2,
            All = -1
        }

        internal enum MessageType
        {
            Unknown,
            ControlAlarm,
            ArmDisarmAlarm,
            SendImage,
            SendCamerasList,
            ChangeSourceCamera
        }

        public class ControllerItem
        {
            public string Token { get; set; }
            public TcpClient Client { get; set; }
            public ControllerPermissions Permissions { get; set; }
            public string CurrentCamera { get; set; }

            public ControllerItem(string token, TcpClient client, ControllerPermissions permissions)
            {
                Token = token;
                Client = client;
                Permissions = permissions;
                CurrentCamera = null;
            }
        }

        public class ControllerData
        {
            public ControllerItem Controller { get; set; }
            public byte[] Data { get; set; }

            public ControllerData(ControllerItem controller, byte[] data)
            {
                Controller = controller;
                Data = data;
            }
        }

        public interface Command
        {
            void PerformAction();
        }

        private class MonochromeImageData
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public byte[] Pixels { get; set; }

            public MonochromeImageData()
            {
                Width = 0;
                Height = 0;
                Pixels = null;
            }
        }

        public const string SETTINGS_PATH = "settings.json";
        public const int TOKEN_SIZE = 64;
        public static Object s_lock = new Object();

        static void Main(string[] args)
        {
            new Program().Run(args);
        }

        static void SaveWriteLine(string line)
        {
            lock (s_lock)
            {
                Console.WriteLine(line);
            }
        }

        static double CompareInfraredImages(MonochromeImageData a, MonochromeImageData b, int treshold = 0)
        {
            if (a == null || b == null || a.Pixels == null || b.Pixels == null)
                return 0.0;
            if (a == b)
                return 1.0;
            if (a.Width != b.Width || a.Height != b.Height || a.Pixels.Length != b.Pixels.Length)
                return 0.0;
            var passed = 0;
            for (int i = 0, c = a.Pixels.Length; i < c; ++i)
                if (Math.Abs((int)a.Pixels[i] - (int)b.Pixels[i]) <= treshold)
                    ++passed;
            return (double)passed / (double)a.Pixels.Length;
        }

        public enum ArgMode
        {
            None,
            EyePort,
            ControllerPort,
            Token,
            Treshold,
            ArmedAlarm,
            Silent
        }

        private SettingsModel m_settingsModel;
        private volatile bool m_running = false;
        private Object m_lock = new Object();
        private TcpListener m_eyeListener;
        private TcpListener m_controllerListener;
        private Dictionary<string, TcpClient> m_eyeClients;
        private Dictionary<string, ControllerItem> m_controllerClients;
        private Object m_eyeClientsLock = new Object();
        private Object m_controllerClientsLock = new Object();
        private Queue<byte[]> m_eyeDataQueue;
        private Object m_eyeDataLock = new Object();
        private Queue<ControllerData> m_controllerDataQueue;
        private Object m_controllerDataLock = new Object();
        private Queue<Command> m_commandsQueue;
        private Object m_commandsLock = new Object();
        private IWavePlayer m_waveOutDevice;
        private AudioFileReader m_audioFileReader;
        private Object m_alarmLock = new Object();
        private volatile bool m_alarmIsPlaying = false;
        private volatile bool m_alarmIsArmed = false;

        public void Run(string[] args)
        {
            m_settingsModel = LoadSettings();
            m_running = true;
            m_eyeClients = new Dictionary<string, TcpClient>();
            m_controllerClients = new Dictionary<string, ControllerItem>();
            m_eyeDataQueue = new Queue<byte[]>();
            m_controllerDataQueue = new Queue<ControllerData>();
            m_commandsQueue = new Queue<Command>();
            m_waveOutDevice = new WaveOut();
            m_audioFileReader = new AudioFileReader("alarms/default.mp3");
            m_waveOutDevice.Init(m_audioFileReader);

            var argMode = ArgMode.None;
            foreach (var arg in args)
            {
                if (argMode == ArgMode.None)
                {
                    if (arg == "-ep" || arg == "--eye-port")
                        argMode = ArgMode.EyePort;
                    else if (arg == "-cp" || arg == "--controller-port")
                        argMode = ArgMode.ControllerPort;
                    else if (arg == "-t" || arg == "--token")
                        argMode = ArgMode.Token;
                    else if (arg == "-tr" || arg == "--treshold")
                        argMode = ArgMode.Treshold;
                    else if (arg == "-aa" || arg == "--armed-alarm")
                        argMode = ArgMode.ArmedAlarm;
                    else if (arg == "-s" || arg == "--silent")
                        argMode = ArgMode.Silent;
                }
                else if (argMode == ArgMode.EyePort)
                {
                    var v = m_settingsModel.EyePort;
                    if (int.TryParse(arg, out v))
                        m_settingsModel.EyePort = v;
                    argMode = ArgMode.None;
                }
                else if (argMode == ArgMode.ControllerPort)
                {
                    var v = m_settingsModel.ControllerPort;
                    if (int.TryParse(arg, out v))
                        m_settingsModel.ControllerPort = v;
                    argMode = ArgMode.None;
                }
                else if (argMode == ArgMode.Token)
                {
                    m_settingsModel.Token = arg.Trim();
                    argMode = ArgMode.None;
                }
                else if (argMode == ArgMode.Treshold)
                {
                    var v = m_settingsModel.Treshold;
                    if (int.TryParse(arg, out v))
                        m_settingsModel.Treshold = v;
                    argMode = ArgMode.None;
                }
                else if (argMode == ArgMode.ArmedAlarm)
                {
                    if (arg == "y" || arg == "yes" || arg == "t" || arg == "true")
                        m_settingsModel.ArmedAlarm = true;
                    else if (arg == "n" || arg == "no" || arg == "f" || arg == "false")
                        m_settingsModel.ArmedAlarm = false;
                    argMode = ArgMode.None;
                }
                else if (argMode == ArgMode.Silent)
                {
                    if (arg == "y" || arg == "yes" || arg == "t" || arg == "true")
                        m_settingsModel.Silent = true;
                    else if (arg == "n" || arg == "no" || arg == "f" || arg == "false")
                        m_settingsModel.Silent = false;
                    argMode = ArgMode.None;
                }
            }

            m_alarmIsArmed = m_settingsModel.ArmedAlarm;
            Console.WriteLine("* Connection token: " + m_settingsModel.Token);

            SpawnEyeListenerTask();
            SpawnControllerListenerTask();
            SpawnEyeProcessorTask();
            SpawnControllerProcessorTask();
            SpawnCommandsExecutorTask();

            while (m_running)
            {
                var line = Console.ReadLine();
                if (line == "exit")
                    m_running = false;
                else if (line == "arm")
                    ArmAlarm();
                else if (line == "disarm")
                    DisarmAlarm();
                else if (line == "alarm-on")
                    StartAlarm();
                else if (line == "alarm-off")
                    StopAlarm();
            }

            lock (m_commandsLock)
            {
                if (m_commandsQueue != null)
                {
                    foreach (var command in m_commandsQueue)
                        command.PerformAction();
                    m_commandsQueue.Clear();
                }
            }
            lock (m_eyeClientsLock)
            {
                if (m_eyeClients != null)
                    foreach (var eye in m_eyeClients)
                        eye.Value.Close();
            }
            lock (m_controllerClientsLock)
            {
                if (m_controllerClients != null)
                    foreach (var controller in m_controllerClients)
                        controller.Value.Client.Close();
            }
            while (true)
            {
                lock (m_lock)
                {
                    if (m_eyeListener == null && m_controllerListener == null && m_eyeDataQueue == null && m_controllerDataQueue == null && m_commandsQueue == null)
                        break;
                }
                Thread.Sleep(10);
            }
            m_alarmIsPlaying = false;
            lock (m_alarmLock)
            {
                m_waveOutDevice.Stop();
                m_audioFileReader.Dispose();
                m_audioFileReader = null;
                m_waveOutDevice.Dispose();
                m_waveOutDevice = null;
            }
            m_eyeClients = null;
            m_controllerClients = null;
            SaveSettings(m_settingsModel);
            Thread.Sleep(1000);
        }

        public SettingsModel LoadSettings(string path = SETTINGS_PATH)
        {
            if (!File.Exists(path))
                return new SettingsModel();

            var content = File.ReadAllText(path);
            try
            {
                return JsonConvert.DeserializeObject<SettingsModel>(content);
            }
            catch
            {
                return new SettingsModel();
            }
        }

        public bool SaveSettings(SettingsModel model, string path = SETTINGS_PATH)
        {
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(model, Formatting.Indented));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void PushCommand(Command command)
        {
            lock (m_commandsLock)
            {
                if (m_commandsQueue != null)
                    m_commandsQueue.Enqueue(command);
            }
        }

        private void SpawnEyeListenerTask()
        {
            Task.Run(() =>
            {
                lock (m_lock)
                {
                    try
                    {
                        m_eyeListener = new TcpListener(IPAddress.Any, m_settingsModel.EyePort);
                        m_eyeListener.AllowNatTraversal(true);
                        m_eyeListener.Start();
                    }
                    catch
                    {
                        m_eyeListener = null;
                        return;
                    }
                }
                Program.SaveWriteLine("* Listening for eye clients on port: " + m_settingsModel.EyePort);
                while (m_running)
                {
                    try
                    {
                        var client = m_eyeListener.AcceptTcpClient(new TimeSpan(1000));
                        var tokenBytes = new byte[TOKEN_SIZE];
                        var stream = client.GetStream();
                        var tokenSize = stream.Read(tokenBytes, 0, TOKEN_SIZE);
                        if (tokenSize == TOKEN_SIZE)
                        {
                            var ascii = new ASCIIEncoding();
                            var token = ascii.GetString(tokenBytes);
                            if (token == m_settingsModel.Token)
                            {
                                token = Guid.NewGuid().ToString();
                                if (token.Length < TOKEN_SIZE)
                                    for (int i = token.Length; i < TOKEN_SIZE; ++i)
                                        token += " ";
                                else if (token.Length > TOKEN_SIZE)
                                    token = token.Substring(0, TOKEN_SIZE);
                                lock (m_eyeClientsLock)
                                {
                                    if (m_eyeClients.ContainsKey(token))
                                        stream.WriteByte(0);
                                    else
                                    {
                                        m_eyeClients[token] = client;
                                        tokenBytes = ascii.GetBytes(token);
                                        stream.Write(tokenBytes, 0, tokenBytes.Length);
                                        Program.SaveWriteLine("* Eye connected: " + token);
                                        client.ReceiveTimeout = 1000;
                                        SpawnEyeReceiverTask(token);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                    Thread.Sleep(100);
                }
                lock (m_lock)
                {
                    if (m_eyeListener != null)
                        m_eyeListener.Stop();
                    m_eyeListener = null;
                }
                Program.SaveWriteLine("* Stop listening for eye clients");
            });
        }

        private void SpawnControllerListenerTask()
        {
            Task.Run(() =>
            {
                lock (m_lock)
                {
                    try
                    {
                        m_controllerListener = new TcpListener(IPAddress.Any, m_settingsModel.ControllerPort);
                        m_controllerListener.AllowNatTraversal(true);
                        m_controllerListener.Start();
                    }
                    catch
                    {
                        m_controllerListener = null;
                        return;
                    }
                }
                Program.SaveWriteLine("* Listening for controller clients on port: " + m_settingsModel.ControllerPort);
                while (m_running)
                {
                    try
                    {
                        var client = m_controllerListener.AcceptTcpClient(new TimeSpan(1000));
                        var tokenBytes = new byte[TOKEN_SIZE];
                        var stream = client.GetStream();
                        var tokenSize = stream.Read(tokenBytes, 0, TOKEN_SIZE);
                        if (tokenSize == TOKEN_SIZE)
                        {
                            var ascii = new ASCIIEncoding();
                            var token = ascii.GetString(tokenBytes);
                            if (token == m_settingsModel.Token)
                            {
                                token = Guid.NewGuid().ToString();
                                if (token.Length < TOKEN_SIZE)
                                    for (int i = token.Length; i < TOKEN_SIZE; ++i)
                                        token += " ";
                                else if (token.Length > TOKEN_SIZE)
                                    token = token.Substring(0, TOKEN_SIZE);
                                lock (m_controllerClientsLock)
                                {
                                    if (m_controllerClients.ContainsKey(token))
                                        stream.WriteByte(0);
                                    else
                                    {
                                        var permissionsBytes = new byte[4];
                                        stream.Read(permissionsBytes, 0, 4);
                                        m_controllerClients[token] = new ControllerItem(token, client, (ControllerPermissions)BitConverter.ToInt32(permissionsBytes, 0));
                                        tokenBytes = ascii.GetBytes(token);
                                        stream.Write(tokenBytes, 0, tokenBytes.Length);
                                        Program.SaveWriteLine("* Controller connected: " + token);
                                        client.ReceiveTimeout = 1000;
                                        client.NoDelay = true;
                                        SpawnControllerReceiverTask(token);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                    Thread.Sleep(100);
                }
                lock (m_lock)
                {
                    if (m_controllerListener != null)
                        m_controllerListener.Stop();
                    m_controllerListener = null;
                }
                Program.SaveWriteLine("* Stop listening for controller clients");
            });
        }

        private void SpawnEyeProcessorTask()
        {
            Task.Run(() =>
            {
                MonochromeImageData lastMonochromeImage = null;
                while (m_running)
                {
                    byte[] data = null;
                    lock (m_eyeDataLock)
                    {
                        data = m_eyeDataQueue.Count > 0 ? m_eyeDataQueue.Dequeue() : null;
                    }
                    if (data != null)
                    {
                        var width = BitConverter.ToInt32(data, 0);
                        var height = BitConverter.ToInt32(data, 4);
                        var channels = BitConverter.ToInt32(data, 8);
                        if (width > 0 && height > 0 && channels > 0)
                        {
                            var pixels = new byte[width * height * channels];
                            Buffer.BlockCopy(data, 12, pixels, 0, pixels.Length);
                            if (channels == 1)
                            {
                                int treshold = 0;
                                lock (m_lock)
                                {
                                    treshold = m_settingsModel.Treshold;
                                }
                                var currMonochromeImage = new MonochromeImageData();
                                currMonochromeImage.Width = width;
                                currMonochromeImage.Height = height;
                                currMonochromeImage.Pixels = pixels;
                                if (lastMonochromeImage != null)
                                {
                                    var factor = CompareInfraredImages(currMonochromeImage, lastMonochromeImage, treshold);
                                    if (factor < 1.0 && m_alarmIsArmed && !m_alarmIsPlaying)
                                        PushCommand(new Commands.ControlAlarm(this, true));
                                }
                                lastMonochromeImage = currMonochromeImage;
                            }
                        }
                    }
                    else
                        Thread.Sleep(50);
                }
                lock (m_lock)
                {
                    lock (m_eyeDataLock)
                    {
                        m_eyeDataQueue = null;
                    }
                }
            });
        }

        private void SpawnControllerProcessorTask()
        {
            Task.Run(() =>
            {
                while (m_running)
                {
                    ControllerData data = null;
                    lock (m_controllerDataLock)
                    {
                        data = m_controllerDataQueue.Count > 0 ? m_controllerDataQueue.Dequeue() : null;
                    }
                    if (data != null)
                    {
                        using (var stream = new MemoryStream(data.Data))
                        using (var bs = new BinaryReader(stream))
                        {
                            var type = (MessageType)bs.ReadInt32();
                            if (type == MessageType.ControlAlarm)
                                PushCommand(new Commands.ControlAlarm(this, bs.ReadBoolean()));
                            else if (type == MessageType.ArmDisarmAlarm)
                                PushCommand(new Commands.ArmDisarmAlarm(this, bs.ReadBoolean()));
                            else if (type == MessageType.SendCamerasList)
                                PushCommand(new Commands.ObtainCamerasList(this, data.Controller.Token));
                            else if (type == MessageType.ChangeSourceCamera)
                                PushCommand(new Commands.ChangeSourceCamera(this, data.Controller.Token, new ASCIIEncoding().GetString(bs.ReadBytes(TOKEN_SIZE))));
                        }
                    }
                    else
                        Thread.Sleep(50);
                }
                lock (m_lock)
                {
                    lock (m_controllerDataLock)
                    {
                        m_controllerDataQueue = null;
                    }
                }
            });
        }

        private void SpawnCommandsExecutorTask()
        {
            Task.Run(() =>
            {
                while (m_running)
                {
                    Command command = null;
                    lock (m_commandsLock)
                    {
                        command = m_commandsQueue.Count > 0 ? m_commandsQueue.Dequeue() : null;
                    }
                    if (command != null)
                        command.PerformAction();
                    else
                        Thread.Sleep(10);
                }
                lock (m_lock)
                {
                    lock (m_commandsLock)
                    {
                        m_commandsQueue = null;
                    }
                }
            });
        }

        private void SpawnEyeReceiverTask(string token)
        {
            Task.Run(() =>
            {
                var tb = new byte[TOKEN_SIZE];
                var sb = new byte[4];
                TcpClient c = null;
                lock (m_eyeClientsLock)
                {
                    if (m_eyeClients.ContainsKey(token))
                        c = m_eyeClients[token];
                }
                if (c != null)
                {
                    try
                    {
                        while (m_running && c.Connected)
                        {
                            if (c.Available > 0)
                            {
                                var s = c.GetStream();
                                var ts = s.Read(tb, 0, TOKEN_SIZE);
                                if (ts == TOKEN_SIZE)
                                {
                                    var a = new ASCIIEncoding();
                                    var t = a.GetString(tb);
                                    if (t == token && s.Read(sb, 0, 4) == 4)
                                    {
                                        var mb = new byte[BitConverter.ToInt32(sb, 0)];
                                        s.Read(mb, 0, mb.Length);
                                        lock (m_eyeDataLock)
                                        {
                                            m_eyeDataQueue.Enqueue(mb);
                                        }
                                        PushCommand(new Commands.SendImage(this, token, mb));
                                    }
                                }
                            }
                            else
                                Thread.Sleep(100);
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.SaveWriteLine("* Error occured during processing eye: " + token);
                        Program.SaveWriteLine("* Error message: " + ex.Message);
                    }
                    finally
                    {
                        c.Close();
                        lock (m_eyeClientsLock)
                        {
                            if (m_eyeClients.ContainsKey(token))
                                m_eyeClients.Remove(token);
                        }
                        Program.SaveWriteLine("* Eye disconnected: " + token);
                    }
                }
            });
        }

        private void SpawnControllerReceiverTask(string token)
        {
            Task.Run(() =>
            {
                var tb = new byte[TOKEN_SIZE];
                var sb = new byte[4];
                ControllerItem controller = null;
                TcpClient c = null;
                ControllerPermissions p = ControllerPermissions.None;
                lock (m_controllerClientsLock)
                {
                    if (m_controllerClients.ContainsKey(token))
                    {
                        controller = m_controllerClients[token];
                        c = controller.Client;
                        p = controller.Permissions;
                    }
                }
                if (c != null)
                {
                    try
                    {
                        while (m_running && c.Connected)
                        {
                            if (c.Available > 0)
                            {
                                var s = c.GetStream();
                                var ts = s.Read(tb, 0, TOKEN_SIZE);
                                if (ts == TOKEN_SIZE)
                                {
                                    var a = new ASCIIEncoding();
                                    var t = a.GetString(tb);
                                    if (t == token && s.Read(sb, 0, 4) == 4)
                                    {
                                        var mb = new byte[BitConverter.ToInt32(sb, 0)];
                                        s.Read(mb, 0, mb.Length);
                                        lock (m_controllerDataLock)
                                        {
                                            m_controllerDataQueue.Enqueue(new ControllerData(controller, mb));
                                        }
                                    }
                                }
                            }
                            else
                                Thread.Sleep(100);
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.SaveWriteLine("* Error occured during processing controller: " + token);
                        Program.SaveWriteLine("* Error message: " + ex.Message);
                    }
                    finally
                    {
                        c.Close();
                        lock (m_controllerClientsLock)
                        {
                            if (m_controllerClients.ContainsKey(token))
                                m_controllerClients.Remove(token);
                        }
                        Program.SaveWriteLine("* Controller disconnected: " + token);
                    }
                }
            });
        }

        private void SendAll(MessageType msgType, ControllerPermissions msgPerm, string srcToken, byte[] msgData)
        {
            if (msgType != MessageType.Unknown && msgData != null)
            {
                lock (m_controllerClientsLock)
                {
                    using (var stream = new MemoryStream())
                    using (var bs = new BinaryWriter(stream))
                    {
                        bs.Write((int)msgType);
                        bs.Write(msgData);
                        msgData = stream.GetBuffer();
                        var sb = BitConverter.GetBytes(msgData.Length);
                        foreach (var controller in m_controllerClients)
                        {
                            if (controller.Value.Permissions.HasFlag(msgPerm) && controller.Value.CurrentCamera == srcToken)
                            {
                                try
                                {
                                    var s = controller.Value.Client.GetStream();
                                    s.Write(sb, 0, 4);
                                    s.Write(msgData, 0, msgData.Length);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
        }

        private void Send(MessageType msgType, ControllerPermissions msgPerm, string dstToken, byte[] msgData)
        {
            if (msgType != MessageType.Unknown && msgData != null && dstToken != null)
            {
                lock (m_controllerClientsLock)
                {
                    if (!m_controllerClients.ContainsKey(dstToken))
                        return;
                    ControllerItem controller = m_controllerClients[dstToken];
                    if (controller.Permissions.HasFlag(msgPerm))
                    {
                        using (var stream = new MemoryStream())
                        using (var bs = new BinaryWriter(stream))
                        {
                            bs.Write((int)msgType);
                            bs.Write(msgData);
                            msgData = stream.GetBuffer();
                            var sb = BitConverter.GetBytes(msgData.Length);
                            try
                            {
                                var s = controller.Client.GetStream();
                                s.Write(sb, 0, 4);
                                s.Write(msgData, 0, msgData.Length);
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        internal void StartAlarm()
        {
            SaveWriteLine("* Alarm started!");
            m_alarmIsPlaying = true;
            if (m_settingsModel.Silent)
                return;
            lock (m_alarmLock)
            {
                if (m_waveOutDevice != null)
                    m_waveOutDevice.Play();
            }
            Task.Run(() =>
            {
                while (m_alarmIsPlaying)
                {
                    lock (m_alarmLock)
                    {
                        if (m_waveOutDevice != null && m_waveOutDevice.PlaybackState == PlaybackState.Stopped)
                        {
                            m_audioFileReader.Position = 0;
                            m_waveOutDevice.Play();
                        }
                    }
                    Thread.Sleep(10);
                }
                lock (m_alarmLock)
                {
                    if (m_waveOutDevice != null)
                        m_waveOutDevice.Stop();
                }
            });
        }

        internal void StopAlarm()
        {
            SaveWriteLine("* Alarm stopped!");
            m_alarmIsPlaying = false;
        }

        internal void ArmAlarm()
        {
            SaveWriteLine("* Alarm armed!");
            m_alarmIsArmed = true;
        }

        internal void DisarmAlarm()
        {
            SaveWriteLine("* Alarm disarmed!");
            m_alarmIsArmed = false;
            StopAlarm();
        }

        internal void SendImage(string token, byte[] data)
        {
            SendAll(MessageType.SendImage, ControllerPermissions.ReceiveEyeImages, token, data);
        }

        internal void SendCamerasList(string token)
        {
            lock (m_eyeClientsLock)
            {
                if (m_eyeClients.Count == 0)
                    return;
                var ascii = new ASCIIEncoding();
                using (var stream = new MemoryStream())
                using (var bs = new BinaryWriter(stream))
                {
                    bs.Write(m_eyeClients.Count);
                    foreach (var eye in m_eyeClients)
                        bs.Write(ascii.GetBytes(eye.Key));
                    Send(MessageType.SendCamerasList, ControllerPermissions.ReceiveEyeImages, token, stream.GetBuffer());
                }
            }
        }

        internal void ChangeSourceCamera(string dstToken, string srcToken)
        {
            lock (m_controllerClientsLock)
            {
                if (m_controllerClients.ContainsKey(dstToken))
                    m_controllerClients[dstToken].CurrentCamera = srcToken;
            }
        }
    }
}
