#:package SkiaSharp@3.119.0
#:package SkiaSharp.NativeAssets.Linux.NoDependencies@3.119.0
#:property ManagePackageVersionsCentrally=false

// Turns the generated pictures in Blobs/ (JPEG or PNG, 2-3 MB each) into the WebP files the
// shop serves and the repository keeps: product shots ("Name-1.jpg") at most 1600 px wide,
// wide editorial pictures (covers, lookbooks) at most 2000 px. Only the WebP files are
// committed; the originals stay on the machine that generated them (see .gitignore).
//
//   dotnet run Scripts/optimize-images.cs            converts what is new or changed
//   dotnet run Scripts/optimize-images.cs -- --force converts everything again
//
// Subfolders ("old pictures", the smaller copies in w400/ and the like) are left alone; the copies
// come from Scripts/make-image-sizes.cs, run after this one.

using System.Text.RegularExpressions;
using SkiaSharp;

const int ProductMaxWidth = 1600;
const int EditorialMaxWidth = 2000;
const int Quality = 80;

var force = args.Contains("--force");
var folder = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? "Blobs");
var productShot = new Regex(@"-\d+$", RegexOptions.CultureInvariant);

var sources = Directory.EnumerateFiles(folder)
    .Where(path => Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png")
    .Order(StringComparer.Ordinal)
    .ToList();

int converted = 0, skipped = 0;
long before = 0, after = 0;

foreach (var source in sources)
{
    var name = Path.GetFileNameWithoutExtension(source);
    var target = Path.Combine(folder, name + ".webp");
    if (!force && File.Exists(target) && File.GetLastWriteTimeUtc(target) >= File.GetLastWriteTimeUtc(source))
    {
        skipped++;
        continue;
    }

    using var original = SKBitmap.Decode(source)
        ?? throw new InvalidOperationException($"{source} is not an image SkiaSharp can read");

    var maxWidth = productShot.IsMatch(name) ? ProductMaxWidth : EditorialMaxWidth;
    var bitmap = original;
    if (original.Width > maxWidth)
    {
        var height = (int)Math.Round(original.Height * (double)maxWidth / original.Width);
        bitmap = original.Resize(new SKImageInfo(maxWidth, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
    }

    var size = $"{bitmap.Width}x{bitmap.Height}";
    using (var image = SKImage.FromBitmap(bitmap))
    using (var data = image.Encode(SKEncodedImageFormat.Webp, Quality))
    using (var output = File.Create(target))
    {
        data.SaveTo(output);
    }

    if (!ReferenceEquals(bitmap, original))
    {
        bitmap.Dispose();
    }

    before += new FileInfo(source).Length;
    after += new FileInfo(target).Length;
    converted++;
    Console.WriteLine($"{name}.webp  {size}  {new FileInfo(target).Length / 1024} kB");
}

Console.WriteLine(converted == 0
    ? $"Nothing to convert ({skipped} up to date)."
    : $"Converted {converted} ({before / 1048576} MB -> {after / 1048576} MB), {skipped} up to date.");
