namespace PropertyApi.Common;

public class PageRequest
{
    public PageRequest(int page, int size)
    {
        Page = page;
        Size = size;
    }

    public int Page { get; }
    public int Size { get; }
}

public static class PagingExtensions
{
    public const int DefaultPageSize = 100;
    public const int MaxPageSize = 1000;

    public static bool TryGetPage(this HttpRequest request, out PageRequest page, out string error)
    {
        page = null;

        if (!TryReadInt(request, "page", 1, out var number)) {
            error = "page must be a whole number.";
            return false;
        }

        if (!TryReadInt(request, "pageSize", DefaultPageSize, out var size)) {
            error = "pageSize must be a whole number.";
            return false;
        }

        if (number < 1) {
            error = "page must be 1 or more.";
            return false;
        }

        if (size < 1 || size > MaxPageSize) {
            error = $"pageSize must be between 1 and {MaxPageSize}.";
            return false;
        }

        page = new PageRequest(number, size);
        error = null;
        return true;
    }

    private static bool TryReadInt(HttpRequest request, string name, int fallback, out int value)
    {
        if (!request.Query.TryGetValue(name, out var raw) || string.IsNullOrWhiteSpace(raw)) {
            value = fallback;
            return true;
        }

        return int.TryParse(raw, out value);
    }
}
