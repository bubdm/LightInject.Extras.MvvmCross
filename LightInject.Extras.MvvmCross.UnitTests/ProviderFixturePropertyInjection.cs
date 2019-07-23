﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MvvmCross.Base;
using MvvmCross.Exceptions;
using MvvmCross.IoC;

namespace LightInject.Extras.MvvmCross.UnitTests
{
    [TestClass]
    public class ProviderFixturePropertyInjection : IDisposable
    {
        [TestInitialize]
        public void BeforeEachMethod()
        {
            MvxSingleton.ClearAllSingletons();
        }

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private interface IHasDependentProperty
        {
            IInterface1 Dependency { get; set; }

            IInterface2 MarkedDependency { get; set; }
        }

        private interface IInterface
        {
        }

        private interface IInterface1
        {
        }

        private interface IInterface2
        {
        }

        public void Dispose()
        {
            foreach (var disposable in this._disposables)
            {
                disposable.Dispose();
            }

            this._disposables.Clear();
        }

        [TestMethod]
        public void IfSetInOptions_OnNonResolvableProperty_Throws()
        {
            Assert.ThrowsException<NotSupportedException>(() =>
            {
                this.CreateProvider(options: new MvxPropertyInjectorOptions()
                {
                    ThrowIfPropertyInjectionFails = true,
                    InjectIntoProperties = MvxPropertyInjection.MvxInjectInterfaceProperties,
                });
            });
        }

        [TestMethod]
        public void IgnoresNonResolvableProperty()
        {
            // Arrange
            var provider = this.CreateProvider(options:
                new MvxPropertyInjectorOptions()
                {
                    InjectIntoProperties = MvxPropertyInjection.MvxInjectInterfaceProperties,
                });

            // Act
            var obj = provider.IoCConstruct<HasDependantProperty>();

            // Assert
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Dependency);
            Assert.IsNull(obj.MarkedDependency);
        }

        [TestMethod]
        public void InjectsOnlyMarkedProperties_WithCustomAttribute_IfEnabled()
        {
            // Arrange
            var provider = CreateProvider(new MvxPropertyInjectorOptions() { InjectIntoProperties = MvxPropertyInjection.AllInterfaceProperties });
            provider.RegisterType<IInterface1, Concrete1>();
            provider.RegisterType<IInterface2, Concrete2>();

            // Act
            var obj = provider.IoCConstruct<HasDependantProperty>();

            // Assert
            Assert.IsNotNull(obj);
            Assert.IsNotNull(obj.Dependency);
            Assert.IsNull(obj.MarkedDependency);
        }

        [TestMethod]
        public void InjectsOnlyMarkedProperties_WithCustomAttribute_IfEnabled_Lazy()
        {
            // Arrange
            var provider = CreateProvider(new MvxPropertyInjectorOptions() { InjectIntoProperties = MvxPropertyInjection.AllInterfaceProperties });
            provider.RegisterType<IInterface1, Concrete1>();
            provider.RegisterType<IInterface2, Concrete2>();
            provider.RegisterSingleton<IHasDependentProperty>(provider.IoCConstruct<HasDependantProperty>);

            // Act
            var obj = provider.Resolve<IHasDependentProperty>();

            // Assert
            Assert.IsNotNull(obj);
            Assert.IsNotNull(obj.Dependency);
            Assert.IsNull(obj.MarkedDependency);
        }

        [TestMethod]
        public void InjectsOnlyMarkedPropertiesIfEnabled()
        {
            // Arrange
            var provider = this.CreateProvider(options:
                new MvxPropertyInjectorOptions()
                {
                    InjectIntoProperties = MvxPropertyInjection.MvxInjectInterfaceProperties,
                });
            provider.RegisterType<IInterface1, Concrete1>();
            provider.RegisterType<IInterface2, Concrete2>();

            // Act
            var obj = provider.IoCConstruct<HasDependantProperty>();

            // Assert
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Dependency);
            Assert.IsNotNull(obj.MarkedDependency);
        }

        [TestMethod]
        public void InjectsOnlyMarkedPropertiesIfEnabled_Lazy()
        {
            // Arrange
            var provider = this.CreateProvider(options:
                new MvxPropertyInjectorOptions()
                {
                    InjectIntoProperties = MvxPropertyInjection.MvxInjectInterfaceProperties,
                });
            provider.RegisterType<IInterface1, Concrete1>();
            provider.RegisterType<IInterface2, Concrete2>();
            provider.RegisterSingleton<IHasDependentProperty>(provider.IoCConstruct<HasDependantProperty>);

            // Act
            var obj = provider.Resolve<IHasDependentProperty>();

            // Assert
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Dependency);
            Assert.IsNotNull(obj.MarkedDependency);
        }

        [TestMethod]
        public void InjectsPropertiesIfEnabled()
        {
            // Arrange
            var provider = this.CreateProvider(new MvxPropertyInjectorOptions() { InjectIntoProperties = MvxPropertyInjection.AllInterfaceProperties });
            provider.RegisterType<IInterface1, Concrete1>();
            provider.RegisterType<IInterface2, Concrete2>();

            // Act
            var obj = provider.IoCConstruct<HasDependantProperty>();

            // Assert
            Assert.IsNotNull(obj);
            Assert.IsNotNull(obj.Dependency);
            Assert.IsNotNull(obj.MarkedDependency);
        }

        [TestMethod]
        public void PropertyInjectionCanBeCustomized()
        {
            var provider = CreateProvider(new MvxPropertyInjectorOptions() { InjectIntoProperties = MvxPropertyInjection.AllInterfaceProperties });
            this._disposables.Add(provider);
            provider.RegisterType(() => new Concrete());
            provider.RegisterType(typeof(Exception), () => new DivideByZeroException());
            var resolved = provider.Resolve<Concrete>();

            Assert.IsInstanceOfType(resolved.PropertyToInject, typeof(DivideByZeroException));
            Assert.IsNull(resolved.PropertyToSkip);
        }

        [TestMethod]
        public void PropertyInjectionCanBeEnabled()
        {
            var provider = CreateProvider(new MvxPropertyInjectorOptions() { InjectIntoProperties = MvxPropertyInjection.AllInterfaceProperties });
            this._disposables.Add(provider);
            provider.RegisterType(() => new Concrete());
            provider.RegisterType(typeof(Exception), () => new DivideByZeroException());
            var resolved = provider.Resolve<Concrete>();

            // Default behavior is to inject all unset properties.
            Assert.IsInstanceOfType(resolved.PropertyToInject, typeof(DivideByZeroException));
            Assert.IsInstanceOfType(resolved.PropertyToSkip, typeof(DivideByZeroException));
        }

        [TestMethod]
        public void PropertyInjectionOffByDefault()
        {
            var provider = this.CreateProvider();
            provider.RegisterType(() => new Concrete());
            var resolved = provider.Resolve<Concrete>();
            Assert.IsNull(resolved.PropertyToInject);
        }

        private LightInjectIocProvider CreateProvider(MvxPropertyInjectorOptions options = null)
        {
            var container = new ServiceContainer();
            var provider = new LightInjectIocProvider(container, options);
            this._disposables.Add(provider);
            return provider;
        }

        private class Concrete : IInterface
        {
            public Exception PropertyToInject { get; set; }

            public Exception PropertyToSkip { get; set; }
        }

        private class Concrete1 : IInterface1
        {
        }

        private class Concrete2 : IInterface2
        {
        }

        private class HasDependantProperty : IHasDependentProperty
        {
            public IInterface1 Dependency { get; set; }

            [Inject]
            public IInterface2 MarkedDependency { get; set; }
        }
    }
}
