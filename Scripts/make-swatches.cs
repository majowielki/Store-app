#:package SkiaSharp@3.119.0
#:package SkiaSharp.NativeAssets.Linux.NoDependencies@3.119.0
#:property ManagePackageVersionsCentrally=false
#:property PublishAot=false

// Makes the colour swatches of the shop from its product pictures, as Scripts/swatches.json lists
// them: every texture (a wood, a weave, a stone, a metal) is a crop of a picture in Blobs/,
// written as a small square WebP to UI/store-app.UI/src/assets/swatches/, and every colour is
// the average of a crop. The averages are printed for ProductService's FinishCatalogue, which
// uses them as the colour of each swatch part. Needs the original JPEG pictures, which stay on
// the machine that generated them (see Scripts/optimize-images.cs).
//
//   dotnet run Scripts/make-swatches.cs                      writes the textures, prints the colours
//   dotnet run Scripts/make-swatches.cs -- --preview <folder> also draws a sheet to check the crops

using System.Text.Json;
using SkiaSharp;

const int Quality = 82;
var root = Directory.GetCurrentDirectory();
var spec = JsonSerializer.Deserialize<Spec>(File.ReadAllText(Path.Combine(root, "Scripts", "swatches.json")), new JsonSerializerOptions(JsonSerializerDefaults.Web))
    ?? throw new InvalidOperationException("Scripts/swatches.json is empty");
var output = Path.Combine(root, "UI", "store-app.UI", "src", "assets", "swatches");
Directory.CreateDirectory(output);
var previewIndex = Array.IndexOf(args, "--preview");
var preview = previewIndex >= 0 && previewIndex + 1 < args.Length ? args[previewIndex + 1] : null;

var textures = new List<(string Name, SKBitmap Swatch, SKColor Average)>();
foreach (var texture in spec.Textures)
{
    using var picture = Load(texture.Picture);
    var crop = Pixels(texture.Crop, picture);
    var swatch = Render(picture, crop, texture.Fit ?? "cover", spec.Size);
    using (var image = SKImage.FromBitmap(swatch))
    using (var data = image.Encode(SKEncodedImageFormat.Webp, Quality))
    using (var file = File.Create(Path.Combine(output, texture.Name + ".webp")))
    {
        data.SaveTo(file);
    }

    textures.Add((texture.Name, swatch, Average(picture, crop)));
}

var colors = spec.Colors.Select(color =>
{
    using var picture = Load(color.Picture);
    return (color.Name, Average: Average(picture, Pixels(color.Crop, picture)));
}).ToList();

Console.WriteLine("Textures (average colour):");
textures.ForEach(t => Console.WriteLine($"  {t.Name,-22} {Hex(t.Average)}"));
Console.WriteLine("Colours:");
colors.ForEach(c => Console.WriteLine($"  {c.Name,-22} {Hex(c.Average)}"));

if (preview is not null)
{
    DrawPreview(preview, textures, colors, spec.Size);
}

SKBitmap Load(string picture)
    => SKBitmap.Decode(Path.Combine(root, "Blobs", picture + ".jpg"))
       ?? throw new InvalidOperationException($"Blobs/{picture}.jpg is missing or not a picture");

static SKRectI Pixels(double[] crop, SKBitmap picture)
    => new((int)(crop[0] * picture.Width), (int)(crop[1] * picture.Height), (int)(crop[2] * picture.Width), (int)(crop[3] * picture.Height));

// The square swatch: the middle square of the crop, the whole crop squeezed, or a strip repeated
static SKBitmap Render(SKBitmap picture, SKRectI crop, string fit, int size)
{
    var swatch = new SKBitmap(size, size);
    using var canvas = new SKCanvas(swatch);
    using var paint = new SKPaint { IsAntialias = true };
    var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);
    using var image = SKImage.FromBitmap(picture);
    switch (fit)
    {
        case "cover":
            var side = Math.Min(crop.Width, crop.Height);
            var square = SKRect.Create(crop.MidX - side / 2f, crop.MidY - side / 2f, side, side);
            canvas.DrawImage(image, square, new SKRect(0, 0, size, size), sampling, paint);
            break;
        case "stretch":
            canvas.DrawImage(image, crop, new SKRect(0, 0, size, size), sampling, paint);
            break;
        case "tile":
            var width = crop.Width * (float)size / crop.Height;
            for (var x = 0f; x < size; x += width)
            {
                canvas.DrawImage(image, crop, SKRect.Create(x, 0, width, size), sampling, paint);
            }
            break;
        default:
            throw new InvalidOperationException($"Unknown fit '{fit}'");
    }

    return swatch;
}

static SKColor Average(SKBitmap picture, SKRectI crop)
{
    long r = 0, g = 0, b = 0, n = 0;
    for (var y = crop.Top; y < crop.Bottom; y += 2)
    for (var x = crop.Left; x < crop.Right; x += 2)
    {
        var c = picture.GetPixel(x, y);
        r += c.Red; g += c.Green; b += c.Blue; n++;
    }

    return new SKColor((byte)(r / n), (byte)(g / n), (byte)(b / n));
}

static string Hex(SKColor c) => $"#{c.Red:x2}{c.Green:x2}{c.Blue:x2}";

// Every swatch as a circle, the way the shop shows it, with its name and colour, to check the crops by eye
static void DrawPreview(string folder, List<(string Name, SKBitmap Swatch, SKColor Average)> textures, List<(string Name, SKColor Average)> colors, int size)
{
    Directory.CreateDirectory(folder);
    const int Columns = 7, Cell = 200;
    var items = textures.Select(t => (t.Name, (SKBitmap?)t.Swatch, t.Average)).Concat(colors.Select(c => (c.Name, (SKBitmap?)null, c.Average))).ToList();
    var rows = (items.Count + Columns - 1) / Columns;
    using var surface = SKSurface.Create(new SKImageInfo(Columns * Cell, rows * Cell));
    var canvas = surface.Canvas;
    canvas.Clear(new SKColor(0xf4, 0xef, 0xe7));
    using var font = new SKFont(SKTypeface.Default, 16);
    using var ink = new SKPaint { Color = SKColors.Black, IsAntialias = true };
    for (var i = 0; i < items.Count; i++)
    {
        var (name, swatch, average) = items[i];
        var x = i % Columns * Cell + (Cell - size) / 2f;
        var y = i / Columns * Cell + 12;
        using var clip = new SKPath();
        clip.AddCircle(x + size / 2f, y + size / 2f, size / 2f);
        canvas.Save();
        canvas.ClipPath(clip, antialias: true);
        if (swatch is not null) canvas.DrawBitmap(swatch, x, y);
        else canvas.DrawRect(x, y, size, size, new SKPaint { Color = average });
        canvas.Restore();
        canvas.DrawCircle(x + size - 14, y + size - 14, 12, new SKPaint { Color = average, IsAntialias = true });
        canvas.DrawText($"{name} {Hex(average)}", i % Columns * Cell + 6, y + size + 22, font, ink);
    }

    using var shot = surface.Snapshot();
    using var data = shot.Encode(SKEncodedImageFormat.Jpeg, 90);
    File.WriteAllBytes(Path.Combine(folder, "swatches-preview.jpg"), data.ToArray());
}

sealed record Spec(int Size, List<SwatchSource> Textures, List<SwatchSource> Colors);

/// <summary>A crop of a product picture: x0, y0, x1, y1 as fractions of its width and height.</summary>
sealed record SwatchSource(string Name, string Picture, double[] Crop, string? Fit);
