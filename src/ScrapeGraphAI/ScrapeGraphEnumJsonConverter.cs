using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScrapeGraphAI;

/// <summary>Converts ScrapeGraphAI request enums to and from documented API wire values.</summary>
public sealed class ScrapeGraphEnumJsonConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a string value for {typeof(TEnum).Name}.");
        }

        var wireValue = reader.GetString();
        if (wireValue is not null && ScrapeGraphEnumWireValues.TryFromWireValue(wireValue, out TEnum value))
        {
            return value;
        }

        throw new JsonException($"Unsupported ScrapeGraphAI {typeof(TEnum).Name} wire value '{wireValue}'.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        => writer.WriteStringValue(ScrapeGraphEnumWireValues.ToWireValue(value));
}

internal static class ScrapeGraphEnumWireValues
{
    public static bool TryFromWireValue<TEnum>(string wireValue, out TEnum value)
        where TEnum : struct, Enum
    {
        if (typeof(TEnum) == typeof(ScrapeContentMode))
        {
            return wireValue switch
            {
                "normal" => TrySet(ScrapeContentMode.Normal, out value),
                "reader" => TrySet(ScrapeContentMode.Reader, out value),
                "prune" => TrySet(ScrapeContentMode.Prune, out value),
                _ => TrySetDefault(out value)
            };
        }

        if (typeof(TEnum) == typeof(ScrapeFormatType))
        {
            return wireValue switch
            {
                "markdown" => TrySet(ScrapeFormatType.Markdown, out value),
                "html" => TrySet(ScrapeFormatType.Html, out value),
                "links" => TrySet(ScrapeFormatType.Links, out value),
                "images" => TrySet(ScrapeFormatType.Images, out value),
                "summary" => TrySet(ScrapeFormatType.Summary, out value),
                "json" => TrySet(ScrapeFormatType.Json, out value),
                "branding" => TrySet(ScrapeFormatType.Branding, out value),
                "screenshot" => TrySet(ScrapeFormatType.Screenshot, out value),
                _ => TrySetDefault(out value)
            };
        }

        if (typeof(TEnum) == typeof(FetchMode))
        {
            return wireValue switch
            {
                "auto" => TrySet(FetchMode.Auto, out value),
                "fast" => TrySet(FetchMode.Fast, out value),
                "js" => TrySet(FetchMode.Js, out value),
                _ => TrySetDefault(out value)
            };
        }

        if (typeof(TEnum) == typeof(SearchResultFormat))
        {
            return wireValue switch
            {
                "markdown" => TrySet(SearchResultFormat.Markdown, out value),
                "html" => TrySet(SearchResultFormat.Html, out value),
                _ => TrySetDefault(out value)
            };
        }

        if (typeof(TEnum) == typeof(SearchTimeRange))
        {
            return wireValue switch
            {
                "past_hour" => TrySet(SearchTimeRange.PastHour, out value),
                "past_24_hours" => TrySet(SearchTimeRange.Past24Hours, out value),
                "past_week" => TrySet(SearchTimeRange.PastWeek, out value),
                "past_month" => TrySet(SearchTimeRange.PastMonth, out value),
                "past_year" => TrySet(SearchTimeRange.PastYear, out value),
                _ => TrySetDefault(out value)
            };
        }

        if (typeof(TEnum) == typeof(SearchLocationGeoCode))
        {
            return wireValue switch
            {
                "ae" => TrySet(SearchLocationGeoCode.Ae, out value),
                "ar" => TrySet(SearchLocationGeoCode.Ar, out value),
                "at" => TrySet(SearchLocationGeoCode.At, out value),
                "au" => TrySet(SearchLocationGeoCode.Au, out value),
                "be" => TrySet(SearchLocationGeoCode.Be, out value),
                "br" => TrySet(SearchLocationGeoCode.Br, out value),
                "ca" => TrySet(SearchLocationGeoCode.Ca, out value),
                "ch" => TrySet(SearchLocationGeoCode.Ch, out value),
                "cl" => TrySet(SearchLocationGeoCode.Cl, out value),
                "cn" => TrySet(SearchLocationGeoCode.Cn, out value),
                "co" => TrySet(SearchLocationGeoCode.Co, out value),
                "cz" => TrySet(SearchLocationGeoCode.Cz, out value),
                "de" => TrySet(SearchLocationGeoCode.De, out value),
                "dk" => TrySet(SearchLocationGeoCode.Dk, out value),
                "eg" => TrySet(SearchLocationGeoCode.Eg, out value),
                "es" => TrySet(SearchLocationGeoCode.Es, out value),
                "fi" => TrySet(SearchLocationGeoCode.Fi, out value),
                "fr" => TrySet(SearchLocationGeoCode.Fr, out value),
                "gb" => TrySet(SearchLocationGeoCode.Gb, out value),
                "gr" => TrySet(SearchLocationGeoCode.Gr, out value),
                "hk" => TrySet(SearchLocationGeoCode.Hk, out value),
                "hu" => TrySet(SearchLocationGeoCode.Hu, out value),
                "id" => TrySet(SearchLocationGeoCode.Id, out value),
                "ie" => TrySet(SearchLocationGeoCode.Ie, out value),
                "il" => TrySet(SearchLocationGeoCode.Il, out value),
                "in" => TrySet(SearchLocationGeoCode.In, out value),
                "it" => TrySet(SearchLocationGeoCode.It, out value),
                "jp" => TrySet(SearchLocationGeoCode.Jp, out value),
                "kr" => TrySet(SearchLocationGeoCode.Kr, out value),
                "mx" => TrySet(SearchLocationGeoCode.Mx, out value),
                "my" => TrySet(SearchLocationGeoCode.My, out value),
                "ng" => TrySet(SearchLocationGeoCode.Ng, out value),
                "nl" => TrySet(SearchLocationGeoCode.Nl, out value),
                "no" => TrySet(SearchLocationGeoCode.No, out value),
                "nz" => TrySet(SearchLocationGeoCode.Nz, out value),
                "pe" => TrySet(SearchLocationGeoCode.Pe, out value),
                "ph" => TrySet(SearchLocationGeoCode.Ph, out value),
                "pk" => TrySet(SearchLocationGeoCode.Pk, out value),
                "pl" => TrySet(SearchLocationGeoCode.Pl, out value),
                "pt" => TrySet(SearchLocationGeoCode.Pt, out value),
                "ro" => TrySet(SearchLocationGeoCode.Ro, out value),
                "ru" => TrySet(SearchLocationGeoCode.Ru, out value),
                "sa" => TrySet(SearchLocationGeoCode.Sa, out value),
                "se" => TrySet(SearchLocationGeoCode.Se, out value),
                "sg" => TrySet(SearchLocationGeoCode.Sg, out value),
                "th" => TrySet(SearchLocationGeoCode.Th, out value),
                "tr" => TrySet(SearchLocationGeoCode.Tr, out value),
                "tw" => TrySet(SearchLocationGeoCode.Tw, out value),
                "ua" => TrySet(SearchLocationGeoCode.Ua, out value),
                "us" => TrySet(SearchLocationGeoCode.Us, out value),
                "vn" => TrySet(SearchLocationGeoCode.Vn, out value),
                "za" => TrySet(SearchLocationGeoCode.Za, out value),
                _ => TrySetDefault(out value)
            };
        }

        if (typeof(TEnum) == typeof(HistoryService))
        {
            return wireValue switch
            {
                "scrape" => TrySet(HistoryService.Scrape, out value),
                "extract" => TrySet(HistoryService.Extract, out value),
                "search" => TrySet(HistoryService.Search, out value),
                "monitor" => TrySet(HistoryService.Monitor, out value),
                "crawl" => TrySet(HistoryService.Crawl, out value),
                "schema" => TrySet(HistoryService.Schema, out value),
                _ => TrySetDefault(out value)
            };
        }

        return TrySetDefault(out value);
    }

