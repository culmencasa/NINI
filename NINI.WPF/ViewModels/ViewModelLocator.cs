using Autofac;
using System;
using System.Collections.Generic;
using System.Text;

namespace NINI.ViewModels
{

    public class ViewModelLocator : IDisposable
    {
        /// <summary>
        /// ViewModelLocator单例
        /// </summary>
        public static ViewModelLocator Instance
        {
            get;
            private set;
        }

        private IContainer _container;


        public ViewModelLocator()
        {
            ViewModelLocator.Instance = this;

            var containerBuilder = new ContainerBuilder();
            //containerBuilder.RegisterType<ChangeDirectoryDialogService>().Named<IOutputDialogService>("ChangeDirectoryDialogService").AsSelf();
            //containerBuilder.RegisterType<SelectFileDialogService>().Named<IOutputDialogService>("SelectFileDialogService").AsSelf();

            //containerBuilder.RegisterInstance(SimpleMessenger.Default).As<IMessenger>();
            containerBuilder.RegisterType<NotifyIconViewModel>().AsSelf().SingleInstance();
            containerBuilder.RegisterType<RunViewModel>().AsSelf().SingleInstance();

            _container = containerBuilder.Build();
        }

        public NotifyIconViewModel NotifyIconVM
        {
            get
            {
                return _container.Resolve<NotifyIconViewModel>();
            }
        }

        public RunViewModel RunVM
        {
            get
            {
                return _container.Resolve<RunViewModel>();
            }
        }

        //public IOutputDialogService ChangeDirectoryDialogService
        //{
        //    get
        //    {
        //        return _container.Resolve<ChangeDirectoryDialogService>();
        //    }
        //}


        //public IOutputDialogService SelectFileDialogService
        //{
        //    get
        //    {
        //        return _container.Resolve<SelectFileDialogService>();
        //    }
        //}


        public void Dispose()
        {
        }
    }
}
