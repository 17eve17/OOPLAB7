using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class Operation
{
    public string ThreadName { get; set; }
    public DateTime Timestamp { get; set; }
    public string Resource { get; set; }
    public string Action { get; set; }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class OperationLog
{
    private readonly List<Operation> _log;
    private readonly Dictionary<string, object> _resourceLocks;
    private readonly Mutex _logMutex;

    public OperationLog()
    {
        _log = new List<Operation>();
        _resourceLocks = new Dictionary<string, object>();
        _logMutex = new Mutex();
    }

    private object GetResourceLock(string resource)
    {
        if (!_resourceLocks.ContainsKey(resource))
        {
            _resourceLocks[resource] = new object();
        }
        return _resourceLocks[resource];
    }

    public void RecordOperation(string threadName, string resource, string action)
    {
        var operation = new Operation
        {
            ThreadName = threadName,
            Timestamp = DateTime.UtcNow,
            Resource = resource,
            Action = action
        };

        lock (GetResourceLock(resource))
        {
            _logMutex.WaitOne();
            _log.Add(operation);
            _logMutex.ReleaseMutex();
            Console.WriteLine($"Операція виконана: {threadName} на ресурсі {resource} з дією: {action}");
        }
    }

    public List<Operation> GetOperations() => _log;

    public void ResolveConflict(string resource)
    {
        Console.WriteLine($"Конфлікт на ресурсі {resource}. Спроба відновлення...");
        Thread.Sleep(1000);
        Console.WriteLine($"Відновлення для ресурсу {resource} завершено.");
    }

    public void CheckAndHandleConflicts()
    {
        _logMutex.WaitOne();
        var groupedOperations = _log.GroupBy(op => op.Resource);

        foreach (var group in groupedOperations)
        {
            var operationsByResource = group.ToList();
            if (operationsByResource.Count > 1)
            {
                Console.WriteLine($"Конфлікт для ресурсу: {group.Key}");
                foreach (var op in operationsByResource)
                {
                    Console.WriteLine($"Операція від {op.ThreadName} о {op.Timestamp} на ресурсі {group.Key}");
                }

                ResolveConflict(group.Key);
            }
        }
        _logMutex.ReleaseMutex();
    }
}

public class OperationManager
{
    private readonly OperationLog _log;

    public OperationManager(OperationLog log)
    {
        _log = log;
    }

    public async Task ExecuteOperation(string threadName, string resource, string action)
    {
        try
        {
            await Task.Run(() => _log.RecordOperation(threadName, resource, action));
        }
        catch (ConflictException ex)
        {
            Console.WriteLine($"Конфлікт в операції: {ex.Message}");
        }
    }

    public void HandleConflicts()
    {
        _log.CheckAndHandleConflicts();
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var log = new OperationLog();
        var manager = new OperationManager(log);

        List<Task> tasks = new List<Task>
        {
            manager.ExecuteOperation("Thread 1", "ResourceA", "Update"),
            manager.ExecuteOperation("Thread 2", "ResourceA", "Delete"),
            manager.ExecuteOperation("Thread 3", "ResourceB", "Create"),
            manager.ExecuteOperation("Thread 4", "ResourceA", "Update")
        };

        await Task.WhenAll(tasks);

        manager.HandleConflicts();

        Console.WriteLine("\nВсі операції в журналі:");
        foreach (var operation in log.GetOperations())
        {
            Console.WriteLine($"{operation.Timestamp} - {operation.ThreadName} - {operation.Resource} - {operation.Action}");
        }
    }
}
