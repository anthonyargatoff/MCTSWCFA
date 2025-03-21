using System.Collections.Generic;

public interface ICreationObserver<T>
{
    void OnObservableCreated(T obj);
    void OnObservableDestroyed(T obj);
}

public interface ICreationObservable<T>
{
    private static List<ICreationObserver<T>> observers = new();

    public static void Subscribe(ICreationObserver<T> observer) => observers.Add(observer);
    
    public static void Unsubscribe(ICreationObserver<T> observer) => observers.Remove(observer);

    protected static void NotifyCreated(T obj)
    {
        foreach (var observer in observers)
        {
            observer.OnObservableCreated(obj);
        }
    }

    protected static void NotifyDestroyed(T obj)
    {
        foreach (var observer in observers)
        {
            observer.OnObservableDestroyed(obj);
        }
    }
}