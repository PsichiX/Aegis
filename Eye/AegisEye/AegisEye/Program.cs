using System;
using System.Threading;

namespace AegisEye
{
    class Program
    {
        public enum ArgMode
        {
            None,
            ImageSource,
            ReceiverAddress,
            ReceiverPort,
            Token,
            Interval
        }

        public enum ImageSource
        {
            None,
            WebCam,
            LeapMotion
        }

        static void Main(string[] args)
        {
            var argMode = ArgMode.None;
            var imageSource = ImageSource.LeapMotion;
            var receiverAddress = "localhost";
            var receiverPort = 8081;
            var token = "";
            var interval = 100;

            foreach (var arg in args)
            {
                if (argMode == ArgMode.None)
                {
                    if (arg == "-is" || arg == "--image-source")
                        argMode = ArgMode.ImageSource;
                    else if (arg == "-ra" || arg == "--receiver-address")
                        argMode = ArgMode.ReceiverAddress;
                    else if (arg == "-rp" || arg == "--receiver-port")
                        argMode = ArgMode.ReceiverPort;
                    else if (arg == "-t" || arg == "--token")
                        argMode = ArgMode.Token;
                    else if (arg == "-i" || arg == "--interval")
                        argMode = ArgMode.Interval;
                }
                else if (argMode == ArgMode.ImageSource)
                {
                    if (arg == "webcam")
                        imageSource = ImageSource.WebCam;
                    else if (arg == "leap")
                        imageSource = ImageSource.LeapMotion;
                    argMode = ArgMode.None;
                }
                else if (argMode == ArgMode.ReceiverAddress)
                {
                    receiverAddress = arg;
                    argMode = ArgMode.None;
                }
                else if (argMode == ArgMode.ReceiverPort)
                {
                    int v = receiverPort;
                    if (int.TryParse(arg, out v))
                        receiverPort = v;
                    argMode = ArgMode.None;
                }
                else if (argMode == ArgMode.Token)
                {
                    token = arg;
                    argMode = ArgMode.None;
                }
                else if (argMode == ArgMode.Interval)
                {
                    int v = interval;
                    if (int.TryParse(arg, out v))
                        interval = v;
                    argMode = ArgMode.None;
                }
            }

            ImagesService imagesService = null;
            if (imageSource == ImageSource.LeapMotion)
                imagesService = new LeapImagesService();
            if (imagesService != null)
            {
                var communication = new CommunicationService();
                Console.WriteLine("* Connectiong to server..");
                if (communication.Connect(receiverAddress, receiverPort, token))
                {
                    Console.WriteLine("* Connected to server!");
                    imagesService.OnInitialize(communication, interval);
                    while (true)
                    {
                        var line = Console.ReadLine();
                        if (line == "exit")
                            break;
                    }
                    imagesService.OnRelease();
                }
                else
                    Console.WriteLine("* Cannot connect to server!");
                communication.Disconnect();
                Thread.Sleep(1000);
            }
        }
    }
}
