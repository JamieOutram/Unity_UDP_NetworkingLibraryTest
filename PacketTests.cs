using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnityNetworkingLibrary;
using UnityNetworkingLibrary.Utils;
using UnityNetworkingLibrary.Messages;
using UnityNetworkingLibrary.ExceptionExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UnityNetworkingLibraryTest
{
    using Utils;
    [TestClass]
    public class PacketTests
    {
        const int ExpectedPacketLengthA = Packet.headerSize; //Minimum packet data size
        Packet FakeDataPacketA()
        {
            var ackedBits = new AckBitArray(Packet.ackedBitsLength);
            //Salt is random
            return new Packet(0, 0, ackedBits, PacketType.dataReliable, 12345);
        }

        const int ExpectedPacketLengthB = PacketManager._maxPacketSizeBytes;
        Packet FakeDataPacketB()
        {
            //Engineered to take up all byte space
            List<Message> data = new List<Message>();
            int lengthTotal = 0;
            MessageExample m = new MessageExample(0, "Test12"); //17 byte message for 17*59 = 1003 max size

            while (lengthTotal < PacketManager._maxPacketDataBytes) 
            { 
                data.Add(m);
                lengthTotal += m.Length;
            }

            var ackedBits = new AckBitArray(Packet.ackedBitsLength, 0xFFFFFFFFFFFFFFFF);
            return new Packet(ushort.MaxValue, ushort.MaxValue, ackedBits, PacketType.dataUnreliable, ulong.MaxValue, data.ToArray(), byte.MaxValue);
        }
        Packet FakeDataPacketRandom(Random rand)
        {
            //Random messages
            var messages = new MessageExample[rand.Next(0,30)];
            for(int i = 0; i<messages.Length; i++)
            {
                messages[i] = new MessageExample(rand.Next(int.MinValue, int.MaxValue), RandomExtensions.RandomString(rand.Next(1, 25), rand));
            }
            //Random Id
            ushort id = (ushort)rand.Next(0, ushort.MaxValue);
            ushort ackId = (ushort)rand.Next(0, ushort.MaxValue);
            //Random ackedBits
            var buffer = new byte[Packet.ackedBytesLength];
            rand.NextBytes(buffer);
            var ackedBits = new AckBitArray(buffer);
            //Random ulong for salt
            var buf = new byte[8];
            rand.NextBytes(buf);
            var salt = (ulong)BitConverter.ToInt64(buf, 0);
            return new Packet(id, ackId, ackedBits, PacketType.dataUnreliable, salt, messages, byte.MaxValue);
        }
        Packet FakeEmptyPacket(PacketType type)
        {
            var ackedBits = new AckBitArray(Packet.ackedBitsLength, 0xFFFFFFFFFFFFFFFF);
            return new Packet(0, 0, ackedBits, type, 1);
        }


        [TestMethod]
        public void Get_DataPacketLength_ReturnsPacketSize()
        {
            var packetA = FakeDataPacketA();
            var packetB = FakeDataPacketB();

            Assert.AreEqual(packetA.Length, ExpectedPacketLengthA);
            Assert.AreEqual(packetB.Length, ExpectedPacketLengthB);
        }

        [TestMethod]
        public void Get_ClientConnectionPacketLength_ReturnsMaxPacketSize()
        {
            var connectionRequest = FakeEmptyPacket(PacketType.ClientConnectionRequest);
            var challengeResponse = FakeEmptyPacket(PacketType.ClientChallengeResponse);

            Assert.AreEqual(connectionRequest.Length, PacketManager._maxPacketSizeBytes);
            Assert.AreEqual(challengeResponse.Length, PacketManager._maxPacketSizeBytes);
        }

        [TestMethod]
        public void Decode_RandomSerializedPacket_ReturnsCorrectHeaderAndDataInfo()
        {
            Random rand = new Random(0);
            for (int i = 0; i < 100; i++)
            {
                var randomPacket = FakeDataPacketRandom(rand);
                var expectedData = randomPacket.GetMessages();

                var serialized = randomPacket.Serialize();

                (var decodedHeader, var decodedData) = Packet.Deserialize(serialized);

                Assert.AreEqual(decodedHeader.id, randomPacket.Id);
                Assert.AreEqual(decodedHeader.ackId, randomPacket.AckId);
                Assert.AreEqual(decodedHeader.packetType, randomPacket.Type);
                Assert.AreEqual(decodedHeader.salt, randomPacket.Salt);
                Assert.IsTrue(decodedHeader.ackedBits == randomPacket.AckedBits); 
                //Check data Array
                for (int j = 0; j < expectedData.Length; j++)
                {
                    Assert.AreEqual(decodedData[j].Type, MessageType.MessageExample);
                    MessageExample md = (MessageExample)decodedData[j];
                    MessageExample me = (MessageExample)expectedData[j];
                    Assert.AreEqual(md.IntData, me.IntData);
                    Assert.AreEqual(md.StringData, me.StringData);
                }
            }
        }

        [TestMethod]
        public void Time_100RandomSerializationAndDeserialization_ShortAsPossible()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int runs = 100;
            long total = 0, min = long.MaxValue, max = 0;
            Random rand = new Random(0);
            Packet randomPacket;
            Packet.Header test;
            byte[] serialized;
            Message[] testData;
            long time;
            for (int i = 0; i < runs; i++)
            {
                stopwatch.Restart();
                randomPacket = FakeDataPacketRandom(rand);

                serialized = randomPacket.Serialize();

                (test, testData) = Packet.Deserialize(serialized);

                time = stopwatch.ElapsedTicks * 1000000L / Stopwatch.Frequency;
                max = Math.Max(max, time);
                min = Math.Min(min, time);
                total += time;
                Console.WriteLine("Run Took: " + time + " microseconds");
                
                //Assert to avoid optimization
                Assert.AreEqual(test.id, randomPacket.Id);
                Assert.AreEqual(test.ackId, randomPacket.AckId);
                Assert.AreEqual(test.packetType, randomPacket.Type);
                Assert.AreEqual(test.salt, randomPacket.Salt);
            }
            stopwatch.Stop();
            long avg = total / runs;
            Console.WriteLine("Test Took: " + total + " microseconds");
            Assert.IsTrue(avg < 1000);
            Assert.IsTrue(min < 1000);
            Assert.IsTrue(max < 10000);
        }

    }
}
