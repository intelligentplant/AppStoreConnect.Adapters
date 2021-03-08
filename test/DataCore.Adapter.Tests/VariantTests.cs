using System;
using System.Linq;

using DataCore.Adapter.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class VariantTests : TestsBase {

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
        public void VariantShouldAllowCreationWithSupportedType(VariantType expectedType, object value) {
            var variant = new Variant(value);
            ValidateVariant(variant, expectedType, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowCreationWithDateTime() {
            var value = DateTime.UtcNow;
            var variant = new Variant(value);
            ValidateVariant(variant, VariantType.DateTime, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowCreationWithTimeSpan() {
            var value = TimeSpan.FromSeconds(30);
            var variant = new Variant(value);
            ValidateVariant(variant, VariantType.TimeSpan, value, null);
        }


        [TestMethod]
        public void VariantShouldAllowCreationWithUri() {
            var value = new Uri("https://appstore.intelligentplant.com");
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
