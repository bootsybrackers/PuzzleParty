using System;

namespace PuzzleParty.Service
{
    public interface IServiceLocator
    {
        T Get<T>();

        void Register<T>(T service);
    }
}