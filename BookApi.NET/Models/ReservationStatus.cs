using System.Text.Json.Serialization;

namespace BookApi.NET.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReservationStatus
{
    Active,
    Cancelled
}