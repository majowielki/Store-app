namespace Store.Contracts.Catalog;

public enum Category
{
    All,
    Sofas,
    Chairs,
    Tables,
    Beds,
    Mattresses,
    Desks,
    TVStands,
    Bookcases,
    Wardrobes,
    Dressers,
    Nightstands,
    Sideboards,
    Rugs,
    OutdoorFurniture,
    EntrywayFurniture,
    Decor,
    BathroomStorage,
    BathroomFurniture,
    BathroomMirrors,
    KitchenCabinets,
    KitchenIslands,
    GardenSets,
    KidsBeds,
    KidsDesks,
    TableLamps,
    FloorLamps
}

public enum Group
{
    All,
    Furniture,
    Kitchen,
    Bathroom,
    Bedroom,
    Decorations,
    Kids,
    Garden
}

public static class CategoryHelper
{
    public static string GetDisplayName(this Category category) => category switch
    {
        Category.All => "All Categories",
        Category.Sofas => "Sofas",
        Category.Chairs => "Chairs",
        Category.Tables => "Tables",
        Category.Beds => "Beds",
        Category.Mattresses => "Mattresses",
        Category.Desks => "Desks",
        Category.TVStands => "TV Stands",
        Category.Bookcases => "Bookcases",
        Category.Wardrobes => "Wardrobes",
        Category.Dressers => "Dressers",
        Category.Nightstands => "Nightstands",
        Category.Sideboards => "Sideboards",
        Category.Rugs => "Rugs",
        Category.OutdoorFurniture => "Outdoor Furniture",
        Category.EntrywayFurniture => "Entryway Furniture",
        Category.Decor => "Decor",
        Category.BathroomStorage => "Bathroom Storage",
        Category.BathroomFurniture => "Bathroom Furniture",
        Category.BathroomMirrors => "Bathroom Mirrors",
        Category.KitchenCabinets => "Kitchen Cabinets",
        Category.KitchenIslands => "Kitchen Islands",
        Category.GardenSets => "Garden Sets",
        Category.KidsBeds => "Kids Beds",
        Category.KidsDesks => "Kids Desks",
        Category.TableLamps => "Table Lamps",
        Category.FloorLamps => "Floor Lamps",
        _ => category.ToString()
    };

    public static string GetDisplayName(this Group group) => group switch
    {
        Group.All => "All products",
        Group.Furniture => "Furniture",
        Group.Kitchen => "Kitchen",
        Group.Bathroom => "Bathroom",
        Group.Bedroom => "Bedroom",
        Group.Decorations => "Decorations",
        Group.Kids => "Kids",
        Group.Garden => "Garden",
        _ => group.ToString()
    };

    public static Group? GetGroup(this Category category) => category switch
    {
        Category.Rugs or Category.Decor => Group.Decorations,
        Category.KidsBeds or Category.KidsDesks => Group.Kids,
        Category.OutdoorFurniture or Category.GardenSets => Group.Garden,
        Category.BathroomFurniture or Category.BathroomMirrors or Category.BathroomStorage => Group.Bathroom,
        Category.KitchenCabinets or Category.KitchenIslands => Group.Kitchen,
        Category.Beds or Category.Mattresses or Category.Nightstands or Category.Wardrobes or Category.Dressers or Category.TableLamps => Group.Bedroom,
        Category.Sofas or Category.Chairs or Category.Tables or Category.TVStands or Category.Bookcases or Category.Desks or Category.EntrywayFurniture or Category.FloorLamps or Category.Sideboards
            => Group.Furniture,
        _ => Group.Furniture
    };

    public static IReadOnlyCollection<Category> GetCategories(this Group group) => group switch
    {
        Group.All => Enum.GetValues<Category>().Where(c => c != Category.All).ToArray(),
        Group.Furniture => new[] { Category.Sofas, Category.Chairs, Category.Tables, Category.TVStands, Category.Bookcases, Category.Desks, Category.EntrywayFurniture, Category.FloorLamps, Category.Sideboards },
        Group.Kitchen => new[] { Category.KitchenCabinets, Category.KitchenIslands },
        Group.Bathroom => new[] { Category.BathroomFurniture, Category.BathroomMirrors, Category.BathroomStorage },
        Group.Bedroom => new[] { Category.Beds, Category.Mattresses, Category.Nightstands, Category.Wardrobes, Category.Dressers, Category.TableLamps },
        Group.Decorations => new[] { Category.Decor, Category.Rugs },
        Group.Kids => new[] { Category.KidsBeds, Category.KidsDesks },
        Group.Garden => new[] { Category.OutdoorFurniture, Category.GardenSets },
        _ => Enum.GetValues<Category>().Where(c => c != Category.All).ToArray()
    };
}
