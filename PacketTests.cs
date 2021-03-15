using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnityNetworkingLibrary;
using UnityNetworkingLibrary.Utils;
using UnityNetworkingLibrary.ExceptionExtensions;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;

namespace UnityNetworkingLibraryTest
{
    [TestClass]
    public class PacketTests
    {
        const int ExpectedPacketLengthA = Packet.headerSize + 1; //Minimum packet data size is 1
        Packet FakeDataPacketA()
        {
            var ackedBits = new AckBitArray(Packet.ackedBitsLength);
            //Salt is random
            return new Packet(0, 0, ackedBits, PacketType.dataReliable, 12345);
        }

        const int ExpectedPacketLengthB = PacketManager._maxPacketSizeBytes;
        Packet FakeDataPacketB()
        {
            byte[] data = new byte[PacketManager._maxPacketDataBytes];
            var ackedBits = new AckBitArray(Packet.ackedBitsLength, 0xFFFFFFFFFFFFFFFF);
            return new Packet(ushort.MaxValue, ushort.MaxValue, ackedBits, PacketType.dataUnreliable, ulong.MaxValue, data, byte.MaxValue);
        }
        Packet FakeDataPacketRandom(Random rand)
        {
            //Random data
            byte[] data = new byte[rand.Next(1, PacketManager._maxPacketDataBytes)];
            rand.NextBytes(data);
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
            return new Packet(id, ackId, ackedBits, PacketType.dataUnreliable, salt, data, byte.MaxValue);
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
            for (int i = 0; i < 10; i++)
            {
                var randomPacket = FakeDataPacketRandom(rand);
                var expectedData = randomPacket.GetMessageData();

                var serialized = randomPacket.Serialize();

                (var decodedHeader, var decodedData) = Packet.Decode(serialized);

                Assert.AreEqual(decodedHeader.id, randomPacket.Id);
                Assert.AreEqual(decodedHeader.ackId, randomPacket.AckId);
                Assert.AreEqual(decodedHeader.packetType, randomPacket.Type);
                Assert.AreEqual(decodedHeader.salt, randomPacket.Salt);
                Assert.IsTrue(decodedHeader.ackedBits == randomPacket.AckedBits); 
                //Check data Array
                for (int j = 0; j < expectedData.Length; j++)
                {
                    Assert.AreEqual(decodedData[i], expectedData[i]);
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
            byte[] serialized, testData;
            long time;
            for (int i = 0; i < runs; i++)
            {
                stopwatch.Restart();
                randomPacket = FakeDataPacketRandom(rand);

                serialized = randomPacket.Serialize();

                (test, testData) = Packet.Decode(serialized);

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
            Assert.IsTrue(max < 7000);
        }

    }
}
