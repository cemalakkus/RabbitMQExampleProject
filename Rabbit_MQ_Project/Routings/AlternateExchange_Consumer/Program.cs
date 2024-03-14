using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;

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


channel.BasicConsume("queue.videg", true, consumer);
channel.BasicConsume("queue.image", true, consumer);
channel.BasicConsume("queue.unrouted", true, consumer);


Console.WriteLine("Waiting for messages... Press any key to exit.");
Console.ReadKey();