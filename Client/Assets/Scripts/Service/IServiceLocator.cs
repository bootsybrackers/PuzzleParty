using System;


public interface IServiceLocator
{
    T Get<T>();

    void Register<T>(T service);
}