using System;
using System.Collections.Generic;

namespace WPFComputationalGeometry.Source.Models
{
    public class BigInteger
    {
        private const int maxLength = 70;

        public static readonly int[] primesBelow2000 =
        {
            2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97,
            101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199,
            211, 223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293,
            307, 311, 313, 317, 331, 337, 347, 349, 353, 359, 367, 373, 379, 383, 389, 397,
            401, 409, 419, 421, 431, 433, 439, 443, 449, 457, 461, 463, 467, 479, 487, 491, 499,
            503, 509, 521, 523, 541, 547, 557, 563, 569, 571, 577, 587, 593, 599,
            601, 607, 613, 617, 619, 631, 641, 643, 647, 653, 659, 661, 673, 677, 683, 691,
            701, 709, 719, 727, 733, 739, 743, 751, 757, 761, 769, 773, 787, 797,
            809, 811, 821, 823, 827, 829, 839, 853, 857, 859, 863, 877, 881, 883, 887,
            907, 911, 919, 929, 937, 941, 947, 953, 967, 971, 977, 983, 991, 997,
            1009, 1013, 1019, 1021, 1031, 1033, 1039, 1049, 1051, 1061, 1063, 1069, 1087, 1091, 1093, 1097,
            1103, 1109, 1117, 1123, 1129, 1151, 1153, 1163, 1171, 1181, 1187, 1193,
            1201, 1213, 1217, 1223, 1229, 1231, 1237, 1249, 1259, 1277, 1279, 1283, 1289, 1291, 1297,
            1301, 1303, 1307, 1319, 1321, 1327, 1361, 1367, 1373, 1381, 1399,
            1409, 1423, 1427, 1429, 1433, 1439, 1447, 1451, 1453, 1459, 1471, 1481, 1483, 1487, 1489, 1493, 1499,
            1511, 1523, 1531, 1543, 1549, 1553, 1559, 1567, 1571, 1579, 1583, 1597,
            1601, 1607, 1609, 1613, 1619, 1621, 1627, 1637, 1657, 1663, 1667, 1669, 1693, 1697, 1699,
            1709, 1721, 1723, 1733, 1741, 1747, 1753, 1759, 1777, 1783, 1787, 1789,
            1801, 1811, 1823, 1831, 1847, 1861, 1867, 1871, 1873, 1877, 1879, 1889,
            1901, 1907, 1913, 1931, 1933, 1949, 1951, 1973, 1979, 1987, 1993, 1997, 1999
        };


        private readonly uint[] data;
        public int dataLength;

        public BigInteger()
        {
            data = new uint[maxLength];
            dataLength = 1;
        }

        public BigInteger(long value)
        {
            data = new uint[maxLength];
            var tempVal = value;

            dataLength = 0;
            while (value != 0 && dataLength < maxLength)
            {
                data[dataLength] = (uint) (value & 0xFFFFFFFF);
                value >>= 32;
                dataLength++;
            }

            if (tempVal > 0) // overflow check for +ve value
            {
                if (value != 0 || (data[maxLength - 1] & 0x80000000) != 0)
                    throw new ArithmeticException("Positive overflow in constructor.");
            }
            else if (tempVal < 0) // underflow check for -ve value
            {
                if (value != -1 || (data[dataLength - 1] & 0x80000000) == 0)
                    throw new ArithmeticException("Negative underflow in constructor.");
            }

            if (dataLength == 0)
                dataLength = 1;
        }

        public BigInteger(ulong value)
        {
            data = new uint[maxLength];

            dataLength = 0; // copy bytes from ulong to BigInteger without any assumption of the length of the ulong datatype
            while (value != 0 && dataLength < maxLength)
            {
                data[dataLength] = (uint) (value & 0xFFFFFFFF);
                value >>= 32;
                dataLength++;
            }

            if (value != 0 || (data[maxLength - 1] & 0x80000000) != 0)
                throw new ArithmeticException("Positive overflow in constructor.");

            if (dataLength == 0)
                dataLength = 1;
        }

        public BigInteger(BigInteger bi)
        {
            data = new uint[maxLength];
            dataLength = bi.dataLength;
            for (var i = 0; i < dataLength; i++)
                data[i] = bi.data[i];
        }

        public BigInteger(string value, int radix)
        {
            var multiplier = new BigInteger(1);
            var result = new BigInteger();
            value = value.ToUpper().Trim();
            var limit = 0;

            if (value[0] == '-')
                limit = 1;

            for (var i = value.Length - 1; i >= limit; i--)
            {
                var posVal = (int) value[i];

                if (posVal >= '0' && posVal <= '9')
                    posVal -= '0';
                else if (posVal >= 'A' && posVal <= 'Z')
                    posVal = posVal - 'A' + 10;
                else
                    posVal = 9999999; // arbitrary large


                if (posVal >= radix)
                    throw new ArithmeticException("Invalid string in constructor.");

                if (value[0] == '-')
                    posVal = -posVal;

                result += multiplier * posVal;

                if (i - 1 >= limit)
                    multiplier *= radix;
            }

            if (value[0] == '-') // negative values
            {
                if ((result.data[maxLength - 1] & 0x80000000) == 0)
                    throw new ArithmeticException("Negative underflow in constructor.");
            }
            else // positive values
            {
                if ((result.data[maxLength - 1] & 0x80000000) != 0)
                    throw new ArithmeticException("Positive overflow in constructor.");
            }

            data = new uint[maxLength];
            for (var i = 0; i < result.dataLength; i++)
                data[i] = result.data[i];

            dataLength = result.dataLength;
        }

        public BigInteger(IReadOnlyList<byte> inData)
        {
            dataLength = inData.Count >> 2;

            var leftOver = inData.Count & 0x3;
            if (leftOver != 0) // length not multiples of 4
                dataLength++;


            if (dataLength > maxLength)
                throw new ArithmeticException("Byte overflow in constructor.");

            data = new uint[maxLength];

            for (int i = inData.Count - 1, j = 0; i >= 3; i -= 4, j++)
            {
                data[j] = (uint) ((inData[i - 3] << 24) + (inData[i - 2] << 16) +
                                  (inData[i - 1] << 8) + inData[i]);
            }

            if (leftOver == 1)
                data[dataLength - 1] = inData[0];
            else if (leftOver == 2)
                data[dataLength - 1] = (uint) ((inData[0] << 8) + inData[1]);
            else if (leftOver == 3)
                data[dataLength - 1] = (uint) ((inData[0] << 16) + (inData[1] << 8) + inData[2]);


            while (dataLength > 1 && data[dataLength - 1] == 0)
                dataLength--;
        }

        public BigInteger(byte[] inData, int inLen)
        {
            dataLength = inLen >> 2;

            var leftOver = inLen & 0x3;
            if (leftOver != 0) // length not multiples of 4
                dataLength++;

            if (dataLength > maxLength || inLen > inData.Length)
                throw new ArithmeticException("Byte overflow in constructor.");


            data = new uint[maxLength];

            for (int i = inLen - 1, j = 0; i >= 3; i -= 4, j++)
                data[j] = (uint) ((inData[i - 3] << 24) + (inData[i - 2] << 16) +
                                  (inData[i - 1] << 8) + inData[i]);

            if (leftOver == 1)
                data[dataLength - 1] = inData[0];
            else if (leftOver == 2)
                data[dataLength - 1] = (uint) ((inData[0] << 8) + inData[1]);
            else if (leftOver == 3)
                data[dataLength - 1] = (uint) ((inData[0] << 16) + (inData[1] << 8) + inData[2]);


            if (dataLength == 0)
                dataLength = 1;

            while (dataLength > 1 && data[dataLength - 1] == 0)
                dataLength--;
        }

