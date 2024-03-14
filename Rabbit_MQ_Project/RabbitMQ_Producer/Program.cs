using RabbitMQ.Client;
using System.Text;


//---------------------------------------------------------Ortak-------------------------------------------------------------------------------

//RabbitMQ, iletişim halinde olan sistemler arasındaki asenkron mesaj kuyruk sistemidir. Bir mesaj oluşturulur ve bu mesaj kabul edilerek ilgili kuyruğa tüketilmek üzere iletilir.
//FIFO (First In First Out) mantığında BLOB(Binary Large Object) olarak taşımaktadır. Producer mesajı ileten, Consumer mesajı alan rollerdir.

//Producer -> Mesajı kuyruğa gönderen uygulamadır.
//Consumer -> Dinlediği kuyruktaki mesajı alan uygulamadır.
//Queue    -> Mesajların eklendiği, tüketildiği ve tutulduğu listedir.
//Exchange -> Producer tarafından üretilen bir mesaj doğrudan alıcıya veya queue 'ya gönderilemez. Arada bir mesaj yöneticisi bulunmaktadır. İşte bu yönetici Exchange'dir.
//Mesaj önce producer tarafından exchange'e iletilir. Exchange, ilgili mesajı Routing Key 'e göre ilgili Queue 'ya iletir.
//Binding -> Exchange ile Queue arasında kurulacak bağlantının yönlendirme kuralıdır. Exchange aldığı mesajları bu kurala göre kuyruklara dağıtır.
//Exchange Type -> Mesajın kuyruğa hangi yönteme göre ileteceğini belirttiği tiptir. 4 çeşit exchange tipi bulunur.(Direct,Topic,Fanout, Header exchange)

//(RabbitMQ.Client'ın sınıfının interfacesi)ConnectionFactory sınıfı ile localhost adresine, VHost üzerinden varsayılan yetkilendirme bilgileriyle bir bağlantı oluşturulacak nesne örneği yaratılıyor.


var factory = new ConnectionFactory()
{
    HostName = "localhost",
    VirtualHost = "/",
    Port = 5672,
    UserName = "guest",
    Password = "guest"
};

//Bu nesene aracılığı ile bir bağlantı açılıyor ve (RabbitMQ.Client'ın sınıfının interfacesi)IConnectiion interface tipinde bir nesne elde ediliyor.
using var connection = factory.CreateConnection();

//Açılan bağlantı ile (RabbitMQ.Client'ın sınıfının interfacesi)IModel interfacesi sayesinde mesajlaşma işlemini gerçekleştirecek bir channel oluşturuyoruz.
using var channel = connection.CreateModel();


//---------------------------------------------------------Direct Exchange-------------------------------------------------------------------------------
//Mesaj gönderilirken belirtilen Routing Key ile eşleşen Binding(Queue bir exchange atanırken belirlenmiş key) ile ilgili Queue'ya aktarılır.
//Sistem tarafından default olarak kullanılan bir exchange türüdür.


//QueueDeclare ve ExchangeDeclare aldıkları parametreler.
//exchange veya queue -> verilecek isimdir.
//type -> exchange için kullanılır. Exchange tipini belirtir (direct veya header v.b.)
//Durable -> Varsayılan olarak Transient yani in-memory olarak tutulmaktadır.RabbitMq servisi dursa bile Queue'lar kaybolmayacaktır. Fakat latency problemi bulunmaktadır. Kuyrukların broker restart olduğunda persistence olması isteniyorsa bu özellik true olarak ayarlanmalıdır.
//Exclusive -> Bu özellik true olarak ayarlanırsa yalnızca kendi oluşturulduğu connection tarafından kullanılabilir.
//AutoDelete -> Bu özellik true olarak ayarlanırsa son consumer kuyruktan unsubscribe olduğunda kuyruk silinir.


channel.ExchangeDeclare(exchange: "ex.direct", type: "direct", durable: true, autoDelete: true); //ExchangeDeclare ile exchange oluşturuyoruz. 1. parametre -> exchange ismi , 2. parametre -> Exchange tipi , 3. parametre -> durable, 4. parametre -> auto-delete

channel.QueueDeclare(queue: "log.error", durable: true, exclusive: false, autoDelete: false); //QueueDeclare ile queue'ları oluşturuyoruz. 1. parametre -> queue ismi , 2. parametre -> durable , 3. parametre -> exclusive , 4. parametre -> auto-delete 
channel.QueueDeclare(queue: "log.warning", durable: true, exclusive: false, autoDelete: false);
channel.QueueDeclare(queue: "log.info", durable: true, exclusive: false, autoDelete: false);

channel.QueueBind(queue: "log.error", exchange: "ex.direct", routingKey:"error"); //QueueBind ile RoutingKey'leri exchange'e bağlama işlemibni yapıyoruz.  1. parametre -> queue ismi , 2. parametre -> exchange ismi , 3. parametre RoutingKey
channel.QueueBind(queue: "log.warning", exchange: "ex.direct", routingKey:"warning");
channel.QueueBind(queue: "log.info", exchange: "ex.direct", routingKey: "info");

