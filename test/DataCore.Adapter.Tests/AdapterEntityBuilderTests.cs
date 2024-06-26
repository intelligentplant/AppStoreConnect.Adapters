using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AdapterEntityBuilderTests : TestsBase {

        private const string TestPropertyName = "X-Test-Property";


        [TestMethod]
        public void ShouldAddProperty() {
            var property = new AdapterProperty(TestPropertyName, TestContext.TestName);
            
            var entity = new TestBuilder()
                .WithProperty(property)
                .Build();
            
            Assert.AreEqual(1, entity.Properties.Count());
            Assert.AreEqual(property, entity.Properties.First());
        }


        [TestMethod]
        public void ShouldReplaceProperty() {
            var property = new AdapterProperty(TestPropertyName, TestContext.TestName);

            var entity = new TestBuilder()
                .WithProperty(TestPropertyName, "Discarded value")
                .WithProperty(property)
                .Build();

            Assert.AreEqual(1, entity.Properties.Count());
            Assert.AreEqual(property, entity.Properties.First());
        }


        [TestMethod]
        public void ShouldNotReplaceProperty() {
            var property = new AdapterProperty(TestPropertyName, TestContext.TestName);

            var entity = new TestBuilder()
                .WithProperty(property)
                .WithProperty(TestPropertyName, "Discarded value", replaceExisting: false)
                .Build();

            Assert.AreEqual(1, entity.Properties.Count());
            Assert.AreEqual(property, entity.Properties.First());
        }


        [TestMethod]
        public void ShouldAddTypedProperty() {
            var entity = new TestBuilder()
                .WithProperty(TestPropertyName, TestContext.TestName)
                .Build();

            Assert.AreEqual(1, entity.Properties.Count());
            
            var propActual = entity.Properties.First();
            Assert.AreEqual(TestPropertyName, propActual.Name);
            Assert.AreEqual(TestContext.TestName, propActual.Value.GetValueOrDefault<string>());
        }


        [TestMethod]
        public void ShouldReplaceTypedProperty() {
            var entity = new TestBuilder()
                .WithProperty(TestPropertyName, "Discarded value")
                .WithProperty(TestPropertyName, TestContext.TestName)
                .Build();

            Assert.AreEqual(1, entity.Properties.Count());

            var propActual = entity.Properties.First();
            Assert.AreEqual(TestPropertyName, propActual.Name);
            Assert.AreEqual(TestContext.TestName, propActual.Value.GetValueOrDefault<string>());
        }


        [TestMethod]
        public void ShouldNotReplaceTypedProperty() {
            var entity = new TestBuilder()
                .WithProperty(TestPropertyName, TestContext.TestName)
                .WithProperty(TestPropertyName, "Discarded value", replaceExisting: false)
                .Build();

            Assert.AreEqual(1, entity.Properties.Count());

            var propActual = entity.Properties.First();
            Assert.AreEqual(TestPropertyName, propActual.Name);
            Assert.AreEqual(TestContext.TestName, propActual.Value.GetValueOrDefault<string>());
        }


        [TestMethod]
        public void ShouldAddVariantProperty() {
            Variant propertyValue = TestContext.TestName;

            var entity = new TestBuilder()
                .WithProperty(TestPropertyName, propertyValue)
                .Build();

            Assert.AreEqual(1, entity.Properties.Count());

            var propActual = entity.Properties.First();
            Assert.AreEqual(TestPropertyName, propActual.Name);
            Assert.AreEqual(propertyValue, propActual.Value);
        }
        

        [TestMethod]
        public void ShouldReplaceVariantProperty() {
            Variant propertyValue1 = "Discarded value";
            Variant propertyValue2 = TestContext.TestName;

            var entity = new TestBuilder()
                .WithProperty(TestPropertyName, propertyValue1)
                .WithProperty(TestPropertyName, propertyValue2)
                .Build();

            Assert.AreEqual(1, entity.Properties.Count());

            var propActual = entity.Properties.First();
            Assert.AreEqual(TestPropertyName, propActual.Name);
            Assert.AreEqual(propertyValue2, propActual.Value);
        }


        [TestMethod]
        public void ShouldNotReplaceVariantProperty() {
            Variant propertyValue1 = "Discarded value";
            Variant propertyValue2 = TestContext.TestName;

            var entity = new TestBuilder()
                .WithProperty(TestPropertyName, propertyValue2)
                .WithProperty(TestPropertyName, propertyValue1, replaceExisting: false)
                .Build();

            Assert.AreEqual(1, entity.Properties.Count());

            var propActual = entity.Properties.First();
            Assert.AreEqual(TestPropertyName, propActual.Name);
            Assert.AreEqual(propertyValue2, propActual.Value);
        }


        [TestMethod]
        public void ShouldRemoveProperty() {
            var property = new AdapterProperty(TestPropertyName, TestContext.TestName);

            var entity = new TestBuilder()
                .WithProperty(property)
                .RemoveProperty(TestPropertyName)
                .Build();

            Assert.AreEqual(0, entity.Properties.Count());
        }


        [TestMethod]
        public void ShouldRemoveAllProperties() {
            var builder = new TestBuilder();

            for (var i = 1; i <= 5; i++) {
                builder = builder.WithProperty(TestPropertyName + "-" + i, TestContext.TestName);
            }

            var entity = builder.ClearProperties().Build();

            Assert.AreEqual(0, entity.Properties.Count());
        }


        private class TestBuilder : AdapterEntityBuilder<TestEntity> {

            public override TestEntity Build() {
                return new TestEntity() {
                    Properties = GetProperties().ToArray()
                };
            }

        }


        private class TestEntity { 
        
            public AdapterProperty[] Properties { get; set; }
        
        }

    }

}
