using RabbitMQ.Client;
using System.Text;

//Alternate Exchange

//Dağıtık sistemler üzerinde kurulan asenkron bir yapıda bir exchange’e yönlendirilen bazı mesajlar bağlı queue’lara yönlendirilmek üzere uygun olmayabilir. Bunlar unrouted message olarak adlandırılmaktadır.
//Bu mesajlar exchange tarafından yok sayılır ve mesaj kaybolur. İşte bu durumlarda bu mesajları toplamak için Alternate Exchange kullanılabilir.
//Bir alternate exchange tanımlamak için -biraz sonra göreceğimiz gibi- exchange’e alternate-exchange keyi ve hangi exchange’in alternate exchange olarak kullanılacağı değer eklenmelidir, böylece unrouted mesajlar bu exchange’e yönlendirilecektir. Fanout Exchange bir filtreleme işlemi yapmadığından dolayı bu yapıya gayet uygundur.



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

channel.QueueDeclare("queue.video", false, false, true);
channel.QueueDeclare("queue.image", false, false, true);
channel.QueueDeclare("queue.unrouted", false, false, true);

channel.ExchangeDeclare("ex.fanout", "fanout", true, false);
channel.ExchangeDeclare("ex.direct", "direct", true, false, new Dictionary<string, object> //ex.fanout exchange'i ex.direct exchange'ine alternate-exchange olarak atanıyor.
{
    {"alternate-exchange", "ex.fanout"}
});

channel.QueueBind("queue.video", "ex.direct", "video");
channel.QueueBind("queue.image", "ex.direct", "image");
channel.QueueBind("queue.unrouted", "ex.fanout", string.Empty);

channel.BasicPublish("ex.direct", "video", false, null, Encoding.UTF8.GetBytes("This is video message."));
channel.BasicPublish("ex.direct", "image", false, null, Encoding.UTF8.GetBytes("This is an image message."));
channel.BasicPublish("ex.direct", "image", false, null, Encoding.UTF8.GetBytes("This is a text message."));

Console.WriteLine("Press any key to exit.");
Console.ReadKey();