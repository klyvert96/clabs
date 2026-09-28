
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace BookColorPalette;

internal static class Program
{
    // file name 
    private const string InputFileName = "Podarok.txt";

    private const string GridOutputFileName = "palette_grid.png";
    private const string LineOutputFileName = "palette_line.png";

    private const int SquareSize = 36;
    private const int MaxGridColumns = 18;

    private static int Main()
    {
        try
        {
            string folder = AppContext.BaseDirectory;
            string inputPath = Path.Combine(folder, InputFileName);

            string text = TextReader.Read(inputPath);
            if (string.IsNullOrWhiteSpace(text))
                Console.WriteLine($"Warning: file \"{InputFileName}\" is empty or contains no text.");

            var mentions = ColorLexicon.FindMentions(text);

           
            PaletteRenderer.SavePng(mentions, Path.Combine(folder, GridOutputFileName), SquareSize, MaxGridColumns, VisualizationScheme.Grid);
            PaletteRenderer.SavePng(mentions, Path.Combine(folder, LineOutputFileName), SquareSize, MaxGridColumns, VisualizationScheme.Line);

            StatisticsPrinter.Print(mentions);

            Console.WriteLine();
            Console.WriteLine($"Done. Saved: {GridOutputFileName}, {LineOutputFileName}");
            return 0;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"Error: input file not found. {ex.Message}");
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine($"Error: file access denied. {ex.Message}");
            return 1;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"I/O error: {ex.Message}");
            return 1;
        }
        catch (OverflowException)
        {
            Console.Error.WriteLine("Error: too many color mentions — the resulting image would exceed the allowed size.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            return 1;
        }
    }
}

internal enum VisualizationScheme
{
    Grid,
    Line
}

internal static class TextReader
{
    public static string Read(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File \"{Path.GetFileName(path)}\" was not found at path: {path}", path);

        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length == 0)
            return string.Empty;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        try
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(RemoveUtf8Bom(bytes));
        }
        catch (DecoderFallbackException)
        {
            
            return Encoding.GetEncoding(1251).GetString(bytes);
        }
    }

    private static byte[] RemoveUtf8Bom(byte[] bytes) =>
        bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? bytes[3..] : bytes;
}

internal readonly record struct Rgb(byte Red, byte Green, byte Blue);
internal sealed record ColorMention(string Name, Rgb Color);

internal static class ColorLexicon
{
    
    private static readonly (string Prefix, string Name, Rgb Color)[] Entries =
    {
        ("красн", "Red", new(255, 0, 0)), ("ал", "Crimson", new(220, 20, 60)),
        ("багр", "Dark red", new(139, 0, 0)), ("зелен", "Green", new(0, 128, 0)),
        ("изумруд", "Emerald", new(60, 179, 113)), ("малахит", "Malachite", new(60, 179, 113)),
        ("син", "Blue", new(0, 0, 255)), ("голуб", "Light blue", new(173, 216, 230)),
        ("лазур", "Azure", new(135, 206, 235)), ("ультрамарин", "Ultramarine", new(0, 0, 255)),
        ("желт", "Yellow", new(255, 255, 0)), ("золот", "Gold", new(255, 215, 0)),
        ("лимон", "Lemon", new(255, 250, 205)), ("бел", "White", new(255, 255, 255)),
        ("черн", "Black", new(0, 0, 0)), ("сер", "Gray", new(128, 128, 128)),
        ("фиолетов", "Purple", new(128, 0, 128)), ("лилов", "Lilac", new(128, 0, 128)),
        ("оранжев", "Orange", new(255, 165, 0)), ("коричнев", "Brown", new(165, 42, 42)),
        ("розов", "Pink", new(255, 192, 203)), ("бирюз", "Turquoise", new(64, 224, 208))
    };

    
    private static readonly string[] ValidSuffixes =
    {
        "",
        "ый", "ий", "ой", "ая", "яя", "ое", "ее", "ые", "ие",
        "ого", "его", "ому", "ему", "ым", "им", "ом", "ем",
        "ую", "юю", "ыми", "ими", "ых", "их",
        "о", "а", "ы", "е"
    };

    private static readonly Regex WordPattern = new(@"\b[\p{IsCyrillic}a-zA-Z]+\b", RegexOptions.Compiled);

