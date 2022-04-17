# Test task for [Veeam](http://www.veeam.com/) in C#/.NET

## Заданиe

Написать клиент-серверную систему, работающую по следующему алгоритму:
1. Сервер держит открытыми порты 8000 и 8001.
2. При запуске клиент выбирает для себя уникальный идентификатор.
3. Клиент подключается к серверу к порту 8000, передает ему свой идентификатор и
   получает от сервера уникальный код.
4. Клиент подключается к серверу к порту 8001 и передает произвольное текстовое
   сообщение, свой идентификатор и код, полученный на шаге 2.
5. Если переданный клиентом код не соответствует его уникальному идентификатору,
   сервер возвращает клиенту сообщение об ошибке.
6. Если код передан правильно, сервер записывает полученное сообщение в лог.
   Сервер должен поддерживать возможность одновременной работы с хотя бы 50
   клиентами.

Для реализации взаимодействия между сервером и клиентом системы допускается (но не
требуется) использование высокоуровнего протокола (например, HTTP).

## Реализация

Реализация основана на использовании двух TCP серверов (TcpListener) на разных портах.
Для аутентификации клиент должен передать свой идентификатор с символом перевода строки
в конце сообщения на сервер с портом 8000 (можно изменить). Сервер ответит постоянным 
токеном (также с символом LF, для удобства чтения), который клиент сможет использовать
для отправки своего сообщения на сервер с портом 8001. Формат сообщения —
"{message}\n{identity}\n{token}\n", Где message — сообщение, которое требуется залогировать.
В случае, если токен соответствует identity, сообщение будет залогировано (в т.ч. в файл,
путь к которому также можно сконфигурировать).

## Пример клиента на 10000 соединений

```c#
using System.Net.Sockets;
using System.Text;

static class Program
{
    public static string Login(string identity)
    {
        using var client = new TcpClient("127.0.0.1", 8000);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        stream.Write(Encoding.ASCII.GetBytes($"{identity}\n"));
        var token = reader.ReadLine();
        return token;
    }

    static string SaveMessage(string message, string identity, string token)
    {
        using var client = new TcpClient("127.0.0.1", 8001);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        stream.Write(Encoding.ASCII.GetBytes($"{message}\n{identity}\n{token}\n"));
        var response = reader.ReadLine();
        return response;
    }

    public static readonly Random Generator = new Random();

    static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Generator.Next(s.Length)]).ToArray());
    }


    static void Main(string[] args)
    {
        var tasks = new List<Task>();

        for (int i = 0; i < 10000; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var identity = RandomString(20);
                var token = Login(identity);
                Console.WriteLine("token = '{0}'", token);
                var response = SaveMessage("Hey! Bye!", identity + "1", token);
                Console.WriteLine("response = '{0}'", response);
            }));
        }

        Task.WhenAll(tasks).Wait();
    }
}
```