        public BigInteger(IReadOnlyList<uint> inData)
        {
            dataLength = inData.Count;

            if (dataLength > maxLength)
                throw new ArithmeticException("Byte overflow in constructor.");

            data = new uint[maxLength];

            for (int i = dataLength - 1, j = 0; i >= 0; i--, j++)
                data[j] = inData[i];

            while (dataLength > 1 && data[dataLength - 1] == 0)
                dataLength--;
        }

        public static implicit operator BigInteger(long value) => new BigInteger(value);

        public static implicit operator BigInteger(ulong value) => new BigInteger(value);

        public static implicit operator BigInteger(int value) => new BigInteger(value);

        public static implicit operator BigInteger(uint value) => new BigInteger((ulong) value);

        public static BigInteger operator +(BigInteger bi1, BigInteger bi2)
        {
            var result = new BigInteger
            {
                dataLength = bi1.dataLength > bi2.dataLength ? bi1.dataLength : bi2.dataLength
            };

            long carry = 0;
            for (var i = 0; i < result.dataLength; i++)
            {
                var sum = bi1.data[i] + (long) bi2.data[i] + carry;
                carry = sum >> 32;
                result.data[i] = (uint) (sum & 0xFFFFFFFF);
            }

            if (carry != 0 && result.dataLength < maxLength)
            {
                result.data[result.dataLength] = (uint) carry;
                result.dataLength++;
            }

            while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
                result.dataLength--;

            var lastPos = maxLength - 1; // overflow check
            if ((bi1.data[lastPos] & 0x80000000) == (bi2.data[lastPos] & 0x80000000) &&
                (result.data[lastPos] & 0x80000000) != (bi1.data[lastPos] & 0x80000000))
            {
                throw new ArithmeticException();
            }

            return result;
        }

        public static BigInteger operator ++(BigInteger bi1)
        {
            var result = new BigInteger(bi1);

            long val, carry = 1;
            var index = 0;

            while (carry != 0 && index < maxLength)
            {
                val = result.data[index];
                val++;

                result.data[index] = (uint) (val & 0xFFFFFFFF);
                carry = val >> 32;

                index++;
            }

            if (index > result.dataLength)
                result.dataLength = index;
            else
            {
                while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
                    result.dataLength--;
            }

            const int lastPos = maxLength - 1;

            if ((bi1.data[lastPos] & 0x80000000) == 0 &&
                (result.data[lastPos] & 0x80000000) != (bi1.data[lastPos] & 0x80000000))
            {
                throw new ArithmeticException("Overflow in ++.");
            }

            return result;
        }

        public static BigInteger operator -(BigInteger bi1, BigInteger bi2)
        {
            var result = new BigInteger
            {
                dataLength = bi1.dataLength > bi2.dataLength ? bi1.dataLength : bi2.dataLength
            };

            long carryIn = 0;
            for (var i = 0; i < result.dataLength; i++)
            {
                var diff = bi1.data[i] - (long) bi2.data[i] - carryIn;
                result.data[i] = (uint) (diff & 0xFFFFFFFF);

                carryIn = diff < 0 ? 1 : 0;
            }

            if (carryIn != 0) // roll over to negative
            {
                for (var i = result.dataLength; i < maxLength; i++)
                    result.data[i] = 0xFFFFFFFF;
                result.dataLength = maxLength;
            }

            while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0) // fixed in v1.03 to give correct datalength for a - (-b)
                result.dataLength--;

            const int lastPos = maxLength - 1; // overflow check
            if ((bi1.data[lastPos] & 0x80000000) != (bi2.data[lastPos] & 0x80000000) &&
                (result.data[lastPos] & 0x80000000) != (bi1.data[lastPos] & 0x80000000))
            {
                throw new ArithmeticException();
            }

            return result;
        }

        public static BigInteger operator --(BigInteger bi1)
        {
            var result = new BigInteger(bi1);

            var carryIn = true;
            var index = 0;

            while (carryIn && index < maxLength)
            {
                long val = result.data[index];
                val--;

                result.data[index] = (uint) (val & 0xFFFFFFFF);

                if (val >= 0)
                    carryIn = false;

                index++;
            }

            if (index > result.dataLength)
                result.dataLength = index;

            while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
                result.dataLength--;

            const int lastPos = maxLength - 1; // overflow check

            if ((bi1.data[lastPos] & 0x80000000) != 0 && (result.data[lastPos] & 0x80000000) != (bi1.data[lastPos] & 0x80000000)) // overflow if initial value was -ve but -- caused a signchange to positive.
                throw new ArithmeticException("Underflow in --.");

            return result;
        }

        public static BigInteger operator *(BigInteger bi1, BigInteger bi2)
        {
            var lastPos = maxLength - 1;
            bool bi1Neg = false, bi2Neg = false;

            try
            {
                if ((bi1.data[lastPos] & 0x80000000) != 0) // bi1 negative
                {
                    bi1Neg = true;
                    bi1 = -bi1;
                }

                if ((bi2.data[lastPos] & 0x80000000) != 0) // bi2 negative
                {
                    bi2Neg = true;
                    bi2 = -bi2;
                }
            }
            catch (Exception) { }

            var result = new BigInteger();

            try  // multiply the absolute values
            {
                for (var i = 0; i < bi1.dataLength; i++)
                {
                    if (bi1.data[i] == 0) continue;

                    ulong mcarry = 0;
                    for (int j = 0, k = i; j < bi2.dataLength; j++, k++)
                    {
                        // k = i + j
                        var val = bi1.data[i] * (ulong) bi2.data[j] +
                                  result.data[k] + mcarry;

                        result.data[k] = (uint) (val & 0xFFFFFFFF);
                        mcarry = val >> 32;
                    }

                    if (mcarry != 0)
                        result.data[i + bi2.dataLength] = (uint) mcarry;
                }
            }
            catch (Exception)
            {
                throw new ArithmeticException("Multiplication overflow.");
            }


            result.dataLength = bi1.dataLength + bi2.dataLength;
            if (result.dataLength > maxLength)
                result.dataLength = maxLength;

            while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
                result.dataLength--;

            // overflow check (result is -ve)
            if ((result.data[lastPos] & 0x80000000) != 0)
            {
                if (bi1Neg != bi2Neg && result.data[lastPos] == 0x80000000) // different sign
                {
                    // handle the special case where multiplication produces
                    // a max negative number in 2's complement.

                    if (result.dataLength == 1)
                        return result;
                    var isMaxNeg = true;
                    for (var i = 0; i < result.dataLength - 1 && isMaxNeg; i++)
                    {
                        if (result.data[i] != 0)
                            isMaxNeg = false;
                    }

                    if (isMaxNeg)
                        return result;
                }

                throw new ArithmeticException("Multiplication overflow.");
            }

            // if input has different signs, then result is -ve
            if (bi1Neg != bi2Neg)
                return -result;

            return result;
        }

        public static BigInteger operator <<(BigInteger bi1, int shiftVal)
        {
            var result = new BigInteger(bi1);
            result.dataLength = ShiftLeft(result.data, shiftVal);

            return result;
        }

