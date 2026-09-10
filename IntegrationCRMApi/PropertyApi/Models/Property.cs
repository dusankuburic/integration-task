namespace PropertyApi.Models;

public class Property
{
    public Guid Id { get; init; }
    public string PropertyId { get; init; }
    public string Name { get; init; }
    public string PropertyType { get; init; }
    public int? NumberOfRooms { get; init; }
    public string Location { get; init; }
    public decimal? AverageDailyRate { get; init; }
    public decimal? RatingStars { get; init; }
    public DateTime? CreatedOn { get; init; }
}