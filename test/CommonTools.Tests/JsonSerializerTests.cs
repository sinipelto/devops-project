using CommonTools.Utils;
using NUnit.Framework;

namespace CommonTools.Tests
{
    public class JsonSerializerTests
    {
        private class TestObj
        {
            public Hello Hello { get; set; }

            public int Sum { get; set; }
        }

        private class Hello
        {
            public string Value { get; set; }

            public bool BoolValue { get; set; }

            public bool? NullableBool { get; set; }
        }

        [Test]
        public void Test_DeserializeFromJsonString_ValidValues()
        {
            const string json = "{ \"hello\": {\"value\": \"WorlD\"} }";

            var obj = json.Deserialize<TestObj>();

            Assert.IsInstanceOf<TestObj>(obj);
            Assert.Zero(obj.Sum);
            Assert.AreEqual("WorlD", obj.Hello.Value);
            Assert.AreEqual(false, obj.Hello.BoolValue);
            Assert.AreEqual(null, obj.Hello.NullableBool);
        }

        [Test]
        public void Test_DeserializeFromInvalidJsonString_InvalidCasing()
        {
            const string json = "{ \"heLLO\": {\"value\": \"WorlD\"} }";

            var obj = json.Deserialize<TestObj>();

            Assert.IsInstanceOf<TestObj>(obj);
            Assert.IsInstanceOf<Hello>(obj.Hello);
            Assert.Zero(obj.Sum);
            Assert.AreEqual("WorlD", obj.Hello.Value);
            Assert.AreEqual(false, obj.Hello.BoolValue);
            Assert.AreEqual(null, obj.Hello.NullableBool);
        }

        [Test]
        public void Test_DeserializeFromInvalidJsonString_InvalidCasing2()
        {
            const string json = "{ \"hello\": {\"vAlUe\": \"WorlD\"} }";

            var obj = json.Deserialize<TestObj>();


            Assert.IsInstanceOf<TestObj>(obj);
            Assert.Zero(obj.Sum);
            Assert.AreEqual("WorlD", obj.Hello.Value);
            Assert.AreEqual(false, obj.Hello.BoolValue);
            Assert.AreEqual(null, obj.Hello.NullableBool);
        }

        [Test]
        public void Test_SerializeJson_Valid()
        {
            var obj = new TestObj
            {
                Hello = new Hello {BoolValue = true, Value = "xYz"},
                Sum = 12
            };

            var result = obj.Serialize();

            Assert.AreEqual("{\n  \"hello\": {\n    \"value\": \"xYz\",\n    \"boolValue\": true\n  },\n  \"sum\": 12\n}", result);
        }
    }
}