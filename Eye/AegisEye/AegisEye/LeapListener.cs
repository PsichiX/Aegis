using Leap;
using System;

namespace AegisEye
{
    public class LeapListener : Listener
    {
        public class ImageData
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public byte[] Pixels { get; set; }

            public ImageData()
            {
                Width = 0;
                Height = 0;
                Pixels = null;
            }

            public byte[] ToBytes()
            {
                if (Width > 0 && Height > 0 && Pixels != null && Pixels.Length > 0)
                {
                    var result = new byte[12 + Pixels.Length];
                    Buffer.BlockCopy(BitConverter.GetBytes(Width), 0, result, 0, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(Height), 0, result, 4, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(1), 0, result, 8, 4);
                    Buffer.BlockCopy(Pixels, 0, result, 12, Pixels.Length);
                    return result;
                }
                return null;
            }
        }

        public ImageData Image { get { lock (m_lock) { return m_imageData; } } }

        private LeapImagesService m_service;
        private Object m_lock = new Object();
        private ImageData m_imageData;

        public LeapListener(LeapImagesService service)
        {
            m_service = service;
        }

        public override void OnInit(Controller controller)
        {
            SafeWriteLine("* Initialized");
        }

        public override void OnExit(Controller controller)
        {
            SafeWriteLine("* Exit");
        }

        public override void OnServiceConnect(Controller controller)
        {
            SafeWriteLine("* Connected to LeapMotion service");
        }

        public override void OnServiceDisconnect(Controller controller)
        {
            SafeWriteLine("* Disconnected from LeapMotion service");
        }

        public override void OnConnect(Controller controller)
        {
            SafeWriteLine("* Connected to LeapMotion");
        }

        public override void OnDisconnect(Controller controller)
        {
            SafeWriteLine("* Disconnected from LeapMotion");
        }

        public override void OnImages(Controller controller)
        {
            lock (m_lock)
            {
                var image = controller.Images[0];
                if (m_imageData == null)
                    m_imageData = new ImageData();
                if (m_imageData.Width != image.Width || m_imageData.Height != image.Height || m_imageData.Pixels == null)
                {
                    m_imageData.Width = image.Width;
                    m_imageData.Height = image.Height;
                    m_imageData.Pixels = new byte[image.Width * image.Height];
                }
                Buffer.BlockCopy(image.Data, 0, m_imageData.Pixels, 0, m_imageData.Pixels.Length);
            }
            m_service.NotifyImageAvailable();
        }



        private void SafeWriteLine(String line)
        {
            lock (m_lock)
            {
                Console.WriteLine(line);
            }
        }
    }
}
