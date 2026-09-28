#:package SkiaSharp@3.119.0
#:package SkiaSharp.NativeAssets.Linux.NoDependencies@3.119.0
#:property ManagePackageVersionsCentrally=false
#:property PublishAot=false

// Makes the smaller copies of the shop's pictures that the UI asks for by size (ADR 016): every
// Blobs/Name.webp gets Blobs/w400/Name.webp, w800 and w1200 for srcset, and a 32 px w32 copy the
// UI blurs as a placeholder while the picture loads. The copies are committed with the pictures,
// and docker compose (blobs-seed) and Scripts/Upload-Blobs.ps1 upload them along.
//
//   dotnet run Scripts/make-image-sizes.cs            makes what is missing or older than its picture
//   dotnet run Scripts/make-image-sizes.cs -- --force makes every copy again
//
// The widths must match the UI's (UI/store-app.UI/src/lib/images.ts).

using SkiaSharp;

int[] widths = [400, 800, 1200];
const int PlaceholderWidth = 32;
const int Quality = 75;
const int PlaceholderQuality = 40;

var force = args.Contains("--force");
var folder = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? "Blobs");
var pictures = Directory.EnumerateFiles(folder, "*.webp").Order(StringComparer.Ordinal).ToList();

int made = 0, skipped = 0;
var bytes = new Dictionary<int, long>();

foreach (var picture in pictures)
{
    var name = Path.GetFileName(picture);
    var sizes = widths.Select(width => (width, quality: Quality)).Append((width: PlaceholderWidth, quality: PlaceholderQuality)).ToList();
    var targets = sizes.Select(size => (size.width, size.quality, path: Path.Combine(folder, $"w{size.width}", name))).ToList();
    if (!force && targets.All(t => File.Exists(t.path) && File.GetLastWriteTimeUtc(t.path) >= File.GetLastWriteTimeUtc(picture)))
    {
        skipped++;
        continue;
    }

    using var original = SKBitmap.Decode(picture)
        ?? throw new InvalidOperationException($"{picture} is not an image SkiaSharp can read");

    foreach (var (width, quality, path) in targets)
    {
        // Never larger than the picture itself: a narrow one is copied at its own size
        var targetWidth = Math.Min(width, original.Width);
        var height = (int)Math.Round(original.Height * (double)targetWidth / original.Width);
        using var resized = original.Resize(new SKImageInfo(targetWidth, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Webp, quality);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using (var output = File.Create(path))
        {
            data.SaveTo(output);
        }
        bytes[width] = bytes.GetValueOrDefault(width) + new FileInfo(path).Length;
    }
    made++;
}

Console.WriteLine(made == 0
    ? $"Nothing to make ({skipped} up to date)."
    : $"Made the sizes of {made} pictures ({string.Join(", ", bytes.OrderBy(b => b.Key).Select(b => $"w{b.Key}: {b.Value / 1024} kB"))}), {skipped} up to date.");
