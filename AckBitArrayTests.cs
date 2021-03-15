using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnityNetworkingLibrary.Utils;

namespace UnityNetworkingLibraryTest
{
    [TestClass]
    public class AckBitArrayTests
    {
        [TestMethod]
        public void LeftShiftTest()
        {
            AckBitArray a = new AckBitArray(2, 0xFA74);
            long validate = 0xA740;
            bool[] overflows = a << 4;

            Assert.IsTrue(a.ToLong() == validate);
        }

        [TestMethod]
        public void RightShiftTest()
        {
            AckBitArray a = new AckBitArray(2, 0xFA74);
            long validate = 0x0FA7;
            bool[] overflows = a >> 4;

            Assert.IsTrue(a.ToLong() == validate);
        }

        [TestMethod]
        public void Get_Bit_ReturnsCorrectBool()
        {
            AckBitArray a = new AckBitArray(2, 0x7E18);

            Assert.IsFalse(a[0]);
            Assert.IsFalse(a[1]);
            Assert.IsFalse(a[2]);
            Assert.IsTrue(a[3]);
            
            Assert.IsTrue(a[4]);
            Assert.IsFalse(a[5]);
            Assert.IsFalse(a[6]);
            Assert.IsFalse(a[7]);

            Assert.IsFalse(a[8]);
            Assert.IsTrue(a[9]);
            Assert.IsTrue(a[10]);
            Assert.IsTrue(a[11]);

            Assert.IsTrue(a[12]);
            Assert.IsTrue(a[13]);
            Assert.IsTrue(a[14]);
            Assert.IsFalse(a[15]);
        }

        [TestMethod]
        public void Set_Bit_SetsCorrectBit()
        {
            AckBitArray a = new AckBitArray(2, 0xFF00);
            long validate = 0x7E38;
            a[3] = true;
            a[4] = true;
            a[5] = true;
            a[8] = false;
            a[15] = false;

            Assert.IsTrue(a == validate);
        }

    }
}
