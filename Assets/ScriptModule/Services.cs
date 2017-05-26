/// <summary>
/// Simple service manager. Allows global access to a single instance of any class.
/// Copyright (c) 2014-2015 Eliot Lash
/// </summary>
using System;
using System.Collections.Generic;

public class Services
{
    //Statics
    private static Services _instance;

    //Instance
    private Dictionary<Type, object> services = new Dictionary<Type, object>();

    public Services()
    {
        if (_instance != null)
        {
            UnityEngine.Debug.LogError("Cannot have two instances of singleton.");
            return;
        }

        _instance = this;
    }

    /// <summary>
    /// Getter for singelton instance.
    /// </summary>
    public static Services instance
    {
        get
        {
            if (_instance == null)
            {
                new Services();
            }

            return _instance;
        }
    }

    /// <summary>
    /// Set the specified service instance. Usually called like Set<ExampleService>(this).
    /// </summary>
    /// <param name="service">Service instance object.</param>
    /// <typeparam name="T">Type of the instance object.</typeparam>
    public void Set<T>(T service) where T : class
    {
        services.Add(typeof(T), service);
    }

    /// <summary>
    /// Gets the specified service instance. Called like Get<ExampleService>().
    /// </summary>
    /// <typeparam name="T">Type of the service.</typeparam>
    /// <returns>Service instance, or null if not initialized</returns>
    public T Get<T>() where T : class
    {
        T ret = null;
        try
        {
            ret = services[typeof(T)] as T;
        }
        catch (KeyNotFoundException)
        {
        }
        return ret;
    }

    /// <summary>
    /// Clears internal dictionary of service instances.
    /// This will not clear out any global state that they contain,
    /// unless there are no other references to the object.
    /// </summary>
    public void Clear()
    {
        services.Clear();
    }
}