        // least significant bits at lower part of buffer
        private static int ShiftLeft(IList<uint> buffer, int shiftVal)
        {
            var shiftAmount = 32;
            var bufLen = buffer.Count;

            while (bufLen > 1 && buffer[bufLen - 1] == 0)
                bufLen--;

            for (var count = shiftVal; count > 0;)
            {
                if (count < shiftAmount)
                    shiftAmount = count;

                ulong carry = 0;
                for (var i = 0; i < bufLen; i++)
                {
                    var val = (ulong) buffer[i] << shiftAmount;
                    val |= carry;

                    buffer[i] = (uint) (val & 0xFFFFFFFF);
                    carry = val >> 32;
                }

                if (carry != 0)
                {
                    if (bufLen + 1 <= buffer.Count)
                    {
                        buffer[bufLen] = (uint) carry;
                        bufLen++;
                    }
                }

                count -= shiftAmount;
            }

            return bufLen;
        }

        public static BigInteger operator >>(BigInteger bi1, int shiftVal)
        {
            var result = new BigInteger(bi1);
            result.dataLength = ShiftRight(result.data, shiftVal);


            if ((bi1.data[maxLength - 1] & 0x80000000) != 0) // negative
            {
                for (var i = maxLength - 1; i >= result.dataLength; i--)
                    result.data[i] = 0xFFFFFFFF;

                var mask = 0x80000000;
                for (var i = 0; i < 32; i++)
                {
                    if ((result.data[result.dataLength - 1] & mask) != 0)
                        break;

                    result.data[result.dataLength - 1] |= mask;
                    mask >>= 1;
                }

                result.dataLength = maxLength;
            }

            return result;
        }

        private static int ShiftRight(uint[] buffer, int shiftVal)
        {
            var shiftAmount = 32;
            var invShift = 0;
            var bufLen = buffer.Length;

            while (bufLen > 1 && buffer[bufLen - 1] == 0)
                bufLen--;

            for (var count = shiftVal; count > 0;)
            {
                if (count < shiftAmount)
                {
                    shiftAmount = count;
                    invShift = 32 - shiftAmount;
                }

                ulong carry = 0;
                for (var i = bufLen - 1; i >= 0; i--)
                {
                    var val = (ulong) buffer[i] >> shiftAmount;
                    val |= carry;

                    carry = (ulong) buffer[i] << invShift;
                    buffer[i] = (uint) val;
                }

                count -= shiftAmount;
            }

            while (bufLen > 1 && buffer[bufLen - 1] == 0)
                bufLen--;

            return bufLen;
        }

        public static BigInteger operator ~(BigInteger bi1)
        {
            var result = new BigInteger(bi1);

            for (var i = 0; i < maxLength; i++)
                result.data[i] = ~bi1.data[i];

            result.dataLength = maxLength;

            while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
                result.dataLength--;

            return result;
        }

        public static BigInteger operator -(BigInteger bi1)
        {
            // handle neg of zero separately since it'll cause an overflow
            // if we proceed.

            if (bi1.dataLength == 1 && bi1.data[0] == 0)
                return new BigInteger();

            var result = new BigInteger(bi1);

            // 1's complement
            for (var i = 0; i < maxLength; i++)
                result.data[i] = ~bi1.data[i];

            // add one to result of 1's complement
            long val, carry = 1;
            var index = 0;

            while (carry != 0 && index < maxLength)
            {
                val = result.data[index];
                val++;

                result.data[index] = (uint) (val & 0xFFFFFFFF);
                carry = val >> 32;

                index++;
            }

            if ((bi1.data[maxLength - 1] & 0x80000000) == (result.data[maxLength - 1] & 0x80000000))
                throw new ArithmeticException("Overflow in negation.\n");

            result.dataLength = maxLength;

            while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
                result.dataLength--;
            return result;
        }

        public static bool operator ==(BigInteger bi1, BigInteger bi2) => bi1?.Equals(bi2) == true;
        public static bool operator !=(BigInteger bi1, BigInteger bi2) => bi1?.Equals(bi2) != true;

        public override bool Equals(object o)
        {
            var bi = (BigInteger) o;

            if (dataLength != bi.dataLength)
                return false;

            for (var i = 0; i < dataLength; i++)
            {
                if (data[i] != bi.data[i])
                    return false;
            }

            return true;
        }

        public override int GetHashCode() => ToString().GetHashCode();

        public static bool operator >(BigInteger bi1, BigInteger bi2)
        {
            var pos = maxLength - 1;

            // bi1 is negative, bi2 is positive
            if ((bi1.data[pos] & 0x80000000) != 0 && (bi2.data[pos] & 0x80000000) == 0)
                return false;

            // bi1 is positive, bi2 is negative
            if ((bi1.data[pos] & 0x80000000) == 0 && (bi2.data[pos] & 0x80000000) != 0)
                return true;

            // same sign
            var len = bi1.dataLength > bi2.dataLength ? bi1.dataLength : bi2.dataLength;
            for (pos = len - 1; pos >= 0 && bi1.data[pos] == bi2.data[pos]; pos--) ;

            if (pos < 0)
                return false;
            return bi1.data[pos] > bi2.data[pos];
        }


        public static bool operator <(BigInteger bi1, BigInteger bi2)
        {
            var pos = maxLength - 1;

            // bi1 is negative, bi2 is positive
            if ((bi1.data[pos] & 0x80000000) != 0 && (bi2.data[pos] & 0x80000000) == 0)
                return true;

            // bi1 is positive, bi2 is negative
            if ((bi1.data[pos] & 0x80000000) == 0 && (bi2.data[pos] & 0x80000000) != 0)
                return false;

            // same sign
            var len = bi1.dataLength > bi2.dataLength ? bi1.dataLength : bi2.dataLength;
            for (pos = len - 1; pos >= 0 && bi1.data[pos] == bi2.data[pos]; pos--) ;

            if (pos < 0)
                return false;
            return bi1.data[pos] < bi2.data[pos];
        }


        public static bool operator >=(BigInteger bi1, BigInteger bi2) => bi1 == bi2 || bi1 > bi2;
        public static bool operator <=(BigInteger bi1, BigInteger bi2) => bi1 == bi2 || bi1 < bi2;

