using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nu.Web.ViewModel.Attributes;
using Nu.Web.ViewModel.FrontController;
using Should;

namespace Test.Nu.Web.Mvc
{
    [TestClass]
    public class FrontControllerTest
    {

        private FrontController frontController;

        [TestInitialize]
        public void Initialize()
        {
            frontController = new FrontController();
            frontController.RegisterObject<StubbedController>();
        }

        [TestMethod]
        public void TestEmptyValue()
        {
            object value;
            frontController.Invoke("DefaultValue", new object[0], out value);
            (value is StubbedModel).ShouldBeTrue();
            (value as StubbedModel).Value.ShouldEqual("default");
        }

        [TestMethod]
        public void TestSetValue()
        {
            object value;
            frontController.Invoke("SetValue", new[] {"value"}, out value);
            (value is StubbedModel).ShouldBeTrue();
            (value as StubbedModel).Value.ShouldEqual("value");
        }

    }

    public class StubbedController
    {
        [NuController]
        public StubbedModel DefaultValue()
        {
            return new StubbedModel {Value = "default"};
        }

        [NuController]
        public StubbedModel SetValue(string value)
        {
            return new StubbedModel {Value = value};
        }
        
    }

    [NuModel]
    public class StubbedModel
    {
        public string Value { get; set; }
    }
}
