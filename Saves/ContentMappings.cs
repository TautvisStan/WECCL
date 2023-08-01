using Newtonsoft.Json;

namespace WECCL.Saves;

internal class ContentMappings
{
    private static ContentMappings _instance;

    internal ContentMappings()
    {
        for (int i = 0; i < 40; i++)
        {
            this.MaterialNameMap.Add(new ContentList());
            this.FleshNameMap.Add(new ContentList());
            this.ShapeNameMap.Add(new ContentList());
        }
    }

    internal static ContentMappings ContentMap => _instance ??= new ContentMappings();

    public List<ContentList> MaterialNameMap { get; set; } = new();
    public List<ContentList> FleshNameMap { get; set; } = new();
    public List<ContentList> ShapeNameMap { get; set; } = new();

    public ContentList FaceFemaleNameMap { get; set; } = new();

    public ContentList BodyFemaleNameMap { get; set; } = new();

    public ContentList SpecialFootwearNameMap { get; set; } = new();

    public ContentList TransparentHairMaterialNameMap { get; set; } = new();

    public ContentList TransparentHairHairstyleNameMap { get; set; } = new();

    public ContentList KneepadNameMap { get; set; } = new();

    public ContentList MusicNameMap { get; set; } = new();

    public List<string> PreviouslyImportedCharacters { get; set; } = new();

    public List<int> PreviouslyImportedCharacterIds { get; set; } = new();

    public float GameVersion { get; set; } = Plugin.PluginVersion;

    public void Save()
    {
        string path = Locations.ContentMappings.FullName;
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(path, json);
        Plugin.Log.LogDebug($"Saved custom content map to {path}.");
    }

    public static ContentMappings Load()
    {
        string path = Locations.ContentMappings.FullName;
        if (!File.Exists(path))
        {
            return new ContentMappings();
        }

        try
        {
            string json = File.ReadAllText(path);
            ContentMappings obj = JsonConvert.DeserializeObject<ContentMappings>(json,
                new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });

            for (int i = 0; i < obj.PreviouslyImportedCharacters.Count; i++)
            {
                if (obj.PreviouslyImportedCharacters[i].EndsWith(".json"))
                {
                    obj.PreviouslyImportedCharacters[i] = obj.PreviouslyImportedCharacters[i]
                        .Substring(0, obj.PreviouslyImportedCharacters[i].Length - 5);
                }
                else if (obj.PreviouslyImportedCharacters[i].EndsWith(".character"))
                {
                    obj.PreviouslyImportedCharacters[i] = obj.PreviouslyImportedCharacters[i]
                        .Substring(0, obj.PreviouslyImportedCharacters[i].Length - 10);
                }
            }

            return obj;
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"Unable to load custom content map: {e}");
            return new ContentMappings();
        }
    }

    public void AddPreviouslyImportedCharacter(string name, int id)
    {
        if (name.EndsWith(".json"))
        {
            name = name.Substring(0, name.Length - 5);
        }
        else if (name.EndsWith(".character"))
        {
            name = name.Substring(0, name.Length - 10);
        }

        if (this.PreviouslyImportedCharacters.Contains(name))
        {
            return;
        }

        this.PreviouslyImportedCharacters.Add(name);
        if (id > 0 && !this.PreviouslyImportedCharacterIds.Contains(id))
        {
            this.PreviouslyImportedCharacterIds.Add(id);
        }
    }
}