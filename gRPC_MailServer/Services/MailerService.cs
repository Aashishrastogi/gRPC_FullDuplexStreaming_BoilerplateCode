using Grpc.Core;
using Mail;

namespace gRPC_MailServer.Services;

public class MailerService : MailBox.MailBoxBase
{
    /*  This 'MailBox.MailBoxBase is the inherited class . this gets automatically generated when
        compiling or rebuilding the the corresponding .proto file MailBox is the Service name in the .proto file 
        and MailBoxBase is an automatic generation of the base class which will contains the methods 'RPC' 
        : just like the win forms automatic code generation in the partial classes for the styling properties */

    private readonly ILogger _logger;
    private readonly MailQueueRepository _messageQueueRepository;


    public MailerService(ILoggerFactory loggerfactory , MailQueueRepository messageQueueRepository)
    {
        _logger = loggerfactory.CreateLogger<MailerService>();
        _messageQueueRepository = messageQueueRepository;
        
    }

    public override async Task Mailer
        (
            /*the input parameters will be auto generated upon overriding the method in the service class
             must be strictly followed . the type of the input parameters depends upon the type declared 
             of method body in the proto file*/
            IAsyncStreamReader<ForwardMailMessage> requestStream,
            IServerStreamWriter<MailboxMessage> responseStream,
            ServerCallContext context
        )
        /*  Every method written in the .proto file must overridden in the service class or
            else they will throw an exception upon accessing them  */
        /* The main logic of the method must start from this section and can be dispersed over number of classes
            keeping the concepts of Multithreading and task in mind */
    {
        var mailboxName = context.RequestHeaders.Single((e) => e.Key == "mailbox-name").Value;

        var mailQueue = _messageQueueRepository.GetMailQueue(mailboxName);

        _logger.LogInformation("connected to {MailboxName}", mailboxName);

        mailQueue.Changed += ReportChanges;
        
        try
        {
            while (await requestStream.MoveNext())
            {
                if (mailQueue.TryForwardMail(out var message))
                {
                    _logger.LogInformation($"Forwarded mail: {message.Content}");
                }
                else
                {
                    _logger.LogWarning("No mail to forward.");
                }
            }
        }
        finally
        {
            mailQueue.Changed -= ReportChanges;
        }

        _logger.LogInformation($"{mailboxName} disconnected");
        
        
        async Task ReportChanges(MailQueueChangeState currentstate)
        {
            await responseStream.WriteAsync(new MailboxMessage
            {
                Forwarded = currentstate.ForwardedCount,
                New = currentstate.TotalCount - currentstate.ForwardedCount,
                Reason = currentstate.Reason
            });
            
        }
    }

    
}