using System.Collections.Generic;
using System;

public class EventBinding<T> where T : class
{
    public EventBinding(Action<T> callback)
    {
        Action = callback;
    }
    public Action<T> Action = (_) => { };

}

public class EventBus<T> where T : class
{
    readonly HashSet<EventBinding<T>> eventBindings = new HashSet<EventBinding<T>>();

    public void Register(EventBinding<T> binding) => eventBindings.Add(binding);
    public void Unrgister(EventBinding<T> binding) => eventBindings.Remove(binding);

    public void Raise(T arg)
    {
        foreach (var binding in eventBindings)
        {
            binding.Action(arg);
        }
    }
}

//public class EventBus