using System.Collections.Concurrent;

namespace gRPC_MailServer;

public class MailQueueRepository
{
    private readonly ConcurrentDictionary<string, MailQueue> 
        _mailQueues = new();

    public MailQueue GetMailQueue(string name)
    {
        return _mailQueues.GetOrAdd(name, n => new MailQueue(n));
    }
}