    public static List<ColorMention> FindMentions(string text)
    {
        var result = new List<ColorMention>();
        if (string.IsNullOrEmpty(text))
            return result;

        foreach (Match match in WordPattern.Matches(text))
        {
            string word = match.Value.ToLowerInvariant();

            foreach (var entry in Entries)
            {
                if (!word.StartsWith(entry.Prefix, StringComparison.Ordinal))
                    continue;

                string suffix = word[entry.Prefix.Length..];
                if (Array.IndexOf(ValidSuffixes, suffix) >= 0)
                {
                    result.Add(new ColorMention(entry.Name, entry.Color));
                    break;
                }
            }
        }
        return result;
    }
}

internal static class StatisticsPrinter
{
    public static void Print(IReadOnlyCollection<ColorMention> mentions)
    {
        Console.WriteLine($"Color mentions: {mentions.Count}");
        foreach (var item in mentions.GroupBy(x => x.Name).OrderByDescending(x => x.Count()).ThenBy(x => x.Key))
            Console.WriteLine($"{item.Key}: {item.Count()}");
    }
}

internal static class PaletteRenderer
{
    public static void SavePng(IReadOnlyList<ColorMention> mentions, string path, int squareSize, int maxColumns, VisualizationScheme scheme)
    {
        if (squareSize <= 0) throw new ArgumentOutOfRangeException(nameof(squareSize), "Square size must be positive.");
        if (maxColumns <= 0) throw new ArgumentOutOfRangeException(nameof(maxColumns), "Column count must be positive.");

        int count = Math.Max(mentions.Count, 1);
        int columns = scheme switch
        {
            
            VisualizationScheme.Line => count,
            
            VisualizationScheme.Grid => Math.Min(maxColumns, Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count)))),
            _ => throw new ArgumentOutOfRangeException(nameof(scheme))
        };

        int rows = (int)Math.Ceiling(count / (double)columns);
        int width = checked(columns * squareSize);
        int height = checked(rows * squareSize);
        byte[] pixels = new byte[checked(width * height * 3)];

        for (int index = 0; index < mentions.Count; index++)
        {
            int x0 = (index % columns) * squareSize;
            int y0 = (index / columns) * squareSize;
            FillSquare(pixels, width, x0, y0, squareSize, mentions[index].Color);
        }

       
        if (mentions.Count == 0)
            FillSquare(pixels, width, 0, 0, squareSize, new Rgb(240, 240, 240));

        string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        PngWriter.WriteRgb(path, width, height, pixels);
    }

    private static void FillSquare(byte[] pixels, int width, int x0, int y0, int size, Rgb color)
    {
        for (int y = y0; y < y0 + size; y++)
            for (int x = x0; x < x0 + size; x++)
            {
                int offset = (y * width + x) * 3;
                pixels[offset] = color.Red; pixels[offset + 1] = color.Green; pixels[offset + 2] = color.Blue;
            }
    }
}

internal static class PngWriter
{
    public static void WriteRgb(string path, int width, int height, byte[] pixels)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        WriteChunk(writer, "IHDR", BuildHeader(width, height));

        byte[] scanlines = new byte[height * (width * 3 + 1)];
        for (int row = 0; row < height; row++)
            Buffer.BlockCopy(pixels, row * width * 3, scanlines, row * (width * 3 + 1) + 1, width * 3);
        using var compressed = new MemoryStream();
        using (var zipper = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true)) zipper.Write(scanlines);
        WriteChunk(writer, "IDAT", compressed.ToArray());
        WriteChunk(writer, "IEND", Array.Empty<byte>());
    }

    private static byte[] BuildHeader(int width, int height)
    {
        var data = new byte[13];
        WriteInt32(data, 0, width); WriteInt32(data, 4, height); data[8] = 8; data[9] = 2;
        return data;
    }

    private static void WriteChunk(BinaryWriter writer, string type, byte[] data)
    {
        WriteBigEndian(writer, data.Length);
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        writer.Write(typeBytes); writer.Write(data);
        var crc = new Crc32(); crc.Update(typeBytes); crc.Update(data); WriteBigEndian(writer, unchecked((int)crc.Value));
    }

    private static void WriteInt32(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24); buffer[offset + 1] = (byte)(value >> 16); buffer[offset + 2] = (byte)(value >> 8); buffer[offset + 3] = (byte)value;
    }

    private static void WriteBigEndian(BinaryWriter writer, int value) => writer.Write(new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value });
}

internal sealed class Crc32
{
    private uint _value = 0xFFFFFFFF;
    public uint Value => _value ^ 0xFFFFFFFF;
    public void Update(IEnumerable<byte> bytes)
    {
        foreach (byte b in bytes)
        {
            _value ^= b;
            for (int bit = 0; bit < 8; bit++) _value = (_value & 1) != 0 ? 0xEDB88320 ^ (_value >> 1) : _value >> 1;
        }
    }
}
