// See https://aka.ms/new-console-template for more information
using NamePipe.Helper;


IPipeServer pipeServer = new PipeServer("TestPipe");
pipeServer.Start();
Console.WriteLine("NamePipe Server String...");
pipeServer.MessageReceivedEvent += (sender, args) => ReceiveMessage(sender, args);

while (true)
{
    Console.WriteLine("Enter input:");
    var line = Console.ReadLine();
    if (line == "exit")
        break;   
  
    if(!string.IsNullOrEmpty(line))
    {
        Console.WriteLine($"You typed: {line} ");
        Task.Run(async () =>
        {
            await pipeServer.SendMessageAsync(line);
            Console.WriteLine("Send Message done");
        }).ContinueWith(reuslt =>
        {
            if (reuslt.IsFaulted)
            {
                Console.WriteLine(reuslt.Exception);
            }            
        });
    }   
}
Environment.Exit(0);


static void ReceiveMessage(object sender, MessageReceivedEventArgs args)
{
    var message = args.Message;
    if (message is not null)
    {
        Console.WriteLine(message);
    }
}
