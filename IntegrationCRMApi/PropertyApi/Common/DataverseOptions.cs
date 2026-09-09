namespace PropertyApi.Common;

public class DataverseOptions
{
    [Required]
    [Url]
    public string DataverseUrl { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;
}
