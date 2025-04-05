using System;
using jpgOpt.App.Enums;

namespace jpgOpt.App.Models;

public class AppNotification
{
    public Guid Id { get; set; }

    public DateTime NotifiedAtUtc { get; set; }

    public NotificationType Type { get; set; }

    public string Message { get; set; } = null!;
}