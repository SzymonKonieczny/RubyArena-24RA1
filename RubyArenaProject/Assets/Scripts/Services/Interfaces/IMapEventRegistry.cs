using System;
using System.Collections.Generic;

interface IMapEventRegistry
{
    void PublishEvent<T>(T eventData);
}

public class EventBinding<T> where T : class
{
    public Action<T> Action = (_) => { };

}

public static class EventBus<T> where T : class
{
    static HashSet<EventBinding<T>> eventBindings = new HashSet<EventBinding<T>>();

    public static void Register(EventBinding<T> binding) => eventBindings.Add(binding);
    public static void Unrgister(EventBinding<T> binding) => eventBindings.Remove(binding);

    public static void Raise(T arg)
    {
        foreach (var binding in eventBindings)
        {
            binding.Action(arg);
        }
    }
}
