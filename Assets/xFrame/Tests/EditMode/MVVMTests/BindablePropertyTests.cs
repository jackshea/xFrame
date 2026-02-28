using NUnit.Framework;
using xFrame.Runtime.MVVM.Core;

namespace xFrame.Tests.MVVM
{
    [TestFixture]
    public class BindablePropertyTests
    {
        [Test]
        public void Bind_InvokeImmediately_ShouldReceiveInitialValue()
        {
            var property = new BindableProperty<int>(5);
            int received = 0;

            property.Bind(v => received = v);

            Assert.AreEqual(5, received);
        }

        [Test]
        public void Value_SetSameValue_ShouldNotNotifyAgain()
        {
            var property = new BindableProperty<string>("A");
            int called = 0;
            property.Bind(_ => called++, false);

            property.Value = "A";

            Assert.AreEqual(0, called);
        }

        [Test]
        public void Unbind_AfterDisposedSubscription_ShouldNotNotify()
        {
            var property = new BindableProperty<int>(0);
            int received = 0;

            var subscription = property.Bind(v => received = v, false);
            subscription.Dispose();
            property.Value = 9;

            Assert.AreEqual(0, received);
        }
    }
}