        private static void MultiByteDivide(BigInteger bi1, BigInteger bi2, BigInteger outQuotient, BigInteger outRemainder)
        {
            var result = new uint[maxLength];

            var remainderLen = bi1.dataLength + 1;
            var remainder = new uint[remainderLen];

            var mask = 0x80000000;
            var val = bi2.data[bi2.dataLength - 1];
            int shift = 0, resultPos = 0;

            while (mask != 0 && (val & mask) == 0)
            {
                shift++;
                mask >>= 1;
            }

            for (var i = 0; i < bi1.dataLength; i++)
                remainder[i] = bi1.data[i];
            ShiftLeft(remainder, shift);
            bi2 <<= shift;

            var j = remainderLen - bi2.dataLength;
            var pos = remainderLen - 1;

            ulong firstDivisorByte = bi2.data[bi2.dataLength - 1];
            ulong secondDivisorByte = bi2.data[bi2.dataLength - 2];

            var divisorLen = bi2.dataLength + 1;
            var dividendPart = new uint[divisorLen];

            while (j > 0)
            {
                var dividend = ((ulong) remainder[pos] << 32) + remainder[pos - 1];
             
                var q_hat = dividend / firstDivisorByte;
                var r_hat = dividend % firstDivisorByte;

                var done = false;
                while (!done)
                {
                    done = true;

                    if (q_hat != 0x100000000 && q_hat * secondDivisorByte <= (r_hat << 32) + remainder[pos - 2])
                        continue;

                    q_hat--;
                    r_hat += firstDivisorByte;

                    if (r_hat < 0x100000000)
                        done = false;
                }

                for (var h = 0; h < divisorLen; h++)
                    dividendPart[h] = remainder[pos - h];

                var kk = new BigInteger(dividendPart);
                var ss = bi2 * (long) q_hat;

                while (ss > kk)
                {
                    q_hat--;
                    ss -= bi2;
                }

                var yy = kk - ss;

                for (var h = 0; h < divisorLen; h++)
                    remainder[pos - h] = yy.data[bi2.dataLength - h];

                result[resultPos++] = (uint) q_hat;

                pos--;
                j--;
            }

            outQuotient.dataLength = resultPos;
            var y = 0;
            for (var x = outQuotient.dataLength - 1; x >= 0; x--, y++)
                outQuotient.data[y] = result[x];
            for (; y < maxLength; y++)
                outQuotient.data[y] = 0;

            while (outQuotient.dataLength > 1 && outQuotient.data[outQuotient.dataLength - 1] == 0)
                outQuotient.dataLength--;

            if (outQuotient.dataLength == 0)
                outQuotient.dataLength = 1;

            outRemainder.dataLength = ShiftRight(remainder, shift);

            for (y = 0; y < outRemainder.dataLength; y++)
                outRemainder.data[y] = remainder[y];
            for (; y < maxLength; y++)
                outRemainder.data[y] = 0;
        }

        private static void singleByteDivide(BigInteger bi1, BigInteger bi2,
            BigInteger outQuotient, BigInteger outRemainder)
        {
            var result = new uint[maxLength];
            var resultPos = 0;

            // copy dividend to reminder
            for (var i = 0; i < maxLength; i++)
                outRemainder.data[i] = bi1.data[i];
            outRemainder.dataLength = bi1.dataLength;

            while (outRemainder.dataLength > 1 && outRemainder.data[outRemainder.dataLength - 1] == 0)
                outRemainder.dataLength--;

            var divisor = (ulong) bi2.data[0];
            var pos = outRemainder.dataLength - 1;
            var dividend = (ulong) outRemainder.data[pos];

            if (dividend >= divisor)
            {
                var quotient = dividend / divisor;
                result[resultPos++] = (uint) quotient;

                outRemainder.data[pos] = (uint) (dividend % divisor);
            }

            pos--;

            while (pos >= 0)
            {
                dividend = ((ulong) outRemainder.data[pos + 1] << 32) + outRemainder.data[pos];
                var quotient = dividend / divisor;
                result[resultPos++] = (uint) quotient;

                outRemainder.data[pos + 1] = 0;
                outRemainder.data[pos--] = (uint) (dividend % divisor);
            }

            outQuotient.dataLength = resultPos;
            var j = 0;
            for (var i = outQuotient.dataLength - 1; i >= 0; i--, j++)
                outQuotient.data[j] = result[i];
            for (; j < maxLength; j++)
                outQuotient.data[j] = 0;

            while (outQuotient.dataLength > 1 && outQuotient.data[outQuotient.dataLength - 1] == 0)
                outQuotient.dataLength--;

            if (outQuotient.dataLength == 0)
                outQuotient.dataLength = 1;

            while (outRemainder.dataLength > 1 && outRemainder.data[outRemainder.dataLength - 1] == 0)
                outRemainder.dataLength--;
        }

        public static BigInteger operator /(BigInteger bi1, BigInteger bi2)
        {
            var quotient = new BigInteger();
            var remainder = new BigInteger();

            var lastPos = maxLength - 1;
            bool divisorNeg = false, dividendNeg = false;

            if ((bi1.data[lastPos] & 0x80000000) != 0) // bi1 negative
            {
                bi1 = -bi1;
                dividendNeg = true;
            }

            if ((bi2.data[lastPos] & 0x80000000) != 0) // bi2 negative
            {
                bi2 = -bi2;
                divisorNeg = true;
            }

            if (bi1 < bi2)
                return quotient;

            if (bi2.dataLength == 1)
                singleByteDivide(bi1, bi2, quotient, remainder);
            else
                MultiByteDivide(bi1, bi2, quotient, remainder);

            if (dividendNeg != divisorNeg)
                return -quotient;

            return quotient;
        }

        public static BigInteger operator %(BigInteger bi1, BigInteger bi2)
        {
            var quotient = new BigInteger();
            var remainder = new BigInteger(bi1);

            var lastPos = maxLength - 1;
            var dividendNeg = false;

            if ((bi1.data[lastPos] & 0x80000000) != 0) // bi1 negative
            {
                bi1 = -bi1;
                dividendNeg = true;
            }

            if ((bi2.data[lastPos] & 0x80000000) != 0) // bi2 negative
                bi2 = -bi2;

            if (bi1 < bi2)
                return remainder;

            if (bi2.dataLength == 1)
                singleByteDivide(bi1, bi2, quotient, remainder);
            else
                MultiByteDivide(bi1, bi2, quotient, remainder);

            if (dividendNeg)
                return -remainder;

            return remainder;
        }

        public static BigInteger operator &(BigInteger bi1, BigInteger bi2)
        {
            var result = new BigInteger();

            var len = bi1.dataLength > bi2.dataLength ? bi1.dataLength : bi2.dataLength;

            for (var i = 0; i < len; i++)
            {
                var sum = bi1.data[i] & bi2.data[i];
                result.data[i] = sum;
            }

            result.dataLength = maxLength;

            while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
                result.dataLength--;

            return result;
        }

        public static BigInteger operator |(BigInteger bi1, BigInteger bi2)
        {
            var result = new BigInteger();

            var len = bi1.dataLength > bi2.dataLength ? bi1.dataLength : bi2.dataLength;

            for (var i = 0; i < len; i++)
            {
                var sum = bi1.data[i] | bi2.data[i];
                result.data[i] = sum;
            }

            result.dataLength = maxLength;

            while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
                result.dataLength--;

            return result;
        }

        public static BigInteger operator ^(BigInteger bi1, BigInteger bi2)
        {
            var result = new BigInteger();

            var len = bi1.dataLength > bi2.dataLength ? bi1.dataLength : bi2.dataLength;

            for (var i = 0; i < len; i++)
            {
                var sum = bi1.data[i] ^ bi2.data[i];
                result.data[i] = sum;
            }

            result.dataLength = maxLength;

            while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
                result.dataLength--;

            return result;
        }

        public BigInteger Max(BigInteger bi) => this > bi ? new BigInteger(this) : new BigInteger(bi);
        public BigInteger Min(BigInteger bi) => this < bi ? new BigInteger(this) : new BigInteger(bi);

        public BigInteger Abs()
        {
            if ((data[maxLength - 1] & 0x80000000) != 0)
                return -this;
            return new BigInteger(this);
        }

        public override string ToString() => ToString(10);

