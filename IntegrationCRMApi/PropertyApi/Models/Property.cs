namespace PropertyApi.Models;

public class Property
{
    public Guid Id { get; set; }
    public string? PropertyId { get; set; }
    public string? Name { get; set; }
    public string? PropertyType { get; set; }
    public int? NumberOfRooms { get; set; }
    public string? Location { get; set; }
    public decimal? AverageDailyRate { get; set; }
    public decimal? RatingStars { get; set; }
    public int? StateCode { get; set; }
    public string? State { get; set; }
    public DateTime? CreatedOn { get; set; }
}