    public static string ToWireValue<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => value switch
        {
            ScrapeContentMode.Normal => "normal",
            ScrapeContentMode.Reader => "reader",
            ScrapeContentMode.Prune => "prune",

            ScrapeFormatType.Markdown => "markdown",
            ScrapeFormatType.Html => "html",
            ScrapeFormatType.Links => "links",
            ScrapeFormatType.Images => "images",
            ScrapeFormatType.Summary => "summary",
            ScrapeFormatType.Json => "json",
            ScrapeFormatType.Branding => "branding",
            ScrapeFormatType.Screenshot => "screenshot",

            FetchMode.Auto => "auto",
            FetchMode.Fast => "fast",
            FetchMode.Js => "js",

            SearchResultFormat.Markdown => "markdown",
            SearchResultFormat.Html => "html",

            SearchTimeRange.PastHour => "past_hour",
            SearchTimeRange.Past24Hours => "past_24_hours",
            SearchTimeRange.PastWeek => "past_week",
            SearchTimeRange.PastMonth => "past_month",
            SearchTimeRange.PastYear => "past_year",

            SearchLocationGeoCode.Ae => "ae",
            SearchLocationGeoCode.Ar => "ar",
            SearchLocationGeoCode.At => "at",
            SearchLocationGeoCode.Au => "au",
            SearchLocationGeoCode.Be => "be",
            SearchLocationGeoCode.Br => "br",
            SearchLocationGeoCode.Ca => "ca",
            SearchLocationGeoCode.Ch => "ch",
            SearchLocationGeoCode.Cl => "cl",
            SearchLocationGeoCode.Cn => "cn",
            SearchLocationGeoCode.Co => "co",
            SearchLocationGeoCode.Cz => "cz",
            SearchLocationGeoCode.De => "de",
            SearchLocationGeoCode.Dk => "dk",
            SearchLocationGeoCode.Eg => "eg",
            SearchLocationGeoCode.Es => "es",
            SearchLocationGeoCode.Fi => "fi",
            SearchLocationGeoCode.Fr => "fr",
            SearchLocationGeoCode.Gb => "gb",
            SearchLocationGeoCode.Gr => "gr",
            SearchLocationGeoCode.Hk => "hk",
            SearchLocationGeoCode.Hu => "hu",
            SearchLocationGeoCode.Id => "id",
            SearchLocationGeoCode.Ie => "ie",
            SearchLocationGeoCode.Il => "il",
            SearchLocationGeoCode.In => "in",
            SearchLocationGeoCode.It => "it",
            SearchLocationGeoCode.Jp => "jp",
            SearchLocationGeoCode.Kr => "kr",
            SearchLocationGeoCode.Mx => "mx",
            SearchLocationGeoCode.My => "my",
            SearchLocationGeoCode.Ng => "ng",
            SearchLocationGeoCode.Nl => "nl",
            SearchLocationGeoCode.No => "no",
            SearchLocationGeoCode.Nz => "nz",
            SearchLocationGeoCode.Pe => "pe",
            SearchLocationGeoCode.Ph => "ph",
            SearchLocationGeoCode.Pk => "pk",
            SearchLocationGeoCode.Pl => "pl",
            SearchLocationGeoCode.Pt => "pt",
            SearchLocationGeoCode.Ro => "ro",
            SearchLocationGeoCode.Ru => "ru",
            SearchLocationGeoCode.Sa => "sa",
            SearchLocationGeoCode.Se => "se",
            SearchLocationGeoCode.Sg => "sg",
            SearchLocationGeoCode.Th => "th",
            SearchLocationGeoCode.Tr => "tr",
            SearchLocationGeoCode.Tw => "tw",
            SearchLocationGeoCode.Ua => "ua",
            SearchLocationGeoCode.Us => "us",
            SearchLocationGeoCode.Vn => "vn",
            SearchLocationGeoCode.Za => "za",

            HistoryService.Scrape => "scrape",
            HistoryService.Extract => "extract",
            HistoryService.Search => "search",
            HistoryService.Monitor => "monitor",
            HistoryService.Crawl => "crawl",
            HistoryService.Schema => "schema",

            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported ScrapeGraphAI enum value.")
        };

    private static bool TrySet<TEnum>(object enumValue, out TEnum value)
        where TEnum : struct, Enum
    {
        value = (TEnum)enumValue;
        return true;
    }

    private static bool TrySetDefault<TEnum>(out TEnum value)
        where TEnum : struct, Enum
    {
        value = default;
        return false;
    }
}
