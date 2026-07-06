namespace codeTalks.Application.Services.Notifications.Models;

public record DeliveryDecision(
    DeliveryMode Mode,
    bool WithSound
);