channel.BasicPublish(exchange: "ex.direct", routingKey: "info", basicProperties:null, body: Encoding.UTF8.GetBytes("This is an info message."));//BasicPublish ile mesajın hangi exchange'e ve o exchange'den de hangi kuyruğa gideceğini belirtiyoruz. 1. parametre -> exchange ismi , 2. parametre RoutingKey , 3. parametre -> temel özellikler , 4. parametre -> Queue'ya gönderilecek mesajdır. Byte olarak çevirilmelidir.
channel.BasicPublish(exchange: "ex.direct", routingKey: "error", basicProperties: null, body: Encoding.UTF8.GetBytes("This is an error message."));
channel.BasicPublish(exchange: "ex.direct", routingKey: "warning", basicProperties: null, body: Encoding.UTF8.GetBytes("This is a warning message."));

Console.WriteLine("Press any key to exit.");
Console.ReadKey();

channel.QueueDelete("log.error"); //Kuyruk siliniyor.
channel.QueueDelete("log.info");
channel.QueueDelete("log.warning");

channel.ExchangeDelete("ex.direct"); //Exchange siliniyor.
//----------------------------------------------------------------------------------------------------------------------------------------




//--------------------------------------------------------------------Topic Exchange--------------------------------------------------------------------

//Topic exchange, direct exchange’in aksine birden fazla Queue’ya mesaj iletebilir.
//Yine aynı şekilde gönderilen mesajdaki routing key, eşleşen Binding keye sahip queue’ya aktarılır.
//Ancak binding key ile routing key, noktalarla ayrılmış şekilde kelimelerden oluşmalıdır. (Direct exchange’te böyle bir zorunluluk yok.) Kelime 255 byte’a kadar herhangi bir metin olabilir. Binding key tanımlanırken regex’ten de aşina olduğumuz iki özel durum söz konusudur.

//routingKey -> log olan aşağıdaki queue'lardan biri ile eşleşmediğinden mesaj iletilmez.
//routingKey -> log.error olan aşağıdaki log.error queue'suna mesaj iletilir.
//routingKey -> log.info olan aşağıda bu isimde queue bulunmuyor fakat topic exchange olduğundan logs.all queue'suna mesaj iletilir. Çünkü routingKey'i log.* dır. Bu da 'log.' içerdiğinden routingKey otomatik eşleşecektir.
//routingKey -> test.warning" olan aşağıda bu isimde queue bulunmuyor fakat topic exchange olduğundan all.warnings queue'suna mesaj iletilir. Çünkü routingKey'i "#.warning dır. Bu da '.warning' içerdiğinden routingKey otomatik eşleşecektir.
//routingKey -> warning" olan aşağıda bu isimde queue bulunmuyor fakat topic exchange olduğundan all.warnings queue'suna mesaj iletilir. Çünkü routingKey'i "#.warning dır. Bu da '.warning' içerdiğinden routingKey otomatik eşleşecektir.


channel.ExchangeDeclare("ex.topic", "topic", false, true);

channel.QueueDeclare("log.error", false, false, true);
channel.QueueDeclare("logs.all", false, false, true);
channel.QueueDeclare("all.warnings", false, false, true);

channel.QueueBind("log.error", "ex.topic", "log.error");
channel.QueueBind("logs.all", "ex.topic", "log.*");
channel.QueueBind("all.warnings", "ex.topic", "#.warning");

channel.BasicPublish("ex.topic", "log", null, Encoding.UTF8.GetBytes("This is a log message"));
channel.BasicPublish("ex.topic", "log.error", null, Encoding.UTF8.GetBytes("This is an error message"));
channel.BasicPublish("ex.topic", "log.info", null, Encoding.UTF8.GetBytes("This is an info message"));
channel.BasicPublish("ex.topic", "test.warning", null, Encoding.UTF8.GetBytes("This is a test warning message"));
channel.BasicPublish("ex.topic", "warning", null, Encoding.UTF8.GetBytes("This is a warning message"));

Console.WriteLine("Press any key to exit.");
Console.ReadKey();

channel.QueueDelete("log.error");
channel.QueueDelete("logs.all");
channel.QueueDelete("all.warnings");
channel.ExchangeDelete("ex.topic");


//----------------------------------------------------------------------------------------------------------------------------------------





//------------------------------------------------------------Fanout Exchange----------------------------------------------------------------------------
//Bu exchange türünde herhangi bir routingKey'e ihtiyaç yoktur. Mesaj tüm queue'lara kopyalanarak dağıtılır.

channel.ExchangeDeclare("ex.fanout", "fanout", false, true);

channel.QueueDeclare("queue.one", false, false, true);
channel.QueueDeclare("queue.two", false, false, true);

channel.QueueBind("queue.one", "ex.fanout", string.Empty); //RoutingKey alanını boş geçiyoruz.
channel.QueueBind("queue.two", "ex.fanout", string.Empty);

channel.BasicPublish("ex.fanout", string.Empty, false, null, Encoding.UTF8.GetBytes("This is a first fanout message."));
channel.BasicPublish("ex.fanout", string.Empty, false, null, Encoding.UTF8.GetBytes("This is a second fanout message."));

