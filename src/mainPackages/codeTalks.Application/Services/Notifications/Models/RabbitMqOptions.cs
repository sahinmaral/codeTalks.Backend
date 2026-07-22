namespace codeTalks.Application.Services.Notifications.Models;

public class RabbitMqOptions
{
    public string Host { get; set; }
    public int Port { get; set; } = 5672;
    public string Username { get; set; }
    public string Password { get; set; }
}