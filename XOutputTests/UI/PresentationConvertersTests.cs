using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Globalization;
using System.Windows.Media;
using XOutput.Devices.Input;
using XOutput.UI.Converters;
using XOutput.UI.Shell;

namespace XOutput.UI.Tests
{
    /// <summary>
    /// Tests for the presentation converters introduced with the Kinetic Console
    /// redesign (pure value conversions — no UI or hardware required).
    /// </summary>
    [TestClass()]
    public class RatioScaleConverterTests
    {
        private readonly RatioScaleConverter converter = new RatioScaleConverter();

        [DataRow(0, 100, 0.0)]
        [DataRow(50, 100, 0.5)]
        [DataRow(100, 100, 1.0)]
        [DataRow(150, 100, 1.0)]   // clamped to 1
        [DataRow(-10, 100, 0.0)]   // clamped to 0
        [DataRow(10, 0, 0.0)]      // max <= 0 -> 0
        [TestMethod]
        public void ConvertTest(double value, double max, double expected)
        {
            object result = converter.Convert(new object[] { value, max }, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(expected, (double)result, 1e-9);
        }

        [TestMethod]
        public void ConvertBackNotImplemented()
        {
            bool threw = false;
            try
            {
                converter.ConvertBack(new object[0], new[] { typeof(double) }, null, CultureInfo.InvariantCulture);
            }
            catch (System.NotImplementedException)
            {
                threw = true;
            }
            Assert.IsTrue(threw);
        }
    }

    /// <summary>
    /// Tests for the 0-1 to 2D-indicator pixel-position converter.
    /// </summary>
    [TestClass()]
    public class RatioToPositionConverterTests
    {
        private readonly RatioToPositionConverter converter = new RatioToPositionConverter();

        [DataRow(0.0, 4.0)]   // center 20 - 16
        [DataRow(0.5, 20.0)]  // center
        [DataRow(1.0, 36.0)]  // center + 16
        [TestMethod]
        public void ConvertTest(double value, double expected)
        {
            object result = converter.Convert(value, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(expected, (double)result, 1e-9);
        }
    }

    /// <summary>
    /// Tests for the bool-to-fill-scale converter.
    /// </summary>
    [TestClass()]
    public class BoolToDoubleConverterTests
    {
        private readonly BoolToDoubleConverter converter = new BoolToDoubleConverter();

        [DataRow(true, 1.0)]
        [DataRow(false, 0.0)]
        [TestMethod]
        public void ConvertTest(bool value, double expected)
        {
            object result = converter.Convert(value, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(expected, (double)result, 1e-9);
        }
    }

    /// <summary>
    /// Tests for the VID/PID label converter (hardware ID -> "VID xxxx · PID xxxx").
    /// </summary>
    [TestClass()]
    public class SourceToVidPidConverterTests
    {
        private readonly SourceToVidPidConverter converter = new SourceToVidPidConverter();

        [TestMethod]
        public void ConvertWithVidPid()
        {
            var mock = new Mock<IInputDevice>();
            mock.SetupGet(d => d.HardwareID).Returns("USB#VID_046D#PID_C24F");
            object result = converter.Convert(mock.Object, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.AreEqual("VID 046D · PID C24F", result);
        }

        [TestMethod]
        public void ConvertWithVidOnly()
        {
            var mock = new Mock<IInputDevice>();
            mock.SetupGet(d => d.HardwareID).Returns("HID#VID_045E");
            object result = converter.Convert(mock.Object, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.AreEqual("VID 045E", result);
        }

        [TestMethod]
        public void ConvertWithoutHardwareId()
        {
            var mock = new Mock<IInputDevice>();
            mock.SetupGet(d => d.HardwareID).Returns((string)null);
            object result = converter.Convert(mock.Object, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void ConvertNullDevice()
        {
            object result = converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.AreEqual("", result);
        }
    }

    /// <summary>
    /// Tests for the sidebar navigation item model.
    /// </summary>
    [TestClass()]
    public class ShellNavItemTests
    {
        [TestMethod]
        public void ConstructorTest()
        {
            var item = new ShellNavItem("HomeMenu", Geometry.Parse("M 1,1 L 5,5"), ShellPageType.Home);
            Assert.AreEqual("HomeMenu", item.LabelKey);
            Assert.AreEqual(ShellPageType.Home, item.PageType);
            Assert.IsNotNull(item.Icon);
            Assert.IsNotNull(item.LanguageModel);
        }
    }
}
