using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class Resource
{
    public string Name { get; }
    private readonly SemaphoreSlim _semaphore;
    private readonly Mutex _mutex;

    public Resource(string name, int maxConcurrentAccess)
    {
        Name = name;
        _semaphore = new SemaphoreSlim(maxConcurrentAccess, maxConcurrentAccess);
        _mutex = new Mutex();
    }

    public async Task AccessResourceAsync(string threadName, int priority)
    {
        await _semaphore.WaitAsync();
        try
        {
            _mutex.WaitOne();
            Console.WriteLine(string.Format("Потік {0} з пріоритетом {1} отримав доступ до ресурсу: {2}", threadName, priority, Name));
            await Task.Delay(1000);
        }
        finally
        {
            _mutex.ReleaseMutex();
            _semaphore.Release();
            Console.WriteLine(string.Format("Потік {0} з пріоритетом {1} звільнив ресурс: {2}", threadName, priority, Name));
        }
    }
}

public class ResourceManager
{
    private readonly Dictionary<string, Resource> _resources = new Dictionary<string, Resource>();

    public void AddResource(string name, int maxConcurrentAccess)
    {
        _resources[name] = new Resource(name, maxConcurrentAccess);
    }

    public Resource GetResource(string name)
    {
        if (_resources.ContainsKey(name))
        {
            return _resources[name];
        }
        return null;
    }
}

public class TaskScheduler
{
    private readonly ResourceManager _resourceManager;

    public TaskScheduler(ResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public async Task RunTask(string resourceName, string threadName, int priority)
    {
        Resource resource = _resourceManager.GetResource(resourceName);
        if (resource != null)
        {
            await resource.AccessResourceAsync(threadName, priority);
        }
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        ResourceManager resourceManager = new ResourceManager();
        resourceManager.AddResource("CPU", 2);
        resourceManager.AddResource("RAM", 3);
        resourceManager.AddResource("Disk", 1);

        TaskScheduler taskScheduler = new TaskScheduler(resourceManager);

        List<Task> tasks = new List<Task>
        {
            taskScheduler.RunTask("CPU", "Thread A", 1),
            taskScheduler.RunTask("RAM", "Thread B", 2),
            taskScheduler.RunTask("Disk", "Thread C", 3),
            taskScheduler.RunTask("CPU", "Thread D", 1),
            taskScheduler.RunTask("RAM", "Thread E", 3),
            taskScheduler.RunTask("Disk", "Thread F", 2)
        };

        await Task.WhenAll(tasks);
    }
}
