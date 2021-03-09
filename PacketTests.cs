using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnityNetworkingLibrary;
using UnityNetworkingLibrary.ExceptionExtensions;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;

namespace UnityNetworkingLibraryTest
{
    [TestClass]
    class PacketTests
    {
        const int ExpectedPacketLengthA = Packet.headerSize + 1; //Minimum packet data size is 1
        Packet FakeDataPacketA()
        {
            var ackedBits = new BitArray(Packet.ackedBitsLength);
            ackedBits.SetAll(false);
            //Salt is random
            return new Packet(0, ackedBits, PacketType.dataReliable, 12345);
        }

        const int ExpectedPacketLengthB = PacketManager._maxPacketSizeBytes;
        Packet FakeDataPacketB()
        {
            byte[] data = new byte[PacketManager._maxPacketSizeBytes];
            var ackedBits = new BitArray(Packet.ackedBitsLength);
            ackedBits.SetAll(true);
            return new Packet(ushort.MaxValue, ackedBits, PacketType.dataUnreliable, 1, data,byte.MaxValue);
        }
        Packet FakeDataPacketRandom(Random rand)
        {
            //Random data
            byte[] data = new byte[rand.Next(1,PacketManager._maxPacketDataBytes)];
            rand.NextBytes(data);
            //Random Id
            ushort id = (ushort)rand.Next(0, ushort.MaxValue);
            //Random ackedBits
            var buffer = new byte[Packet.ackedBytesLength];
            rand.NextBytes(buffer);
            var ackedBits = new BitArray(buffer);
            //Random ulong for salt
            var buf = new byte[8];
            rand.NextBytes(buf);
            var salt = (ulong)BitConverter.ToInt64(buf, 0);
            return new Packet(id, ackedBits, PacketType.dataUnreliable, salt, data, byte.MaxValue);
        }
        Packet FakeEmptyPacket(PacketType type)
        {
            var ackedBits = new BitArray(Packet.ackedBitsLength);
            ackedBits.SetAll(true);
            return new Packet(0, ackedBits, type, 1);
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
            for(int i=0; i<10; i++)
            {
                var randomPacket = FakeDataPacketRandom(rand);
                var expectedHeader = new Packet.Header(randomPacket.Id, randomPacket.AckedBits, randomPacket.Type, randomPacket.Salt);
                var expectedData = randomPacket.GetMessageData();

                var serialized = randomPacket.Serialize();

                (var decodedHeader, var decodedData) = Packet.Decode(serialized);

                Assert.AreEqual(decodedHeader.id, expectedHeader.id);
                Assert.AreEqual(decodedHeader.packetType, expectedHeader.packetType);
                Assert.AreEqual(decodedHeader.salt, expectedHeader.salt);
                Assert.IsTrue(decodedHeader.ackedBits.Xor(expectedHeader.ackedBits).OfType<bool>().All(e=>!e)); //TODO: Slow as hell, might want to not use bit arrays
                //Check data Array
                for(int j = 0; j<expectedData.Length; j++)
                {
                    Assert.AreEqual(decodedData[i], expectedData[i]);
                }
            }
        }

        [TestMethod]
        public void Time_RandomSerialization_ShortAsPossible()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int runs = 10;
            long total=0, min=long.MaxValue, max = 0;
            Random rand = new Random(0);
            for (int i = 0; i < runs; i++)
            {
                stopwatch.Restart();
                var randomPacket = FakeDataPacketRandom(rand);

                var serialized = randomPacket.Serialize();

                (_, _) = Packet.Decode(serialized);

                long time = stopwatch.ElapsedMilliseconds;
                max = Math.Max(max, time);
                min = Math.Min(min, time);
                total += time;
            }
            stopwatch.Stop();
            long avg = total/runs;
            Console.WriteLine("Test Took: " + total + " milliseconds");
            Assert.IsTrue(avg < 100);
            Assert.IsTrue(min < 100);
            Assert.IsTrue(max < 150);
        }

        [TestMethod]
        public void Time_RandomDecode_ShortAsPossible()
        {
            Stopwatch stopwatch = new Stopwatch();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Time_MaxSizeSerialization_ShortAsPossible()
        {
            Stopwatch stopwatch = new Stopwatch();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Time_MaxSizeDecode_ShortAsPossible()
        {
            Stopwatch stopwatch = new Stopwatch();
            throw new NotImplementedException();
        }

    }
}
