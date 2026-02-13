using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notifications.Consumer
{
    public sealed class RabbitMqOptions
    {
        public string Host { get; set; } = "localhost";
        public string User { get; set; } = "guest";
        public string Pass { get; set; } = "guest";
        public string VHost { get; set; } = "/";
        public string ExchangeName { get; set; } = "fintech.events";
        public string QueueName { get; set; } = "notifications.transfer.completed";
        public string RoutingKey { get; set; } = "transfer.completed";
    }
}