        public string ToString(int radix)
        {
            if (radix < 2 || radix > 36)
                throw new ArgumentException("Radix must be >= 2 and <= 36");

            const string charSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var result = "";

            var a = this;

            var negative = false;
            if ((a.data[maxLength - 1] & 0x80000000) != 0)
            {
                negative = true;
                try
                {
                    a = -a;
                }
                catch (Exception) { }
            }

            var quotient = new BigInteger();
            var remainder = new BigInteger();
            var biRadix = new BigInteger(radix);

            if (a.dataLength == 1 && a.data[0] == 0)
                result = "0";
            else
            {
                while (a.dataLength > 1 || a.dataLength == 1 && a.data[0] != 0)
                {
                    singleByteDivide(a, biRadix, quotient, remainder);

                    if (remainder.data[0] < 10)
                        result = remainder.data[0] + result;
                    else
                        result = charSet[(int) remainder.data[0] - 10] + result;

                    a = quotient;
                }

                if (negative)
                    result = "-" + result;
            }

            return result;
        }

        public string ToHexString()
        {
            var result = data[dataLength - 1].ToString("X");

            for (var i = dataLength - 2; i >= 0; i--)
                result += data[i].ToString("X8");

            return result;
        }

        public BigInteger ModPow(BigInteger exp, BigInteger n)
        {
            if ((exp.data[maxLength - 1] & 0x80000000) != 0)
                throw new ArithmeticException("Positive exponents only.");

            BigInteger resultNum = 1;
            BigInteger tempNum;
            var thisNegative = false;

            if ((data[maxLength - 1] & 0x80000000) != 0) // negative this
            {
                tempNum = -this % n;
                thisNegative = true;
            }
            else
                tempNum = this % n; // ensures (tempNum * tempNum) < b^(2k)

            if ((n.data[maxLength - 1] & 0x80000000) != 0) // negative n
                n = -n;

            // calculate constant = b^(2k) / m
            var constant = new BigInteger();

            var i = n.dataLength << 1;
            constant.data[i] = 0x00000001;
            constant.dataLength = i + 1;

            constant = constant / n;
            var totalBits = exp.bitCount();
            var count = 0;

            // perform squaring and multiply exponentiation
            for (var pos = 0; pos < exp.dataLength; pos++)
            {
                uint mask = 0x01;
                //Console.WriteLine("pos = " + pos);

                for (var index = 0; index < 32; index++)
                {
                    if ((exp.data[pos] & mask) != 0)
                        resultNum = BarrettReduction(resultNum * tempNum, n, constant);

                    mask <<= 1;

                    tempNum = BarrettReduction(tempNum * tempNum, n, constant);


                    if (tempNum.dataLength == 1 && tempNum.data[0] == 1)
                    {
                        if (thisNegative && (exp.data[0] & 0x1) != 0) //odd exp
                            return -resultNum;
                        return resultNum;
                    }

                    count++;
                    if (count == totalBits)
                        break;
                }
            }

            if (thisNegative && (exp.data[0] & 0x1) != 0) //odd exp
                return -resultNum;

            return resultNum;
        }

        private static BigInteger BarrettReduction(BigInteger x, BigInteger n, BigInteger constant)
        {
            int k = n.dataLength,
                kPlusOne = k + 1,
                kMinusOne = k - 1;

            var q1 = new BigInteger();

            // q1 = x / b^(k-1)
            for (int i = kMinusOne, j = 0; i < x.dataLength; i++, j++)
                q1.data[j] = x.data[i];
            q1.dataLength = x.dataLength - kMinusOne;
            if (q1.dataLength <= 0)
                q1.dataLength = 1;


            var q2 = q1 * constant;
            var q3 = new BigInteger();

            // q3 = q2 / b^(k+1)
            for (int i = kPlusOne, j = 0; i < q2.dataLength; i++, j++)
                q3.data[j] = q2.data[i];
            q3.dataLength = q2.dataLength - kPlusOne;
            if (q3.dataLength <= 0)
                q3.dataLength = 1;


            // r1 = x mod b^(k+1)
            // i.e. keep the lowest (k+1) words
            var r1 = new BigInteger();
            var lengthToCopy = x.dataLength > kPlusOne ? kPlusOne : x.dataLength;
            for (var i = 0; i < lengthToCopy; i++)
                r1.data[i] = x.data[i];
            r1.dataLength = lengthToCopy;


            // r2 = (q3 * n) mod b^(k+1)
            // partial multiplication of q3 and n

            var r2 = new BigInteger();
            for (var i = 0; i < q3.dataLength; i++)
            {
                if (q3.data[i] == 0) continue;

                ulong mcarry = 0;
                var t = i;
                for (var j = 0; j < n.dataLength && t < kPlusOne; j++, t++)
                {
                    // t = i + j
                    var val = q3.data[i] * (ulong) n.data[j] +
                              r2.data[t] + mcarry;

                    r2.data[t] = (uint) (val & 0xFFFFFFFF);
                    mcarry = val >> 32;
                }

                if (t < kPlusOne)
                    r2.data[t] = (uint) mcarry;
            }

            r2.dataLength = kPlusOne;
            while (r2.dataLength > 1 && r2.data[r2.dataLength - 1] == 0)
                r2.dataLength--;

            r1 -= r2;
            if ((r1.data[maxLength - 1] & 0x80000000) != 0) // negative
            {
                var val = new BigInteger();
                val.data[kPlusOne] = 0x00000001;
                val.dataLength = kPlusOne + 1;
                r1 += val;
            }

            while (r1 >= n)
                r1 -= n;

            return r1;
        }

        public BigInteger GCD(BigInteger bi)
        {
            BigInteger x;
            BigInteger y;

            if ((data[maxLength - 1] & 0x80000000) != 0) // negative
                x = -this;
            else
                x = this;

            if ((bi.data[maxLength - 1] & 0x80000000) != 0) // negative
                y = -bi;
            else
                y = bi;

            var g = y;

            while (x.dataLength > 1 || x.dataLength == 1 && x.data[0] != 0)
            {
                g = x;
                x = y % x;
                y = g;
            }

            return g;
        }

        public void GenRandomBits(int bits, Random rand)
        {
            var dwords = bits >> 5;
            var remBits = bits & 0x1F;

            if (remBits != 0)
                dwords++;

            if (dwords > maxLength)
                throw new ArithmeticException("Number of required bits > maxLength.");

            for (var i = 0; i < dwords; i++)
                data[i] = (uint) (rand.NextDouble() * 0x100000000);

            for (var i = dwords; i < maxLength; i++)
                data[i] = 0;

            if (remBits != 0)
            {
                var mask = (uint) (0x01 << (remBits - 1));
                data[dwords - 1] |= mask;

                mask = 0xFFFFFFFF >> (32 - remBits);
                data[dwords - 1] &= mask;
            }
            else
                data[dwords - 1] |= 0x80000000;

            dataLength = dwords;

            if (dataLength == 0)
                dataLength = 1;
        }

        public int bitCount()
        {
            while (dataLength > 1 && data[dataLength - 1] == 0)
                dataLength--;

            var value = data[dataLength - 1];
            var mask = 0x80000000;
            var bits = 32;

            while (bits > 0 && (value & mask) == 0)
            {
                bits--;
                mask >>= 1;
            }

            bits += (dataLength - 1) << 5;

            return bits;
        }

