using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

namespace AegisServer
{
    public static class Extensions
    {
        public static TcpClient AcceptTcpClient(this TcpListener tcpListener, TimeSpan timeout, int pollInterval = 10)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (stopWatch.Elapsed < timeout)
            {
                if (tcpListener.Pending())
                    return tcpListener.AcceptTcpClient();

                Thread.Sleep(pollInterval);
            }
            throw new TimeoutException();
        }
    }
}
