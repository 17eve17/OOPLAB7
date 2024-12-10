using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DistributedSystemNode
{
    public string NodeName { get; }
    private readonly List<DistributedSystemNode> _connectedNodes;
    private bool _isActive;

    public DistributedSystemNode(string nodeName)
    {
        NodeName = nodeName;
        _connectedNodes = new List<DistributedSystemNode>();
        _isActive = true;
    }

    public void ConnectTo(DistributedSystemNode node)
    {
        _connectedNodes.Add(node);
    }

    public async Task SendMessageAsync(string message, DistributedSystemNode recipient)
    {
        Console.WriteLine($"{NodeName} відправляє повідомлення: \"{message}\" до {recipient.NodeName}");
        await recipient.ReceiveMessageAsync(message, this);
    }

    public async Task ReceiveMessageAsync(string message, DistributedSystemNode sender)
    {
        Console.WriteLine($"{NodeName} отримав повідомлення: \"{message}\" від {sender.NodeName}");
        await ProcessMessageAsync(message, sender);
    }

    private async Task ProcessMessageAsync(string message, DistributedSystemNode sender)
    {
        await Task.Delay(500);
        Console.WriteLine($"{NodeName} обробив повідомлення: \"{message}\" від {sender.NodeName}");
    }

    public async Task NotifyStatusAsync()
    {
        _isActive = !_isActive;
        string status = _isActive ? "активний" : "неактивний";
        Console.WriteLine($"{NodeName} змінює статус на {status}");
        foreach (var node in _connectedNodes)
        {
            await node.ReceiveStatusNotificationAsync(status, this);
        }
    }

    public async Task ReceiveStatusNotificationAsync(string status, DistributedSystemNode sender)
    {
        Console.WriteLine($"{NodeName} отримав повідомлення про статус {status} від {sender.NodeName}");
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var nodeA = new DistributedSystemNode("Вузол A");
        var nodeB = new DistributedSystemNode("Вузол B");
        var nodeC = new DistributedSystemNode("Вузол C");

        nodeA.ConnectTo(nodeB);
        nodeA.ConnectTo(nodeC);
        nodeB.ConnectTo(nodeA);
        nodeC.ConnectTo(nodeA);

        await nodeA.SendMessageAsync("Привіт, Вузол B!", nodeB);
        await nodeA.SendMessageAsync("Привіт, Вузол C!", nodeC);

        await nodeA.NotifyStatusAsync();
        await nodeB.NotifyStatusAsync();
        await nodeC.NotifyStatusAsync();
    }
}
