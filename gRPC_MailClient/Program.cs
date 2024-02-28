using Grpc.Core;
using Grpc.Net.Client;
using Mail;

namespace gRPC_MailClient;

public class Program
{
    public static async Task Main(string[] args)
    {
        /*  this is used  to get the mailbox name .  to identify which mailbox to operate on    */
        /*  GetMailBoxName(args) is created by developer , args are the PSVM args               */
        var mailboxName = GetMailBoxName(args);

   
        /*  As of this point we get the specified of the mailbox to access now we have to
            create a channel through which we can communicate and which is also mapped to 
            the named mailbox                                                                    */

        var channel = GrpcChannel.ForAddress("https://localhost:7230");

        /*  Now we create a channel through which the grpc client will communicate GrpcChannel
            is a class which exist in nuget packages  then we create a client which uses the 
            channel  
                                                                                        */
        var client = new MailBox.MailBoxClient(channel);
        Console.WriteLine($"Creating client to mailbox '{mailboxName}'");
        Console.WriteLine("Press escape to disconnect. Press any other key to forward mail.");

        /*  'using' Keyword is used to dispose of the currently using object after the usage and Operation over that
            resource is done(good coding practice) the resource we use in this typical 
            example is the 'client' which we previously created . 'Mailer is the method which we created in .proto file
            and rest is according to the parameters overrides available for the method '  */

        using (var call = client.Mailer(new Metadata { new("mailbox-name", mailboxName) }))
        {
            /* creating of task  which contains a anonymous function to execute a loop which continuously read the messages
             which are send by the server responseTask variable is used to keep track of the operation status of the operation */
            var responseTask = Task.Run(async () =>
            {
                /*      looping continuously and the method is awaited so as eben if the list is empty it will not
                 close and still wait for the incoming message  */
                await foreach (var message in call.ResponseStream.ReadAllAsync())
                {
                    /*  Performing the UI Operation and displaying the contents of the 'message ' fetched from the server       */
                    Console.ForegroundColor = message.Reason == MailboxMessage.Types.Reason.Received
                        ? ConsoleColor.Green
                        : ConsoleColor.Cyan;

                    Console.WriteLine();

                    Console.WriteLine(message.Reason == MailboxMessage.Types.Reason.Received
                        ? "Mail Received"
                        : "Mail Forward");

                    Console.WriteLine($"New Mail : {message.New} , Forwarded Mail : {message.Forwarded}");

                    Console.ResetColor();
                }
            });

            /*      this infinity loop is to scan the key pressed so as to control th operation of the user
             i.e to exit the application or to forward the email                                    */
            while (true)
            {
                var result = Console.ReadKey(true);

                if (result.Key == ConsoleKey.Escape)
                {break;}

                /*  if any other key is pressed than esc the application will interpret
                 that as a command to forward the message                                           */
                await call.RequestStream.WriteAsync(new ForwardMailMessage());
            }

            Console.WriteLine("Disconnecting");
            /*  no idea what is this about  . but in theroy these were written to let the ongoing operation to be
             completed of send and receiveng in case of exiting                                 */
            await call.RequestStream.CompleteAsync();
            await responseTask;
        }

        Console.WriteLine("Disconnected .press any key to exit");
        Console.ReadKey();
    }


    private static string GetMailBoxName(string[] args)
    {
        /*  This Method Only returns the name which was passed during the Client invocation ..
            if no name is provided then it simply returns the default name which is hardcoded in 
            the code                                                                              */
        if (args.Length < 1)
        {
            Console.WriteLine("No Name Provided during the Client Invocation as Initialization Parameters \n " +
                              "Using default MailBoxName : DefaultMailBox ");
            return "DefaultMailBox";
        }

        return args[0];
    }
}