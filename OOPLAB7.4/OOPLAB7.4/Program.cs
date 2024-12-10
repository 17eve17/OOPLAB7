using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class Event
{
    public string EventName { get; set; }
    public int Timestamp { get; set; }
    public string NodeId { get; set; }
}

public class EventSystem
{
    private readonly Dictionary<string, List<Action<Event>>> _subscribers;
    private readonly Dictionary<string, int> _timestamps;
    private readonly object _lock;

    public EventSystem()
    {
        _subscribers = new Dictionary<string, List<Action<Event>>>();
        _timestamps = new Dictionary<string, int>();
        _lock = new object();
    }

    public void RegisterNode(string nodeId)
    {
        lock (_lock)
        {
            if (!_timestamps.ContainsKey(nodeId))
            {
                _timestamps[nodeId] = 0;
                _subscribers[nodeId] = new List<Action<Event>>();
                Console.WriteLine($"Node {nodeId} added.");
            }
            else
            {
                Console.WriteLine($"Node {nodeId} is already registered.");
            }
        }
    }

    public void UnregisterNode(string nodeId)
    {
        lock (_lock)
        {
            if (_subscribers.ContainsKey(nodeId))
            {
                _subscribers.Remove(nodeId);
                _timestamps.Remove(nodeId);
                Console.WriteLine($"Node {nodeId} removed.");
            }
            else
            {
                Console.WriteLine($"Node {nodeId} does not exist.");
            }
        }
    }

    public void SubscribeToEvent(string nodeId, Action<Event> callback)
    {
        lock (_lock)
        {
            if (_subscribers.ContainsKey(nodeId))
            {
                _subscribers[nodeId].Add(callback);
                Console.WriteLine($"Node {nodeId} subscribed to events.");
            }
            else
            {
                Console.WriteLine($"Node {nodeId} is not registered.");
            }
        }
    }

    public void CreateEvent(string nodeId, string eventName)
    {
        lock (_lock)
        {
            if (!_timestamps.ContainsKey(nodeId)) return;

            int timestamp = _timestamps[nodeId] + 1;
            _timestamps[nodeId] = timestamp;

            var eventInstance = new Event
            {
                EventName = eventName,
                Timestamp = timestamp,
                NodeId = nodeId
            };

            Console.WriteLine($"Node {nodeId} triggered event {eventName} at time {timestamp}");

            NotifySubscribers(eventInstance);
        }
    }

    private void NotifySubscribers(Event e)
    {
        lock (_lock)
        {
            foreach (var subscriber in _subscribers)
            {
                foreach (var callback in subscriber.Value)
                {
                    callback(e);
                }
            }
        }
    }

    public void ProcessEvent(Event eventObj)
    {
        lock (_lock)
        {
            if (_timestamps.ContainsKey(eventObj.NodeId))
            {
                _timestamps[eventObj.NodeId] = Math.Max(_timestamps[eventObj.NodeId], eventObj.Timestamp) + 1;
            }
        }
    }

    public void SyncEvents(List<Event> events)
    {
        foreach (var ev in events.OrderBy(e => e.Timestamp))
        {
            ProcessEvent(ev);
        }
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var eventSystem = new EventSystem();

        eventSystem.RegisterNode("Node1");
        eventSystem.RegisterNode("Node2");

        eventSystem.SubscribeToEvent("Node1", e => Console.WriteLine($"Node1 отримав подію {e.EventName} від {e.NodeId}"));
        eventSystem.SubscribeToEvent("Node2", e => Console.WriteLine($"Node2 отримав подію {e.EventName} від {e.NodeId}"));

        eventSystem.CreateEvent("Node1", "EventA");
        await Task.Delay(100);
        eventSystem.CreateEvent("Node2", "EventB");

        await Task.Delay(500);
        eventSystem.RegisterNode("Node3");
        eventSystem.SubscribeToEvent("Node3", e => Console.WriteLine($"Node3 отримав подію {e.EventName} від {e.NodeId}"));

        eventSystem.CreateEvent("Node3", "EventC");

        eventSystem.UnregisterNode("Node2");

        var events = new List<Event>
        {
            new Event { EventName = "EventA", Timestamp = 1, NodeId = "Node1" },
            new Event { EventName = "EventB", Timestamp = 2, NodeId = "Node2" }
        };

        eventSystem.SyncEvents(events);

        eventSystem.CreateEvent("Node1", "EventD");
    }
}