        public bool FermatLittleTest(int confidence)
        {
            BigInteger thisVal;
            if ((data[maxLength - 1] & 0x80000000) != 0) // negative
                thisVal = -this;
            else
                thisVal = this;

            if (thisVal.dataLength == 1)
            {
                // test small numbers
                if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
                    return false;
                if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
                    return true;
            }

            if ((thisVal.data[0] & 0x1) == 0) // even numbers
                return false;

            var bits = thisVal.bitCount();
            var a = new BigInteger();
            var p_sub1 = thisVal - new BigInteger(1);
            var rand = new Random();

            for (var round = 0; round < confidence; round++)
            {
                var done = false;

                while (!done) // generate a < n
                {
                    var testBits = 0;

                    // make sure "a" has at least 2 bits
                    while (testBits < 2)
                        testBits = (int) (rand.NextDouble() * bits);

                    a.GenRandomBits(testBits, rand);

                    var byteLen = a.dataLength;

                    // make sure "a" is not 0
                    if (byteLen > 1 || byteLen == 1 && a.data[0] != 1)
                        done = true;
                }

                // check whether a factor exists (fix for version 1.03)
                var gcdTest = a.GCD(thisVal);
                if (gcdTest.dataLength == 1 && gcdTest.data[0] != 1)
                    return false;

                // calculate a^(p-1) mod p
                var expResult = a.ModPow(p_sub1, thisVal);

                var resultLen = expResult.dataLength;

                // is NOT prime is a^(p-1) mod p != 1

                if (resultLen > 1 || resultLen == 1 && expResult.data[0] != 1)
                {
                    //Console.WriteLine("a = " + a.ToString());
                    return false;
                }
            }

            return true;
        }

        public bool RabinMillerTest(int confidence)
        {
            BigInteger thisVal;
            if ((data[maxLength - 1] & 0x80000000) != 0) // negative
                thisVal = -this;
            else
                thisVal = this;

            if (thisVal.dataLength == 1)
            {
                // test small numbers
                if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
                    return false;
                if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
                    return true;
            }

            if ((thisVal.data[0] & 0x1) == 0) // even numbers
                return false;


            // calculate values of s and t
            var p_sub1 = thisVal - new BigInteger(1);
            var s = 0;

            for (var index = 0; index < p_sub1.dataLength; index++)
            {
                uint mask = 0x01;

                for (var i = 0; i < 32; i++)
                {
                    if ((p_sub1.data[index] & mask) != 0)
                    {
                        index = p_sub1.dataLength; // to break the outer loop
                        break;
                    }

                    mask <<= 1;
                    s++;
                }
            }

            var t = p_sub1 >> s;

            var bits = thisVal.bitCount();
            var a = new BigInteger();
            var rand = new Random();

            for (var round = 0; round < confidence; round++)
            {
                var done = false;

                while (!done) // generate a < n
                {
                    var testBits = 0;

                    // make sure "a" has at least 2 bits
                    while (testBits < 2)
                        testBits = (int) (rand.NextDouble() * bits);

                    a.GenRandomBits(testBits, rand);

                    var byteLen = a.dataLength;

                    // make sure "a" is not 0
                    if (byteLen > 1 || byteLen == 1 && a.data[0] != 1)
                        done = true;
                }

                // check whether a factor exists (fix for version 1.03)
                var gcdTest = a.GCD(thisVal);
                if (gcdTest.dataLength == 1 && gcdTest.data[0] != 1)
                    return false;

                var b = a.ModPow(t, thisVal);

                var result = b.dataLength == 1 && b.data[0] == 1;

                for (var j = 0; result == false && j < s; j++)
                {
                    if (b == p_sub1) // a^((2^j)*t) mod p = p-1 for some 0 <= j <= s-1
                    {
                        result = true;
                        break;
                    }

                    b = b * b % thisVal;
                }

                if (result == false)
                    return false;
            }

            return true;
        }

        public bool SolovayStrassenTest(int confidence)
        {
            BigInteger thisVal;
            if ((data[maxLength - 1] & 0x80000000) != 0) // negative
                thisVal = -this;
            else
                thisVal = this;

            if (thisVal.dataLength == 1)
            {
                // test small numbers
                if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
                    return false;
                if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
                    return true;
            }

            if ((thisVal.data[0] & 0x1) == 0) // even numbers
                return false;


            var bits = thisVal.bitCount();
            var a = new BigInteger();
            var p_sub1 = thisVal - 1;
            var p_sub1_shift = p_sub1 >> 1;

            var rand = new Random();

            for (var round = 0; round < confidence; round++)
            {
                var done = false;

                while (!done) // generate a < n
                {
                    var testBits = 0;

                    // make sure "a" has at least 2 bits
                    while (testBits < 2)
                        testBits = (int) (rand.NextDouble() * bits);

                    a.GenRandomBits(testBits, rand);

                    var byteLen = a.dataLength;

                    // make sure "a" is not 0
                    if (byteLen > 1 || byteLen == 1 && a.data[0] != 1)
                        done = true;
                }

                // check whether a factor exists (fix for version 1.03)
                var gcdTest = a.GCD(thisVal);
                if (gcdTest.dataLength == 1 && gcdTest.data[0] != 1)
                    return false;

                // calculate a^((p-1)/2) mod p

                var expResult = a.ModPow(p_sub1_shift, thisVal);
                if (expResult == p_sub1)
                    expResult = -1;

                // calculate Jacobi symbol
                BigInteger jacob = Jacobi(a, thisVal);

                // if they are different then it is not prime
                if (expResult != jacob)
                    return false;
            }

            return true;
        }

        public bool LucasStrongTest()
        {
            BigInteger thisVal;
            if ((data[maxLength - 1] & 0x80000000) != 0) // negative
                thisVal = -this;
            else
                thisVal = this;

            if (thisVal.dataLength == 1)
            {
                // test small numbers
                if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
                    return false;
                if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
                    return true;
            }

            if ((thisVal.data[0] & 0x1) == 0) // even numbers
                return false;

            return LucasStrongTestHelper(thisVal);
        }


