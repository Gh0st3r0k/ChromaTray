using System.Drawing;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChromaTray.Services;

/// <summary>Requests five-color palettes over HTTPS, preserving locally locked colors exactly.</summary>
internal sealed class ColormindClient(HttpClient http)
{
    internal static readonly Uri Endpoint = new("https://colormind.io/api/");

    internal async Task<Color[]> GenerateAsync(IReadOnlyList<Color?> locked, CancellationToken cancellationToken)
    {
        if (locked.Count != 5)
        {
            throw new ArgumentException("A palette must contain five positions.", nameof(locked));
        }
        // Snapshot before awaiting: edits must not affect an in-flight request.
        var inputColors = locked.ToArray();
        if (inputColors.All(color => color.HasValue))
        {
            return inputColors.Select(color => color!.Value).ToArray();
        }
        object[] input = inputColors.Select(color => color is { } rgb
            ? (object)new[] { (int)rgb.R, rgb.G, rgb.B } : "N").ToArray();
        using var response = await http.PostAsJsonAsync(Endpoint, new
        {
            model = "ui",
            input
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<PaletteResponse>(cancellationToken: cancellationToken);
        if (payload?.Result is not { Length: 5 } result ||
            result.Any(rgb => rgb is null || rgb.Length != 3 || rgb.Any(channel => channel < 0 || channel > 255)))
        {
            throw new InvalidDataException("Colormind returned an invalid five-color palette.");
        }
        return result.Select((rgb, index) => inputColors[index] ?? Color.FromArgb(rgb[0], rgb[1], rgb[2])).ToArray();
    }

    private sealed class PaletteResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("result")]
        public int[][]? Result
        {
            get; set;
        }
    }
}
