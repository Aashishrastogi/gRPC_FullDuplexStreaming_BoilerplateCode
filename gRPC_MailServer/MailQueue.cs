using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Mail;
using Microsoft.Extensions.Options;

namespace gRPC_MailServer;

public record Mail
(
    int Id,
    string Content
);

public record MailQueueChangeState
(
    int TotalCount,
    int ForwardedCount,
    MailboxMessage.Types.Reason Reason
);

public class MailQueue
{
    private readonly Channel<Mail> _incomingMail;
    private int _totalMailCount;
    private int _forwardedMailCount;

    public string Name { get; }
    public event Func<MailQueueChangeState, Task>? Changed;

    public MailQueue(string name)
    {
        Name = name;
        _incomingMail = Channel.CreateUnbounded<Mail>();

        _ = Task.Run(async () =>
        {
            var random = new Random();

            while (true)
            {
                _totalMailCount++;
                var mail = new Mail(_totalMailCount, $"Message #{_totalMailCount}");
                await _incomingMail.Writer.WriteAsync(mail);
                OnChange(MailboxMessage.Types.Reason.Received);
               
                await Task.Delay(TimeSpan.FromSeconds(random.Next(5, 15)));
            }
        });
    }

    public bool TryForwardMail([NotNullWhen(true)] out Mail? message)
    {
        if (_incomingMail.Reader.TryRead(out message))
        {
            Interlocked.Increment(ref _forwardedMailCount);
            OnChange(MailboxMessage.Types.Reason.Forwarded);

            return true;
        }

        return false;
    }

    private void OnChange(MailboxMessage.Types.Reason resaon)
    {
        Changed?.Invoke(new MailQueueChangeState(_totalMailCount, _forwardedMailCount, resaon));
    }
}