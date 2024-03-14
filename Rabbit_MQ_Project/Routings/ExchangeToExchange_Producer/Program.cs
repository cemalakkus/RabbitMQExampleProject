//Exchange to Exchange Binding (Routing)

//Bir queue nasıl bir exchange'e bind ediliyorsa, bir exchange'i başka bir exchange'e bind edebiliriz. 
//Yönlendirme kuralı aynıdır. Exchange üzerinden çıkan mesaj, hedef exchange 'e binding ayarlarına göre yönlendirilir ve hedef exchange bu mesajı bağlı queue'lara iletir.

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

channel.ExchangeDeclare("ex.dog", "direct", false, true);
channel.ExchangeDeclare("ex.rabbit", "direct", false, true);

channel.QueueDeclare("queue-dog", false, false, true);
channel.QueueDeclare("queue-rabbit", false, false, true);

channel.ExchangeBind("ex.rabbit", "ex.dog", "route.rabbit"); //ExchangeBind metodu ile ex.rabbit'in ex.dog'a rooute.rabbit routingKey'i ile bind ediliyor.

channel.QueueBind("queue-dog", "ex.dog", "route.dog");
channel.QueueBind("queue-rabbit", "ex.rabbit", "route.rabbit");

channel.BasicPublish("ex.dog", "route.dog", false, null, Encoding.UTF8.GetBytes("This is a message from dog."));
channel.BasicPublish("ex.dog", "route.rabbit", false, null, Encoding.UTF8.GetBytes("This is a message from rabbit."));

Console.WriteLine("Press any key to exit.");
Console.ReadKey();