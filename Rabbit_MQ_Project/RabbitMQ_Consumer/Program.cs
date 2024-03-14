using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ_Consumer;


//---------------------------------------------------------Ortak-------------------------------------------------------------------------------
var factory = new ConnectionFactory()
{
    HostName = "localhost",
    VirtualHost = "/",
    Port = 5672,
    UserName = "guest",
    Password = "guest"
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (s, e) =>
{
    var message = Encoding.UTF8.GetString(e.Body.ToArray());
    Console.WriteLine("Message received: {0}", message);
};




//---------------------------------------------------------Direct Exchange-------------------------------------------------------------------------------

channel.BasicConsume("log.info", true, consumer);
channel.BasicConsume("log.error", true, consumer);
channel.BasicConsume("log.warning", true, consumer);

//----------------------------------------------------------------------------------------------------------------------------------------


//---------------------------------------------------------Topic Exchange-------------------------------------------------------------------------------
channel.BasicConsume("log.error", true, consumer);
channel.BasicConsume("logs.all", true, consumer);
channel.BasicConsume("all.warnings", true, consumer);

//----------------------------------------------------------------------------------------------------------------------------------------


//---------------------------------------------------------Fanout Exchange-------------------------------------------------------------------------------

channel.BasicConsume("queue.one", false, consumer);
channel.BasicConsume("queue.two", false, consumer);

//----------------------------------------------------------------------------------------------------------------------------------------



//---------------------------------------------------------Header Exchange-------------------------------------------------------------------------------

channel.BasicConsume("queue.headerOne", true, consumer);
channel.BasicConsume("queue.headerTwo", true, consumer);
channel.BasicConsume("queue.headerThree", true, consumer);

//----------------------------------------------------------------------------------------------------------------------------------------

Console.WriteLine("Waiting for messages... Press any key to exit.");
Console.ReadKey();




//------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/*
var factoryTest = new ConnectionFactory() { HostName = "localhost" };
using (IConnection connectionTest = factoryTest.CreateConnection())
using (IModel channelTest = connectionTest.CreateModel())
{
    channelTest.QueueDeclare(queue: "CmlMsgQue",
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);

    var consumerTest = new EventingBasicConsumer(channelTest);
    consumerTest.Received += (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Person person = JsonConvert.DeserializeObject<Person>(message);
        Console.WriteLine($" Adı: {person.Name} Soyadı:{person.SurName} [{person.Message}]");
    };
    channelTest.BasicConsume(queue: "CmlMsgQue",
                         autoAck: true,
                         consumer: consumer);

    Console.WriteLine(" İşe Alındınız. Teşekkürler :)");
    Console.ReadLine();
}
*/