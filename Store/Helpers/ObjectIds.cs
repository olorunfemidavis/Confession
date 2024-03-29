﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.System.Diagnostics;

namespace Store.Helpers
{
    public struct ObjectIds : IComparable<ObjectIds>, IEquatable<ObjectIds>, IConvertible
    {
        // private static fields
        private static readonly ObjectIds __emptyInstance = default(ObjectIds);
        private static readonly int __staticMachine = (GetMachineHash() + GetAppDomainId()) & 0x00ffffff;
        private static readonly short __staticPid = GetPid();
        private static int __staticIncrement = (new Random()).Next();

        // private fields
        private readonly int _a;
        private readonly int _b;
        private readonly int _c;

        // constructors
        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public ObjectIds(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (bytes.Length != 12)
            {
                throw new ArgumentException("Byte array must be 12 bytes long", "bytes");
            }

            FromByteArray(bytes, 0, out _a, out _b, out _c);
        }

        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="index">The index into the byte array where the ObjectId starts.</param>
        internal ObjectIds(byte[] bytes, int index)
        {
            FromByteArray(bytes, index, out _a, out _b, out _c);
        }

        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="timestamp">The timestamp (expressed as a DateTime).</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public ObjectIds(DateTime timestamp, int machine, short pid, int increment)
            : this(GetTimestampFromDateTime(timestamp), machine, pid, increment)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public ObjectIds(int timestamp, int machine, short pid, int increment)
        {
            if ((machine & 0xff000000) != 0)
            {
                throw new ArgumentOutOfRangeException("machine", "The machine value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }
            if ((increment & 0xff000000) != 0)
            {
                throw new ArgumentOutOfRangeException("increment", "The increment value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }

            _a = timestamp;
            _b = (machine << 8) | ((pid >> 8) & 0xff);
            _c = (pid << 24) | increment;
        }

        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="value">The value.</param>
        public ObjectIds(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            byte[] bytes = BsonUtil.ParseHexString(value);
            FromByteArray(bytes, 0, out _a, out _b, out _c);
        }

        // public static properties
        /// <summary>
        /// Gets an instance of ObjectId where the value is empty.
        /// </summary>
        public static ObjectIds Empty => __emptyInstance;

        // public properties
        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public int Timestamp => _a;

        /// <summary>
        /// Gets the machine.
        /// </summary>
        public int Machine => (_b >> 8) & 0xffffff;

        /// <summary>
        /// Gets the PID.
        /// </summary>
        public short Pid => (short)(((_b << 8) & 0xff00) | ((_c >> 24) & 0x00ff));

        /// <summary>
        /// Gets the increment.
        /// </summary>
        public int Increment => _c & 0xffffff;

        /// <summary>
        /// Gets the creation time (derived from the timestamp).
        /// </summary>
        public DateTime CreationTime => BsonConstant.UnixEpoch.AddSeconds(Timestamp);

        // public operators
        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is less than the second ObjectId.</returns>
        public static bool operator <(ObjectIds lhs, ObjectIds rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is less than or equal to the second ObjectId.</returns>
        public static bool operator <=(ObjectIds lhs, ObjectIds rhs)
        {
            return lhs.CompareTo(rhs) <= 0;
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId.</param>
        /// <returns>True if the two ObjectIds are equal.</returns>
        public static bool operator ==(ObjectIds lhs, ObjectIds rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId.</param>
        /// <returns>True if the two ObjectIds are not equal.</returns>
        public static bool operator !=(ObjectIds lhs, ObjectIds rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is greather than or equal to the second ObjectId.</returns>
        public static bool operator >=(ObjectIds lhs, ObjectIds rhs)
        {
            return lhs.CompareTo(rhs) >= 0;
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is greather than the second ObjectId.</returns>
        public static bool operator >(ObjectIds lhs, ObjectIds rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        // public static methods
        /// <summary>
        /// Generates a new ObjectId with a unique value.
        /// </summary>
        /// <returns>An ObjectId.</returns>
        public static ObjectIds GenerateNewId()
        {
            return GenerateNewId(GetTimestampFromDateTime(DateTime.UtcNow));
        }

        /// <summary>
        /// Generates a new ObjectId with a unique value (with the timestamp component based on a given DateTime).
        /// </summary>
        /// <param name="timestamp">The timestamp component (expressed as a DateTime).</param>
        /// <returns>An ObjectId.</returns>
        public static ObjectIds GenerateNewId(DateTime timestamp)
        {
            return GenerateNewId(GetTimestampFromDateTime(timestamp));
        }

        /// <summary>
        /// Generates a new ObjectId with a unique value (with the given timestamp).
        /// </summary>
        /// <param name="timestamp">The timestamp component.</param>
        /// <returns>An ObjectId.</returns>
        public static ObjectIds GenerateNewId(int timestamp)
        {
            int increment = Interlocked.Increment(ref __staticIncrement) & 0x00ffffff; // only use low order 3 bytes
            return new ObjectIds(timestamp, __staticMachine, __staticPid, increment);
        }

        /// <summary>
        /// Packs the components of an ObjectId into a byte array.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        /// <returns>A byte array.</returns>
        public static byte[] Pack(int timestamp, int machine, short pid, int increment)
        {
            if ((machine & 0xff000000) != 0)
            {
                throw new ArgumentOutOfRangeException("machine", "The machine value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }
            if ((increment & 0xff000000) != 0)
            {
                throw new ArgumentOutOfRangeException("increment", "The increment value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }

            byte[] bytes = new byte[12];
            bytes[0] = (byte)(timestamp >> 24);
            bytes[1] = (byte)(timestamp >> 16);
            bytes[2] = (byte)(timestamp >> 8);
            bytes[3] = (byte)(timestamp);
            bytes[4] = (byte)(machine >> 16);
            bytes[5] = (byte)(machine >> 8);
            bytes[6] = (byte)(machine);
            bytes[7] = (byte)(pid >> 8);
            bytes[8] = (byte)(pid);
            bytes[9] = (byte)(increment >> 16);
            bytes[10] = (byte)(increment >> 8);
            bytes[11] = (byte)(increment);
            return bytes;
        }

        /// <summary>
        /// Parses a string and creates a new ObjectId.
        /// </summary>
        /// <param name="s">The string value.</param>
        /// <returns>A ObjectId.</returns>
        public static ObjectIds Parse(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            if (TryParse(s, out ObjectIds objectId))
            {
                return objectId;
            }
            else
            {
                string message = string.Format("'{0}' is not a valid 24 digit hex string.", s);
                throw new FormatException(message);
            }
        }

        /// <summary>
        /// Tries to parse a string and create a new ObjectId.
        /// </summary>
        /// <param name="s">The string value.</param>
        /// <param name="objectId">The new ObjectId.</param>
        /// <returns>True if the string was parsed successfully.</returns>
        public static bool TryParse(string s, out ObjectIds objectId)
        {
            // don't throw ArgumentNullException if s is null
            if (s != null && s.Length == 24)
            {
                if (BsonUtil.TryParseHexString(s, out byte[] bytes))
                {
                    objectId = new ObjectIds(bytes);
                    return true;
                }
            }

            objectId = default(ObjectIds);
            return false;
        }

        /// <summary>
        /// Unpacks a byte array into the components of an ObjectId.
        /// </summary>
        /// <param name="bytes">A byte array.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public static void Unpack(byte[] bytes, out int timestamp, out int machine, out short pid, out int increment)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (bytes.Length != 12)
            {
                throw new ArgumentOutOfRangeException("bytes", "Byte array must be 12 bytes long.");
            }

            timestamp = (bytes[0] << 24) + (bytes[1] << 16) + (bytes[2] << 8) + bytes[3];
            machine = (bytes[4] << 16) + (bytes[5] << 8) + bytes[6];
            pid = (short)((bytes[7] << 8) + bytes[8]);
            increment = (bytes[9] << 16) + (bytes[10] << 8) + bytes[11];
        }

        // private static methods
        private static int GetAppDomainId()
        {
#if NETSTANDARD1_5 || NETSTANDARD1_6
            return 1;
#else
            uint processId = ProcessDiagnosticInfo.GetForCurrentProcess().ProcessId;
            return Convert.ToInt32(processId);
#endif
        }

        /// <summary>
        /// Gets the current process id.  This method exists because of how CAS operates on the call stack, checking
        /// for permissions before executing the method.  Hence, if we inlined this call, the calling method would not execute
        /// before throwing an exception requiring the try/catch at an even higher level that we don't necessarily control.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int GetCurrentProcessId()
        {
            uint processId = ProcessDiagnosticInfo.GetForCurrentProcess().ProcessId;
            return Convert.ToInt32(processId);
        }

        private static int GetMachineHash()
        {
            // use instead of Dns.HostName so it will work offline
            string machineName = GetMachineName();
            return 0x00ffffff & machineName.GetHashCode(); // use first 3 bytes of hash
        }

        private static string GetMachineName()
        {
            IReadOnlyList<HostName> hostNames = NetworkInformation.GetHostNames();
            string hostName = hostNames.FirstOrDefault(name => name.Type == HostNameType.DomainName)?.DisplayName ?? "???";
            return hostName;
        }

        private static short GetPid()
        {
            try
            {
                return (short)GetCurrentProcessId(); // use low order two bytes only
            }
            catch (SecurityException)
            {
                return 0;
            }
        }

        private static int GetTimestampFromDateTime(DateTime timestamp)
        {
            long secondsSinceEpoch = (long)Math.Floor((BsonUtil.ToUniversalTime(timestamp) - BsonConstant.UnixEpoch).TotalSeconds);
            if (secondsSinceEpoch < int.MinValue || secondsSinceEpoch > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("timestamp");
            }
            return (int)secondsSinceEpoch;
        }

        private static void FromByteArray(byte[] bytes, int offset, out int a, out int b, out int c)
        {
            a = (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
            b = (bytes[offset + 4] << 24) | (bytes[offset + 5] << 16) | (bytes[offset + 6] << 8) | bytes[offset + 7];
            c = (bytes[offset + 8] << 24) | (bytes[offset + 9] << 16) | (bytes[offset + 10] << 8) | bytes[offset + 11];
        }

        // public methods
        /// <summary>
        /// Compares this ObjectId to another ObjectId.
        /// </summary>
        /// <param name="other">The other ObjectId.</param>
        /// <returns>A 32-bit signed integer that indicates whether this ObjectId is less than, equal to, or greather than the other.</returns>
        public int CompareTo(ObjectIds other)
        {
            int result = ((uint)_a).CompareTo((uint)other._a);
            if (result != 0) { return result; }
            result = ((uint)_b).CompareTo((uint)other._b);
            if (result != 0) { return result; }
            return ((uint)_c).CompareTo((uint)other._c);
        }

        /// <summary>
        /// Compares this ObjectId to another ObjectId.
        /// </summary>
        /// <param name="rhs">The other ObjectId.</param>
        /// <returns>True if the two ObjectIds are equal.</returns>
        public bool Equals(ObjectIds rhs)
        {
            return
                _a == rhs._a &&
                _b == rhs._b &&
                _c == rhs._c;
        }

        /// <summary>
        /// Compares this ObjectId to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is an ObjectId and equal to this one.</returns>
        public override bool Equals(object obj)
        {
            if (obj is ObjectIds)
            {
                return Equals((ObjectIds)obj);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = 37 * hash + _a.GetHashCode();
            hash = 37 * hash + _b.GetHashCode();
            hash = 37 * hash + _c.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Converts the ObjectId to a byte array.
        /// </summary>
        /// <returns>A byte array.</returns>
        public byte[] ToByteArray()
        {
            byte[] bytes = new byte[12];
            ToByteArray(bytes, 0);
            return bytes;
        }

        /// <summary>
        /// Converts the ObjectId to a byte array.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="offset">The offset.</param>
        public void ToByteArray(byte[] destination, int offset)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            if (offset + 12 > destination.Length)
            {
                throw new ArgumentException("Not enough room in destination buffer.", "offset");
            }

            destination[offset + 0] = (byte)(_a >> 24);
            destination[offset + 1] = (byte)(_a >> 16);
            destination[offset + 2] = (byte)(_a >> 8);
            destination[offset + 3] = (byte)(_a);
            destination[offset + 4] = (byte)(_b >> 24);
            destination[offset + 5] = (byte)(_b >> 16);
            destination[offset + 6] = (byte)(_b >> 8);
            destination[offset + 7] = (byte)(_b);
            destination[offset + 8] = (byte)(_c >> 24);
            destination[offset + 9] = (byte)(_c >> 16);
            destination[offset + 10] = (byte)(_c >> 8);
            destination[offset + 11] = (byte)(_c);
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public override string ToString()
        {
            char[] c = new char[24];
            c[0] = BsonUtil.ToHexChar((_a >> 28) & 0x0f);
            c[1] = BsonUtil.ToHexChar((_a >> 24) & 0x0f);
            c[2] = BsonUtil.ToHexChar((_a >> 20) & 0x0f);
            c[3] = BsonUtil.ToHexChar((_a >> 16) & 0x0f);
            c[4] = BsonUtil.ToHexChar((_a >> 12) & 0x0f);
            c[5] = BsonUtil.ToHexChar((_a >> 8) & 0x0f);
            c[6] = BsonUtil.ToHexChar((_a >> 4) & 0x0f);
            c[7] = BsonUtil.ToHexChar(_a & 0x0f);
            c[8] = BsonUtil.ToHexChar((_b >> 28) & 0x0f);
            c[9] = BsonUtil.ToHexChar((_b >> 24) & 0x0f);
            c[10] = BsonUtil.ToHexChar((_b >> 20) & 0x0f);
            c[11] = BsonUtil.ToHexChar((_b >> 16) & 0x0f);
            c[12] = BsonUtil.ToHexChar((_b >> 12) & 0x0f);
            c[13] = BsonUtil.ToHexChar((_b >> 8) & 0x0f);
            c[14] = BsonUtil.ToHexChar((_b >> 4) & 0x0f);
            c[15] = BsonUtil.ToHexChar(_b & 0x0f);
            c[16] = BsonUtil.ToHexChar((_c >> 28) & 0x0f);
            c[17] = BsonUtil.ToHexChar((_c >> 24) & 0x0f);
            c[18] = BsonUtil.ToHexChar((_c >> 20) & 0x0f);
            c[19] = BsonUtil.ToHexChar((_c >> 16) & 0x0f);
            c[20] = BsonUtil.ToHexChar((_c >> 12) & 0x0f);
            c[21] = BsonUtil.ToHexChar((_c >> 8) & 0x0f);
            c[22] = BsonUtil.ToHexChar((_c >> 4) & 0x0f);
            c[23] = BsonUtil.ToHexChar(_c & 0x0f);
            return new string(c);
        }

        // explicit IConvertible implementation
        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return ToString();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            return null;
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }
    }
}
