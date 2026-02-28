using NUnit.Framework;
using xFrame.Runtime.MVVM.Core;

namespace xFrame.Tests.MVVM
{
    [TestFixture]
    public class RelayCommandTests
    {
        [Test]
        public void Execute_WhenCanExecuteFalse_ShouldNotInvokeAction()
        {
            int called = 0;
            var command = new RelayCommand(() => called++, () => false);

            command.Execute();

            Assert.AreEqual(0, called);
        }

        [Test]
        public void Execute_WhenCanExecuteTrue_ShouldInvokeAction()
        {
            int called = 0;
            var command = new RelayCommand(() => called++);

            command.Execute();

            Assert.AreEqual(1, called);
        }
    }
}
