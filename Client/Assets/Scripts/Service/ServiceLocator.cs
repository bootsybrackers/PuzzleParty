using System;
using System.Collections.Generic;
using PuzzleParty.Maps;
using PuzzleParty.Levels;
using PuzzleParty.Progressions;

namespace PuzzleParty.Service
{
    public class ServiceLocator : IServiceLocator
    {

        private static ServiceLocator Instance;

        private Dictionary<Type,object> Services = new(); 
    
    public ServiceLocator()
    {
        Configure();
    }
    
    public static ServiceLocator GetInstance()
    {
        if(Instance == null)
        {
            Instance = new ServiceLocator();
            
        }

        return Instance;

    }
    
    public T Get<T>()
    {
       return (T) Services[typeof(T)];
    }

    public void Register<T>(T service)
    {
        Services.Add(typeof(T), service);
    }

    public void Configure()
    {
        // Clear existing services to avoid duplicates when called multiple times
        Services.Clear();

        // Here you configure every service the game needs
        Register(new ProgressionService());
        Register(new LevelService());
        Register(new SceneLoader());
        Register(new MapService());
        Register(new TransitionService());
    }
    }
}