        private bool LucasStrongTestHelper(BigInteger thisVal)
        {
            // Do the test (selects D based on Selfridge)
            // Let D be the first element of the sequence
            // 5, -7, 9, -11, 13, ... for which J(D,n) = -1
            // Let P = 1, Q = (1-D) / 4

            long D = 5, sign = -1, dCount = 0;
            var done = false;

            while (!done)
            {
                var Jresult = Jacobi(D, thisVal);

                if (Jresult == -1)
                    done = true; // J(D, this) = 1
                else
                {
                    if (Jresult == 0 && Math.Abs(D) < thisVal) // divisor found
                        return false;

                    if (dCount == 20)
                    {
                        // check for square
                        var root = thisVal.Sqrt();
                        if (root * root == thisVal)
                            return false;
                    }

                    D = (Math.Abs(D) + 2) * sign;
                    sign = -sign;
                }

                dCount++;
            }

            var Q = (1 - D) >> 2;

            var p_add1 = thisVal + 1;
            var s = 0;

            for (var index = 0; index < p_add1.dataLength; index++)
            {
                uint mask = 0x01;

                for (var i = 0; i < 32; i++)
                {
                    if ((p_add1.data[index] & mask) != 0)
                    {
                        index = p_add1.dataLength; // to break the outer loop
                        break;
                    }

                    mask <<= 1;
                    s++;
                }
            }

            var t = p_add1 >> s;

            // calculate constant = b^(2k) / m
            // for Barrett Reduction
            var constant = new BigInteger();

            var nLen = thisVal.dataLength << 1;
            constant.data[nLen] = 0x00000001;
            constant.dataLength = nLen + 1;

            constant = constant / thisVal;

            var lucas = LucasSequenceHelper(1, Q, t, thisVal, constant, 0);
            var isPrime = lucas[0].dataLength == 1 && lucas[0].data[0] == 0 || lucas[1].dataLength == 1 && lucas[1].data[0] == 0;

            for (var i = 1; i < s; i++)
            {
                if (!isPrime)
                {
                    // doubling of index
                    lucas[1] = BarrettReduction(lucas[1] * lucas[1], thisVal, constant);
                    lucas[1] = (lucas[1] - (lucas[2] << 1)) % thisVal;

                    //lucas[1] = ((lucas[1] * lucas[1]) - (lucas[2] << 1)) % thisVal;

                    if (lucas[1].dataLength == 1 && lucas[1].data[0] == 0)
                        isPrime = true;
                }

                lucas[2] = BarrettReduction(lucas[2] * lucas[2], thisVal, constant); //Q^k
            }


            if (isPrime) // additional checks for composite numbers
            {
                // If n is prime and gcd(n, Q) == 1, then
                // Q^((n+1)/2) = Q * Q^((n-1)/2) is congruent to (Q * J(Q, n)) mod n

                var g = thisVal.GCD(Q);
                if (g.dataLength == 1 && g.data[0] == 1) // gcd(this, Q) == 1
                {
                    if ((lucas[2].data[maxLength - 1] & 0x80000000) != 0)
                        lucas[2] += thisVal;

                    var temp = Q * Jacobi(Q, thisVal) % thisVal;
                    if ((temp.data[maxLength - 1] & 0x80000000) != 0)
                        temp += thisVal;

                    if (lucas[2] != temp)
                        isPrime = false;
                }
            }

            return isPrime;
        }

        public bool IsProbablePrime(int confidence)
        {
            BigInteger thisVal;
            if ((data[maxLength - 1] & 0x80000000) != 0) // negative
                thisVal = -this;
            else
                thisVal = this;


            // test for divisibility by primes < 2000
            foreach (var divisor in primesBelow2000)
            {
                if (divisor >= thisVal)
                    break;

                var resultNum = thisVal % divisor;
                if (resultNum.IntValue() == 0)
                    return false;
            }

            return thisVal.RabinMillerTest(confidence);
        }

        public bool IsProbablePrime()
        {
            BigInteger thisVal;
            if ((data[maxLength - 1] & 0x80000000) != 0) // negative
                thisVal = -this;
            else
                thisVal = this;

            if (thisVal.dataLength == 1)
            {
                // test small numbers
                if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
                    return false;
                if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
                    return true;
            }

            if ((thisVal.data[0] & 0x1) == 0) // even numbers
                return false;


            // test for divisibility by primes < 2000
            foreach (BigInteger divisor in primesBelow2000)
            {
                if (divisor >= thisVal)
                    break;

                var resultNum = thisVal % divisor;
                if (resultNum.IntValue() == 0)
                    return false;
            }

            // Perform BASE 2 Rabin-Miller Test

            // calculate values of s and t
            var p_sub1 = thisVal - new BigInteger(1);
            var s = 0;

            for (var index = 0; index < p_sub1.dataLength; index++)
            {
                uint mask = 0x01;

                for (var i = 0; i < 32; i++)
                {
                    if ((p_sub1.data[index] & mask) != 0)
                    {
                        index = p_sub1.dataLength; // to break the outer loop
                        break;
                    }

                    mask <<= 1;
                    s++;
                }
            }

            var t = p_sub1 >> s;

            var bits = thisVal.bitCount();
            BigInteger a = 2;

            // b = a^t mod p
            var b = a.ModPow(t, thisVal);
            var result = b.dataLength == 1 && b.data[0] == 1;

            for (var j = 0; result == false && j < s; j++)
            {
                if (b == p_sub1) // a^((2^j)*t) mod p = p-1 for some 0 <= j <= s-1
                {
                    result = true;
                    break;
                }

                b = b * b % thisVal;
            }

            // if number is strong pseudoprime to base 2, then do a strong lucas test
            if (result)
                result = LucasStrongTestHelper(thisVal);

            return result;
        }

        public int IntValue() => (int) data[0];

        public long LongValue()
        {
            long val = data[0];
            try
            {
                // exception if maxLength = 1
                val |= (long) data[1] << 32;
            }
            catch (Exception)
            {
                if ((data[0] & 0x80000000) != 0) // negative
                    val = (int) data[0];
            }

            return val;
        }

        public static int Jacobi(BigInteger a, BigInteger b)
        {
            // Jacobi defined only for odd integers
            if ((b.data[0] & 0x1) == 0)
                throw new ArgumentException("Jacobi defined only for odd integers.");

            if (a >= b) a %= b;
            if (a.dataLength == 1 && a.data[0] == 0) return 0; // a == 0
            if (a.dataLength == 1 && a.data[0] == 1) return 1; // a == 1

            if (a < 0)
            {
                if (((b - 1).data[0] & 0x2) == 0) //if( (((b-1) >> 1).data[0] & 0x1) == 0)
                    return Jacobi(-a, b);
                return -Jacobi(-a, b);
            }

            var e = 0;
            for (var index = 0; index < a.dataLength; index++)
            {
                uint mask = 0x01;

                for (var i = 0; i < 32; i++)
                {
                    if ((a.data[index] & mask) != 0)
                    {
                        index = a.dataLength; // to break the outer loop
                        break;
                    }

                    mask <<= 1;
                    e++;
                }
            }

            var a1 = a >> e;

            var s = 1;
            if ((e & 0x1) != 0 && ((b.data[0] & 0x7) == 3 || (b.data[0] & 0x7) == 5))
                s = -1;

            if ((b.data[0] & 0x3) == 3 && (a1.data[0] & 0x3) == 3)
                s = -s;

            if (a1.dataLength == 1 && a1.data[0] == 1)
                return s;
            return s * Jacobi(b % a1, a1);
        }

        public static BigInteger GenPseudoPrime(int bits, int confidence, Random rand)
        {
            var result = new BigInteger();
            var done = false;

            while (!done)
            {
                result.GenRandomBits(bits, rand);
                result.data[0] |= 0x01; // make it odd

                // prime test
                done = result.IsProbablePrime(confidence);
            }

            return result;
        }

        public BigInteger GenCoPrime(int bits, Random rand)
        {
            var done = false;
            var result = new BigInteger();

            while (!done)
            {
                result.GenRandomBits(bits, rand);

                // gcd test
                var g = result.GCD(this);
                if (g.dataLength == 1 && g.data[0] == 1)
                    done = true;
            }

            return result;
        }

