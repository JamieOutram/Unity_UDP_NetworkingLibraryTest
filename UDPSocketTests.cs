using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.Threading;
using UnityNetworkingLibrary;

namespace UnityNetworkingLibraryTest
{
    [TestClass]
    public class UDPSocketTests
    {
        [TestMethod]
        [Timeout(3000)]
        public void SendSelfUnreliableMessage()
        { 
            string message = "Hello World";
            string ip = "127.0.0.1";
            int port = 27000;
            object receiveLock = new object();
            bool messageReceived = false;
            string receivedMessage = "";
            UDPSocket server = new UDPSocket();
            Assert.IsTrue(server.Server(ip, port));
            
            UDPSocket client = new UDPSocket();
            Assert.IsTrue(client.Client(ip, port));

            server.OnReceived += OnReceived;
            client.Send(message);

            //Just doing something to test asyncronus behaviour
            int i = 0;
            while (true)
            {
                i++;
                lock (receiveLock)
                {
                    if (messageReceived)
                        break;
                }
            }

            Assert.AreEqual(receivedMessage, message);

            return;
            
            void OnReceived(byte[] data, int bytesRead)
            {
                lock (receiveLock)
                {
                    receivedMessage = Encoding.ASCII.GetString(data, 0, bytesRead);
                    messageReceived = true;
                }
                
                CleanUp();
            }

            void CleanUp()
            {
                server.OnReceived -= OnReceived;
                server.Socket.Close();
                client.Socket.Close();
            }

        }

    }
}
