using NUnit.Framework;
using xFrame.Runtime.MVVM.Examples;

namespace xFrame.Tests.MVVM
{
    [TestFixture]
    public class PlayerViewModelTests
    {
        [Test]
        public void Initialize_ShouldExposeModelValuesToBindableProperties()
        {
            var model = new PlayerModel("Hero", 100);
            var vm = new PlayerViewModel(model);

            Assert.AreEqual("Hero", vm.Name.Value);
            Assert.AreEqual("100 / 100", vm.HealthText.Value);
            Assert.AreEqual(1f, vm.HealthFillAmount.Value);
        }

        [Test]
        public void TakeDamage_ShouldUpdateHealthTextAndFillAmount()
        {
            var model = new PlayerModel("Hero", 100);
            var vm = new PlayerViewModel(model);

            vm.TakeDamage(20);

            Assert.AreEqual("80 / 100", vm.HealthText.Value);
            Assert.AreEqual(0.8f, vm.HealthFillAmount.Value);
        }

        [Test]
        public void TakeDamage_ToZero_ShouldDisableCommandExecution()
        {
            var model = new PlayerModel("Hero", 30);
            var vm = new PlayerViewModel(model);

            vm.TakeDamage(30);

            Assert.AreEqual("0 / 30", vm.HealthText.Value);
            Assert.IsFalse(vm.TakeDamageCommand.CanExecute());
        }
    }
}