        public BigInteger ModInverse(BigInteger modulus)
        {
            BigInteger[] p = { 0, 1 };
            var q = new BigInteger[2]; // quotients
            BigInteger[] r = { 0, 0 }; // remainders

            var step = 0;

            var a = modulus;
            var b = this;

            while (b.dataLength > 1 || b.dataLength == 1 && b.data[0] != 0)
            {
                var quotient = new BigInteger();
                var remainder = new BigInteger();

                if (step > 1)
                {
                    var pval = (p[0] - p[1] * q[0]) % modulus;
                    p[0] = p[1];
                    p[1] = pval;
                }

                if (b.dataLength == 1)
                    singleByteDivide(a, b, quotient, remainder);
                else
                    MultiByteDivide(a, b, quotient, remainder);

                q[0] = q[1];
                r[0] = r[1];
                q[1] = quotient;
                r[1] = remainder;

                a = b;
                b = remainder;

                step++;
            }

            if (r[0].dataLength > 1 || r[0].dataLength == 1 && r[0].data[0] != 1)
                throw new ArithmeticException("No inverse!");

            var result = (p[0] - p[1] * q[0]) % modulus;

            if ((result.data[maxLength - 1] & 0x80000000) != 0)
                result += modulus; // get the least positive modulus

            return result;
        }

        public byte[] GetBytes()
        {
            var numBits = bitCount();

            var numBytes = numBits >> 3;
            if ((numBits & 0x7) != 0)
                numBytes++;

            var result = new byte[numBytes];

            var pos = 0;
            uint tempVal, val = data[dataLength - 1];

            if ((tempVal = (val >> 24) & 0xFF) != 0)
                result[pos++] = (byte) tempVal;
            if ((tempVal = (val >> 16) & 0xFF) != 0)
                result[pos++] = (byte) tempVal;
            if ((tempVal = (val >> 8) & 0xFF) != 0)
                result[pos++] = (byte) tempVal;
            if ((tempVal = val & 0xFF) != 0)
                result[pos++] = (byte) tempVal;

            for (var i = dataLength - 2; i >= 0; i--, pos += 4)
            {
                val = data[i];
                result[pos + 3] = (byte) (val & 0xFF);
                val >>= 8;
                result[pos + 2] = (byte) (val & 0xFF);
                val >>= 8;
                result[pos + 1] = (byte) (val & 0xFF);
                val >>= 8;
                result[pos] = (byte) (val & 0xFF);
            }

            return result;
        }

        public void SetBit(uint bitNum)
        {
            var bytePos = bitNum >> 5; // divide by 32
            var bitPos = (byte) (bitNum & 0x1F); // get the lowest 5 bits

            var mask = (uint) 1 << bitPos;
            data[bytePos] |= mask;

            if (bytePos >= dataLength)
                dataLength = (int) bytePos + 1;
        }

        public void UnsetBit(uint bitNum)
        {
            var bytePos = bitNum >> 5;

            if (bytePos < dataLength)
            {
                var bitPos = (byte) (bitNum & 0x1F);

                var mask = (uint) 1 << bitPos;
                var mask2 = 0xFFFFFFFF ^ mask;

                data[bytePos] &= mask2;

                if (dataLength > 1 && data[dataLength - 1] == 0)
                    dataLength--;
            }
        }

        public BigInteger Sqrt()
        {
            var numBits = (uint) bitCount();

            if ((numBits & 0x1) != 0) // odd number of bits
                numBits = (numBits >> 1) + 1;
            else
                numBits = numBits >> 1;

            var bytePos = numBits >> 5;
            var bitPos = (byte) (numBits & 0x1F);

            uint mask;

            var result = new BigInteger();
            if (bitPos == 0)
                mask = 0x80000000;
            else
            {
                mask = (uint) 1 << bitPos;
                bytePos++;
            }

            result.dataLength = (int) bytePos;

            for (var i = (int) bytePos - 1; i >= 0; i--)
            {
                while (mask != 0)
                {
                    // guess
                    result.data[i] ^= mask;

                    // undo the guess if its square is larger than this
                    if (result * result > this)
                        result.data[i] ^= mask;

                    mask >>= 1;
                }

                mask = 0x80000000;
            }

            return result;
        }

        public static BigInteger[] LucasSequence(BigInteger P, BigInteger Q,
            BigInteger k, BigInteger n)
        {
            if (k.dataLength == 1 && k.data[0] == 0)
            {
                var result = new BigInteger[3];

                result[0] = 0;
                result[1] = 2 % n;
                result[2] = 1 % n;
                return result;
            }

            // calculate constant = b^(2k) / m
            // for Barrett Reduction
            var constant = new BigInteger();

            var nLen = n.dataLength << 1;
            constant.data[nLen] = 0x00000001;
            constant.dataLength = nLen + 1;

            constant = constant / n;

            // calculate values of s and t
            var s = 0;

            for (var index = 0; index < k.dataLength; index++)
            {
                uint mask = 0x01;

                for (var i = 0; i < 32; i++)
                {
                    if ((k.data[index] & mask) != 0)
                    {
                        index = k.dataLength; // to break the outer loop
                        break;
                    }

                    mask <<= 1;
                    s++;
                }
            }

            var t = k >> s;

            return LucasSequenceHelper(P, Q, t, n, constant, s);
        }

        private static BigInteger[] LucasSequenceHelper(BigInteger P, BigInteger Q,
            BigInteger k, BigInteger n,
            BigInteger constant, int s)
        {
            var result = new BigInteger[3];

            if ((k.data[0] & 0x00000001) == 0)
                throw new ArgumentException("Argument k must be odd.");

            var numbits = k.bitCount();
            var mask = (uint) 0x1 << ((numbits & 0x1F) - 1);

            // v = v0, v1 = v1, u1 = u1, Q_k = Q^0

            BigInteger v = 2 % n,
                Q_k = 1 % n,
                v1 = P % n,
                u1 = Q_k;
            var flag = true;

            for (var i = k.dataLength - 1; i >= 0; i--) // iterate on the binary expansion of k
            {
                while (mask != 0)
                {
                    if (i == 0 && mask == 0x00000001) // last bit
                        break;

                    if ((k.data[i] & mask) != 0) // bit is set
                    {
                        // index doubling with addition

                        u1 = u1 * v1 % n;

                        v = (v * v1 - P * Q_k) % n;
                        v1 = BarrettReduction(v1 * v1, n, constant);
                        v1 = (v1 - ((Q_k * Q) << 1)) % n;

                        if (flag)
                            flag = false;
                        else
                            Q_k = BarrettReduction(Q_k * Q_k, n, constant);

                        Q_k = Q_k * Q % n;
                    }
                    else
                    {
                        // index doubling
                        u1 = (u1 * v - Q_k) % n;

                        v1 = (v * v1 - P * Q_k) % n;
                        v = BarrettReduction(v * v, n, constant);
                        v = (v - (Q_k << 1)) % n;

                        if (flag)
                        {
                            Q_k = Q % n;
                            flag = false;
                        }
                        else
                            Q_k = BarrettReduction(Q_k * Q_k, n, constant);
                    }

                    mask >>= 1;
                }

                mask = 0x80000000;
            }

            // at this point u1 = u(n+1) and v = v(n)
            // since the last bit always 1, we need to transform u1 to u(2n+1) and v to v(2n+1)

            u1 = (u1 * v - Q_k) % n;
            v = (v * v1 - P * Q_k) % n;
            if (flag)
                flag = false;
            else
                Q_k = BarrettReduction(Q_k * Q_k, n, constant);

            Q_k = Q_k * Q % n;


            for (var i = 0; i < s; i++)
            {
                // index doubling
                u1 = u1 * v % n;
                v = (v * v - (Q_k << 1)) % n;

                Q_k = BarrettReduction(Q_k * Q_k, n, constant);
            }

            result[0] = u1;
            result[1] = v;
            result[2] = Q_k;

            return result;
        }
    }
}