Console.WriteLine("Press any key to exit.");
Console.ReadKey();

channel.QueueDelete("queue.one");
channel.QueueDelete("queue.two");
channel.ExchangeDelete("ex.fanout");

//----------------------------------------------------------------------------------------------------------------------------------------





//------------------------------------------------------------Header Exchange----------------------------------------------------------------------------
//Header Exchange, Direct Exchange’e çok benzer, fakat iletişim routing key yerine header key-value’larına göre yapılmaktadır.
//Exchange, gelen mesajları hangi queue’ya yönlendireceğine karar verebilmesi için binding key ayrıca x-match başlığını barındırmalıdır.
//Bu argüman(x-match) all veya any değerini alabilir. all değerini alması durumunda tüm değerlerinin eşleşmesi, any olması durumunda bir değerin eşleşmesi yeterlidir.

channel.ExchangeDeclare("ex.headers", "headers", false, true);

channel.QueueDeclare("queue.headerOne", false, false, true);
channel.QueueDeclare("queue.headerTwo", false, false, true);
channel.QueueDeclare("queue.headerThree", false, false, true);

channel.QueueBind("queue.headerOne", "ex.headers", string.Empty, new Dictionary<string, object>
{
    {"x-match", "all"},
    {"op", "convert"},
    {"format", "jpeg"}
});
channel.QueueBind("queue.headerTwo", "ex.headers", string.Empty, new Dictionary<string, object>
{
    {"x-match", "all"},
    {"op", "convert"},
    {"format", "png"}
});
channel.QueueBind("queue.headerThree", "ex.headers", string.Empty, new Dictionary<string, object>
{
    {"x-match", "any"},
    {"op", "convert"},
    {"format", "bitmap"}
});

var props = channel.CreateBasicProperties();
props.Headers = new Dictionary<string, object>()
{
    {"op", "convert"},
    {"format", "jpeg"}
};
channel.BasicPublish("ex.headers", string.Empty, false, props, Encoding.UTF8.GetBytes("This is a jpeg convert operation."));

props = channel.CreateBasicProperties();
props.Headers = new Dictionary<string, object>
{
    {"op", "convert"},
    {"format", "gif"}
};
channel.BasicPublish("ex.headers", string.Empty, props, Encoding.UTF8.GetBytes("This is a gif convert operation."));

Console.WriteLine("Press any key to exit.");
Console.ReadKey();

channel.QueueDelete("queue.headerOne");
channel.QueueDelete("queue.headerTwo");
channel.QueueDelete("queue.headerThree");
channel.ExchangeDelete("ex.headers");


//----------------------------------------------------------------------------------------------------------------------------------------


//---------------------------------------------------------Ortak-------------------------------------------------------------------------------
channel.Close(); //Kanal kapatılıyor.
connection.Close(); //Bağlantı kapatılıyor.


//imdiye kadar RabbitMQ ile AMQP 0-9-1 protokolü üzerinden 5672 portuyla haberleştik,
//ancak 15672 portu üzerinden HTTP API istemcilerine ve Management UI sistemine erişebiliriz. 
//Bunun için localhost:15672 adresini kullanmalıyız. Giriş yaparken daha önce de kullandığımız guest/guest default bilgilerini kullanarak sisteme giriş yapıyoruz. Bu ui aracılığıyla aşağıdaki gibi işlemleri gerçekleştirebiliriz.

//-Mevcut queue mesajlarına bakabilir,
//-Bağlantılara ve channel’lara göz atabilir,
//-VHost’lara bakabilir yeni bir tane oluşturabilir,
//-Exchange’lere bakabilir veya yeni bir tane oluşturabilir,
//-Queue’lara bakabilir ve bir queue’yu mevcut exchange’e bind ederek yeni bir mesaj produce edip aynı şekilde consume edebilir,
//-Yeni bir kullanıcı ekleyebilir, yetkilendirme işlemlerini yapabiliriz.



//------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/*
Person person = new Person() { Id = 1, Name = "Cemal", SurName = "Akkuş", BirthDate = new DateTime(1992, 1, 1), Message = "İlgili aday yakınımdır :)" };

var factoryTest = new ConnectionFactory() { HostName = "localhost" };

using (IConnection connectionTest = factoryTest.CreateConnection())

using (IModel channelTest = connectionTest.CreateModel())
{
    channelTest.QueueDeclare(
        queue: "CmlMsgQue",
        durable: false,
        exclusive: false,
        autoDelete: false,
        arguments: null
        );

    string messageTest = JsonConvert.SerializeObject(person);

    var bodyTest = Encoding.UTF8.GetBytes(messageTest);

    channelTest.BasicPublish(
        exchange: "",
        routingKey: "CmlMsgQue",
        basicProperties: null,
        body: bodyTest
        );

    Console.WriteLine($"Gönderilen Kişi: {person.Name}-{person.SurName}");
}

Console.WriteLine("İlgili Kişi Gönderildi.");
Console.ReadLine();
*/