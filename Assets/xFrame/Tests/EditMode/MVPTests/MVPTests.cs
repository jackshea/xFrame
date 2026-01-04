using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using VContainer;
using xFrame.Runtime.Logging;
using xFrame.MVP;

namespace xFrame.Tests.MVP
{
    public class MVPTests
    {
        private IObjectResolver _container;
        private IMVPManager _mvpManager;
        private IXLogger _logger;
        
        [SetUp]
        public void Setup()
        {
            var builder = new ContainerBuilder();
            _logger = new XLogger("MVPTests");
            builder.RegisterInstance<IXLogger>(_logger);
            builder.RegisterMVPModule();
            builder.RegisterMVP<TestModel, TestView, TestPresenter>();
            
            _container = builder.Build();
            _mvpManager = _container.Resolve<IMVPManager>();
        }
        
        [Test]
        public async void TestModelInitialization()
        {
            var model = _container.Resolve<TestModel>();
            await model.InitializeAsync();
            
            Assert.IsTrue(model.IsInitialized);
        }
        
        [Test]
        public async void TestPresenterBinding()
        {
            var model = _container.Resolve<TestModel>();
            var view = _container.Resolve<TestView>();
            var presenter = _container.Resolve<TestPresenter>();
            
            await model.InitializeAsync();
            await presenter.InitializeAsync();
            await presenter.BindAsync(view, model);
            
            Assert.IsTrue(presenter.IsActive);
        }
        
        [Test]
        public void TestMVPTripleCreation()
        {
            var model = _container.Resolve<TestModel>();
            var view = _container.Resolve<TestView>();
            var presenter = _container.Resolve<TestPresenter>();
            
            var triple = new MVPTriple<TestModel, TestView, TestPresenter>(model, view, presenter);
            
            Assert.IsNotNull(triple.Model);
            Assert.IsNotNull(triple.View);
            Assert.IsNotNull(triple.Presenter);
        }
        
        [Test]
        public void TestDataChangeNotification()
        {
            var model = _container.Resolve<TestModel>();
            var dataChanged = false;
            
            model.OnDataChanged += (m) => dataChanged = true;
            model.UpdateData("test");
            
            Assert.IsTrue(dataChanged);
        }
        
        [Test]
        public async void TestPresenterUnbinding()
        {
            var model = _container.Resolve<TestModel>();
            var view = _container.Resolve<TestView>();
            var presenter = _container.Resolve<TestPresenter>();
            
            await model.InitializeAsync();
            await presenter.InitializeAsync();
            await presenter.BindAsync(view, model);
            
            Assert.IsTrue(presenter.IsActive);
            
            await presenter.UnbindAsync();
            
            Assert.IsFalse(presenter.IsActive);
        }
    }
    
    public class TestModel : BaseModel
    {
        public bool IsInitialized { get; private set; }
        public string Data { get; private set; }
        
        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            IsInitialized = true;
        }
        
        public void UpdateData(string data)
        {
            Data = data;
            NotifyDataChanged();
        }
    }
    
    public class TestView : IView
    {
        public bool IsActive { get; private set; }
        private IPresenter _presenter;
        
        public async UniTask ShowAsync()
        {
            IsActive = true;
            await UniTask.CompletedTask;
        }
        
        public async UniTask HideAsync()
        {
            IsActive = false;
            await UniTask.CompletedTask;
        }
        
        public void BindPresenter(IPresenter presenter)
        {
            _presenter = presenter;
        }
        
        public void UnbindPresenter()
        {
            _presenter = null;
        }
        
        public void Dispose()
        {
            UnbindPresenter();
        }
    }
    
    public class TestPresenter : BasePresenter
    {
        protected override async UniTask OnBindAsync()
        {
            await UniTask.CompletedTask;
        }
        
        protected override async UniTask OnUnbindAsync()
        {
            await UniTask.CompletedTask;
        }
        
        protected override async UniTask OnShowAsync()
        {
            await UniTask.CompletedTask;
        }
        
        protected override async UniTask OnHideAsync()
        {
            await UniTask.CompletedTask;
        }
        
        protected override void OnModelDataChanged(IModel model)
        {
        }
    }
}
