using System;
using System.Linq;

using DataCore.Adapter.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class VariantTests : TestsBase {

        [DataTestMethod]
        [DataRow(typeof(bool), typeof(bool[]), typeof(bool[,]), typeof(bool[,,]))]
        [DataRow(typeof(byte), typeof(byte[]), typeof(byte[,]), typeof(byte[,,]))]
        [DataRow(typeof(DateTime), typeof(DateTime[]), typeof(DateTime[,]), typeof(DateTime[,,]))]
        [DataRow(typeof(double), typeof(double[]), typeof(double[,]), typeof(double[,,]))]
        [DataRow(typeof(float), typeof(float[]), typeof(float[,]), typeof(float[,,]))]
        [DataRow(typeof(short), typeof(short[]), typeof(short[,]), typeof(short[,,]))]
        [DataRow(typeof(int), typeof(int[]), typeof(int[,]), typeof(int[,,]))]
        [DataRow(typeof(long), typeof(long[]), typeof(long[,]), typeof(int[,,]))]
        [DataRow(typeof(sbyte), typeof(sbyte[]), typeof(sbyte[,]), typeof(sbyte[,,]))]
        [DataRow(typeof(string), typeof(string[]), typeof(string[,]), typeof(string[,,]))]
        [DataRow(typeof(TimeSpan), typeof(TimeSpan[]), typeof(TimeSpan[,]), typeof(TimeSpan[,,]))]
        [DataRow(typeof(ushort), typeof(ushort[]), typeof(ushort[,]), typeof(ushort[,,]))]
        [DataRow(typeof(uint), typeof(uint[]), typeof(uint[,]), typeof(uint[,,]))]
        [DataRow(typeof(ulong), typeof(ulong[]), typeof(ulong[,]), typeof(ulong[,,]))]
        [DataRow(typeof(Uri), typeof(Uri[]), typeof(Uri[,]), typeof(Uri[,,]))]
        public void VariantTypeShouldBeSupported(params Type[] types) {
            foreach (var type in types) {
                Assert.IsTrue(Variant.IsSupportedValueType(type), $"Type should be supported: {type.Name}");
            }
        }


        private static void ValidateVariant(Variant variant, VariantType expectedType, object expectedValue, int[] expectedArrayDimensions) {
            Assert.AreEqual(expectedType, variant.Type);
            if (expectedValue is Array arr) {
                Assert.IsTrue(expectedArrayDimensions.SequenceEqual(variant.ArrayDimensions));
                Assert.AreEqual(arr.Rank, variant.ArrayDimensions!.Length);

                for (var i = 0; i < arr.Rank; i++) {
                    var length = arr.GetLength(i);
                    Assert.AreEqual(length, variant.ArrayDimensions[i]);
                }

                Assert.IsTrue(arr.Cast<object>().SequenceEqual(((Array) variant.Value).Cast<object>()));
            }
            else {
                Assert.IsNull(variant.ArrayDimensions);
                Assert.AreEqual(expectedValue, variant.Value);
            }
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromBool() {
            bool value = true;
            Variant variant = value;
            ValidateVariant(variant, VariantType.Boolean, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromBoolArray() {
            bool[] value = new[] { true, false };
            Variant variant = value;
            ValidateVariant(variant, VariantType.Boolean, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToBool() {
            bool value = true;
            Variant variant = value;
            var actualValue = (bool) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToBoolArray() {
            bool[] value = new[] { true, false };
            Variant variant = value;
            var actualValue = (bool[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromByte() {
            byte value = 255;
            Variant variant = value;
            ValidateVariant(variant, VariantType.Byte, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromByteArray() {
            byte[] value = new byte [] { 255, 254 };
            Variant variant = value;
            ValidateVariant(variant, VariantType.Byte, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToByte() {
            byte value = 255;
            Variant variant = value;
            var actualValue = (byte) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToByteArray() {
            byte[] value = new byte[] { 255, 254 };
            Variant variant = value;
            var actualValue = (byte[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromDateTime() {
            var value = DateTime.UtcNow;
            Variant variant = value;
            ValidateVariant(variant, VariantType.DateTime, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromDateTimeArray() {
            var value = new DateTime[] { DateTime.UtcNow, DateTime.UtcNow.AddHours(-1) };
            Variant variant = value;
            ValidateVariant(variant, VariantType.DateTime, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToDateTime() {
            var value = DateTime.UtcNow;
            Variant variant = value;
            var actualValue = (DateTime) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToDateTimeArray() {
            var value = new [] { DateTime.UtcNow, DateTime.UtcNow.AddHours(-1) };
            Variant variant = value;
            var actualValue = (DateTime[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromDouble() {
            double value = 1.234;
            Variant variant = value;
            ValidateVariant(variant, VariantType.Double, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromDoubleArray() {
            double[] value = new double[] { 1.234, 5.678 };
            Variant variant = value;
            ValidateVariant(variant, VariantType.Double, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToDouble() {
            double value = 1.234;
            Variant variant = value;
            var actualValue = (double) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToDoubleArray() {
            double[] value = new double[] { 1.234, 5.678 };
            Variant variant = value;
            var actualValue = (double[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromFloat() {
            float value = 1.234f;
            Variant variant = value;
            ValidateVariant(variant, VariantType.Float, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromFloatArray() {
            float[] value = new float[] { 1.234f, 5.678f };
            Variant variant = value;
            ValidateVariant(variant, VariantType.Float, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToFloat() {
            float value = 1.234f;
            Variant variant = value;
            var actualValue = (float) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToFloatArray() {
            float[] value = new float[] { 1.234f, 5.678f };
            Variant variant = value;
            var actualValue = (float[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromInt16() {
            short value = 255;
            Variant variant = value;
            ValidateVariant(variant, VariantType.Int16, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromInt16Array() {
            short[] value = new short[] { 255, 254 };
            Variant variant = value;
            ValidateVariant(variant, VariantType.Int16, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToInt16() {
            short value = 255;
            Variant variant = value;
            var actualValue = (short) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToInt16Array() {
            short[] value = new short[] { 255, 254 };
            Variant variant = value;
            var actualValue = (short[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromInt32() {
            int value = 255;
            Variant variant = value;
            ValidateVariant(variant, VariantType.Int32, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromInt32Array() {
            int[] value = new int[] { 255, 254 };
            Variant variant = value;
            ValidateVariant(variant, VariantType.Int32, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToInt32() {
            int value = 255;
            Variant variant = value;
            var actualValue = (int) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToInt32Array() {
            int[] value = new int[] { 255, 254 };
            Variant variant = value;
            var actualValue = (int[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromInt64() {
            long value = 255;
            Variant variant = value;
            ValidateVariant(variant, VariantType.Int64, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromInt64Array() {
            long[] value = new long[] { 255, 254 };
            Variant variant = value;
            ValidateVariant(variant, VariantType.Int64, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToInt64() {
            long value = 255;
            Variant variant = value;
            var actualValue = (long) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToInt64Array() {
            long[] value = new long[] { 255, 254 };
            Variant variant = value;
            var actualValue = (long[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromSByte() {
            sbyte value = 127;
            Variant variant = value;
            ValidateVariant(variant, VariantType.SByte, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromSByteArray() {
            sbyte[] value = new sbyte[] { -128, 127 };
            Variant variant = value;
            ValidateVariant(variant, VariantType.SByte, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToSByte() {
            sbyte value = 127;
            Variant variant = value;
            var actualValue = (sbyte) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToSByteArray() {
            sbyte[] value = new sbyte[] { 127, -128 };
            Variant variant = value;
            var actualValue = (sbyte[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromString() {
            string value = TestContext.TestName;
            Variant variant = value;
            ValidateVariant(variant, VariantType.String, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromStringArray() {
            string[] value = new string[] { TestContext.TestName, TestContext.FullyQualifiedTestClassName };
            Variant variant = value;
            ValidateVariant(variant, VariantType.String, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToString() {
            var value = TestContext.TestName;
            Variant variant = value;
            var actualValue = (string) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToStringArray() {
            var value = new string[] { TestContext.TestName, TestContext.FullyQualifiedTestClassName };
            Variant variant = value;
            var actualValue = (string[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromTimeSpan() {
            var value = TimeSpan.FromHours(1);
            Variant variant = value;
            ValidateVariant(variant, VariantType.TimeSpan, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromTimeSpanArray() {
            var value = new TimeSpan[] { TimeSpan.FromHours(1), TimeSpan.FromDays(3) };
            Variant variant = value;
            ValidateVariant(variant, VariantType.TimeSpan, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToTimeSpan() {
            var value = TimeSpan.FromHours(1);
            Variant variant = value;
            var actualValue = (TimeSpan) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToTimeSpanArray() {
            var value = new TimeSpan[] { TimeSpan.FromHours(1), TimeSpan.FromDays(3) };
            Variant variant = value;
            var actualValue = (TimeSpan[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromUInt16() {
            ushort value = 255;
            Variant variant = value;
            ValidateVariant(variant, VariantType.UInt16, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromUInt16Array() {
            ushort[] value = new ushort[] { 255, 254 };
            Variant variant = value;
            ValidateVariant(variant, VariantType.UInt16, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToUInt16() {
            ushort value = 255;
            Variant variant = value;
            var actualValue = (ushort) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToUInt16Array() {
            ushort[] value = new ushort[] { 255, 254 };
            Variant variant = value;
            var actualValue = (ushort[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromUInt32() {
            uint value = 255;
            Variant variant = value;
            ValidateVariant(variant, VariantType.UInt32, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromUInt32Array() {
            uint[] value = new uint[] { 255, 254 };
            Variant variant = value;
            ValidateVariant(variant, VariantType.UInt32, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToUInt32() {
            uint value = 255;
            Variant variant = value;
            var actualValue = (uint) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToUInt32Array() {
            uint[] value = new uint[] { 255, 254 };
            Variant variant = value;
            var actualValue = (uint[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromUInt64() {
            ulong value = 255;
            Variant variant = value;
            ValidateVariant(variant, VariantType.UInt64, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromUInt64Array() {
            ulong[] value = new ulong[] { 255, 254 };
            Variant variant = value;
            ValidateVariant(variant, VariantType.UInt64, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToUInt64() {
            ulong value = 255;
            Variant variant = value;
            var actualValue = (ulong) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToUInt64Array() {
            ulong[] value = new ulong[] { 255, 254 };
            Variant variant = value;
            var actualValue = (ulong[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromUri() {
            var value = new Uri("https://appstore.intelligentplant.com");
            Variant variant = value;
            ValidateVariant(variant, VariantType.Url, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowImplicitConversionFromUriArray() {
            var value = new [] { new Uri("https://appstore.intelligentplant.com"), new Uri("https://www.intelligentplant.com") };
            Variant variant = value;
            ValidateVariant(variant, VariantType.Url, value, new[] { value.Length });
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToUri() {
            var value = new Uri("https://appstore.intelligentplant.com");
            Variant variant = value;
            var actualValue = (Uri) variant;
            Assert.AreEqual(value, actualValue);
        }


        [TestMethod]
        public void VariantShouldAllowExplicitConversionToUriArray() {
            var value = new[] { new Uri("https://appstore.intelligentplant.com"), new Uri("https://www.intelligentplant.com") };
            Variant variant = value;
            var actualValue = (Uri[]) variant;
            Assert.IsTrue(value.SequenceEqual(actualValue));
        }


        [DataTestMethod]
        [DataRow(VariantType.Boolean, true)]
        [DataRow(VariantType.Boolean, false)]
        [DataRow(VariantType.Byte, (byte) 0)]
        [DataRow(VariantType.Byte, (byte) 255)]
        [DataRow(VariantType.Double, 1234567890.12345)]
        [DataRow(VariantType.Float, 12345.789f)]
        [DataRow(VariantType.Int16, (short) -32768)]
        [DataRow(VariantType.Int16, (short) 32767)]
        [DataRow(VariantType.Int32, -2147483648)]
        [DataRow(VariantType.Int32, 2147483647)]
        [DataRow(VariantType.Int64, -9223372036854775808)]
        [DataRow(VariantType.Int64, 9223372036854775807)]
        [DataRow(VariantType.Null, null)]
        [DataRow(VariantType.SByte, (sbyte) -128)]
        [DataRow(VariantType.SByte, (sbyte) 127)]
        [DataRow(VariantType.String, "test")]
        [DataRow(VariantType.UInt16, (ushort) 0)]
        [DataRow(VariantType.UInt16, (ushort) 65535)]
        [DataRow(VariantType.UInt32, (uint) 0)]
        [DataRow(VariantType.UInt32, (uint) 4294967295)]
        [DataRow(VariantType.UInt64, (ulong) 0)]
        [DataRow(VariantType.UInt64, (ulong) 18446744073709551615)]
        public void VariantShouldAllowCreationFromObject(VariantType expectedType, object value) {
            var variant = new Variant(value);
            ValidateVariant(variant, expectedType, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowCreationFromDateTimeAsObject() {
            object value = DateTime.UtcNow;
            var variant = new Variant(value);
            ValidateVariant(variant, VariantType.DateTime, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowCreationFromTimeSpanAsObject() {
            object value = TimeSpan.FromSeconds(30);
            var variant = new Variant(value);
            ValidateVariant(variant, VariantType.TimeSpan, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowCreationFromUriAsObject() {
            object value = new Uri("https://appstore.intelligentplant.com");
            var variant = new Variant(value);
            ValidateVariant(variant, VariantType.Url, value, null);
        }


        [TestMethod]
        public void VariantShouldDetect2DArray() {
            var arr = new string[,] {
                { "Intelligent", "Plant", "Limited" },
                { "Industrial", "App", "Store" }
            };

            var variant = new Variant(arr);
            ValidateVariant(variant, VariantType.String, arr, new[] { 2, 3 });
        }


        [TestMethod]
        public void VariantShouldDetect3DArray() {
            var arr = new int[,,] { 
                { 
                    { 1, 2, 3 }, 
                    { 4, 5, 6 } 
                }, 
                { 
                    { 7, 8, 9 }, 
                    { 10, 11, 12 } 
                }, 
                { 
                    { 13, 14, 15 }, 
                    { 16, 17, 18 } 
                }, 
                { 
                    { 19, 20, 21 }, 
                    { 22, 23, 24 } 
                } 
            };

            var variant = new Variant(arr);
            ValidateVariant(variant, VariantType.Int32, arr, new[] { 4, 2, 3 });
        }


        [TestMethod]
        public void VariantShouldDetect4DArray() {
            var arr = new int[,,,] {
                {
                    {
                        { 1, 2, 3 }
                    },
                    {
                        { 4, 5, 6 }
                    }
                },
                {
                    {
                        { 7, 8, 9 }
                    },
                    {
                        { 10, 11, 12 }
                    }
                },
                {
                    {
                        { 13, 14, 15 }
                    },
                    {
                        { 16, 17, 18 }
                    }
                },
                {
                    {
                        { 19, 20, 21 }
                    },
                    {
                        { 22, 23, 24 }
                    }
                }
            };

            var variant = new Variant(arr);
            ValidateVariant(variant, VariantType.Int32, arr, new[] { 4, 2, 1, 3 });
        }


        [TestMethod]
        public void VariantShouldNotAllowCreationWithUnsupportedValueType() {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Variant(new System.Drawing.Point(500, 300)));
        }


        [TestMethod]
        public void VariantShouldNotAllowCreationWithUnsupportedArrayType() {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Variant(new[] { new System.Drawing.Point(500, 300) }));
        }

    }

}
