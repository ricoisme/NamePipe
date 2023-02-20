using NamePipe.Helper;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<IPipeClient>(factory => new PipeClient("TestPipe"));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

var pipeclient=app.Services.GetRequiredService<IPipeClient>();
pipeclient.Start();
pipeclient.MessageReceivedEvent += (sender, args) => ReceiveMessage(sender, args);

app.Run();

static void ReceiveMessage(object sender, MessageReceivedEventArgs args)
{
    var message = args.Message;
    if (message is not null)
    {
        Console.WriteLine(message);
    }
}
