﻿using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using WECCL.Content;
using WECCL.Saves;

namespace WECCL.Patches;

[HarmonyPatch]
internal class SaveFilePatch
{
    /*
     * GameSaveFile.NOKFJAECIGL is called when the game restores the default data
     * This patch resets the character and federation counts.
     * It also resets the star (wrestler) and booker to 1 if they are greater than the new character count.
     */
    [HarmonyPatch(typeof(GameSaveFile), nameof(GameSaveFile.NOKFJAECIGL))]
    [HarmonyPostfix]
    public static void SaveFile_NOKFJAECIGL()
    {
        try
        {
            Characters.no_chars = 350;
            Characters.fedLimit = Plugin.Instance.BaseFedLimit.Value;
            
            if (Characters.star > 350)
            {
                Characters.star = 1;
            }

            if (Characters.booker > 350)
            {
                Characters.booker = 1;
            }

            Array.Resize(ref Characters.c, Characters.no_chars + 1);
            Array.Resize(ref Progress.charUnlock, Characters.no_chars + 1);
            Array.Resize(ref GameSaveFile.GPFFEHKLNLD.charUnlock, Characters.no_chars + 1);
            Array.Resize(ref GameSaveFile.GPFFEHKLNLD.savedChars, Characters.no_chars + 1);
            
            CustomContentSaveFile.ContentMap.PreviouslyImportedCharacters.Clear();
            CustomContentSaveFile.ContentMap.PreviouslyImportedCharacterIds.Clear();
        }
        catch (Exception e)
        {
            Plugin.Log.LogError(e);
        }
    }

    /*
     * GameSaveFile.OLAGCFPPEPB is called when the game loads the save file.
     * This prefix patch is used to update character counts and arrays to accommodate the custom content.
     */
    [HarmonyPatch(typeof(GameSaveFile), nameof(GameSaveFile.OLAGCFPPEPB))]
    [HarmonyPrefix]
    public static void GameSaveFile_OLAGCFPPEPB_PRE(int IHLLJIMFJEN)
    {
        try
        {
            string save = Application.persistentDataPath + "/Save.bytes";
            if (!File.Exists(save))
            {
                return;
            }

            FileStream fileStream = new(save, FileMode.Open);
            SaveData data = new BinaryFormatter().Deserialize(fileStream) as SaveData;
            Characters.no_chars = data!.savedChars.Length - 1;
            int[] fedCharCount = new int[Characters.no_feds + 1];
            foreach (Character c in data.savedChars)
            {
                if (c != null)
                {
                    fedCharCount[c.fed]++;
                }
            }

            Characters.fedLimit = Math.Max(Plugin.Instance.BaseFedLimit.Value, fedCharCount.Max() + 1);
            Array.Resize(ref Characters.c, Characters.no_chars + 1);
            Array.Resize(ref Progress.charUnlock, Characters.no_chars + 1);
            Array.Resize(ref GameSaveFile.GPFFEHKLNLD.charUnlock, Characters.no_chars + 1);
            if (GameSaveFile.GPFFEHKLNLD.savedFeds != null)
            {
                for (int i = 1; i <= Characters.no_feds; i++)
                {
                    if (GameSaveFile.GPFFEHKLNLD.savedFeds[i] != null)
                    {
                        GameSaveFile.GPFFEHKLNLD.savedFeds[i].size = fedCharCount[i] + 1;
                    }
                }
            }

            fileStream.Close();
        }
        catch (Exception e)
        {
            Plugin.Log.LogError(e);
        }
    }

    /*
     * This postfix patch is used to remap any custom content that has moved, and also add the imported characters.
     */
    [HarmonyPatch(typeof(GameSaveFile), nameof(GameSaveFile.OLAGCFPPEPB))]
    [HarmonyPostfix]
    public static void GameSaveFile_OLAGCFPPEPB_POST(int IHLLJIMFJEN)
    {
        string save = Application.persistentDataPath + "/Save.bytes";
        if (!File.Exists(save))
        {
            return;
        }

        try
        {
            PatchCustomContent(ref GameSaveFile.GPFFEHKLNLD);
            foreach (Tuple<string,string, Character> triplet in ImportedCharacters)
            {
                Plugin.Log.LogInfo($"Importing character {triplet.Item3.name} with id {triplet.Item3.id}.");
                string nameWithGuid = triplet.Item1;
                string overrideMode = triplet.Item2;
                Character importedCharacter = triplet.Item3;
                
                bool previouslyImported = CheckIfPreviouslyImported(nameWithGuid);
                
                switch (overrideMode.ToLower())
                {
                    case "override":
                        int id = importedCharacter.id;
                        Character oldCharacter = GameSaveFile.GPFFEHKLNLD.savedChars[id];
                        string name = importedCharacter.name;
                        string oldCharacterName = oldCharacter.name;
                        GameSaveFile.GPFFEHKLNLD.savedChars[id] = importedCharacter;
                        if (importedCharacter.fed != oldCharacter.fed)
                        {
                            if (GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].size + 1 ==
                                GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].roster.Length)
                            {
                                Array.Resize(ref GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].roster,
                                    GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].size + 2);
                                if (GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].roster.Length >
                                    Characters.fedLimit)
                                {
                                    Characters.fedLimit++;
                                }
                            }

                            GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].size++;
                            GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed]
                                .roster[GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].size] = id;
                            GameSaveFile.GPFFEHKLNLD.savedFeds[oldCharacter.fed].HKNHEJHIJLL(id);
                        }

                        Plugin.Log.LogInfo(
                            $"Imported character with id {id} and name {name}, overwriting character with name {oldCharacterName}.");
                        break;
                    case "append":
                        if (!previouslyImported)
                        {
                            int id2 = Characters.no_chars + 1;
                            importedCharacter.id = id2;
                            if (GameSaveFile.GPFFEHKLNLD.savedChars.Length <= id2)
                            {
                                Array.Resize(ref GameSaveFile.GPFFEHKLNLD.savedChars, id2 + 1);
                                Array.Resize(ref GameSaveFile.GPFFEHKLNLD.charUnlock, id2 + 1);
                                Array.Resize(ref Characters.c, id2 + 1);
                                Array.Resize(ref Progress.charUnlock, id2 + 1);
                                GameSaveFile.GPFFEHKLNLD.charUnlock[id2] = 1;
                                Progress.charUnlock[id2] = 1;
                            }
                            else
                            {
                                Plugin.Log.LogWarning(
                                    $"The array of characters is larger than the number of characters. This should not happen. The character {GameSaveFile.GPFFEHKLNLD.savedChars[id2].name} will be overwritten.");
                            }

                            GameSaveFile.GPFFEHKLNLD.savedChars[id2] = importedCharacter;
                            if (importedCharacter.fed != 0)
                            {
                                if (GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].size + 1 ==
                                    GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].roster.Length)
                                {
                                    Array.Resize(ref GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].roster,
                                        GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].size + 2);
                                    if (GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].roster.Length >
                                        Characters.fedLimit)
                                    {
                                        Characters.fedLimit++;
                                    }
                                }

                                GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].size++;
                                GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed]
                                    .roster[GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].size] = id2;
                            }

                            Characters.no_chars++;
                            Plugin.Log.LogInfo(
                                $"Imported character with id {id2} and name {importedCharacter.name}. Incremented number of characters to {Characters.no_chars}.");
                            break;
                        }
                        importedCharacter.id = GetPreviouslyImportedId(nameWithGuid);
                        goto case "merge";
                    case "merge":
                        int id3 = importedCharacter.id;
                        Character oldCharacter2 = GameSaveFile.GPFFEHKLNLD.savedChars[id3];
                        string name2 = importedCharacter.name;
                        string oldCharacterName2 = oldCharacter2.name;
                        foreach (FieldInfo field in typeof(Character).GetFields())
                        {
                            if (field.GetValue(importedCharacter) != default)
                            {
                                field.SetValue(oldCharacter2, field.GetValue(importedCharacter));
                            }
                        }

                        GameSaveFile.GPFFEHKLNLD.savedChars[id3] = oldCharacter2;
                        if (importedCharacter.fed != oldCharacter2.fed)
                        {
                            if (GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].size + 1 ==
                                GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].roster.Length)
                            {
                                Array.Resize(ref GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].roster,
                                    GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].size + 2);
                                if (GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].roster.Length >
                                    Characters.fedLimit)
                                {
                                    Characters.fedLimit++;
                                }
                            }

                            GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].size++;
                            GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed]
                                .roster[GameSaveFile.GPFFEHKLNLD.savedFeds[importedCharacter.fed].size] = id3;
                            GameSaveFile.GPFFEHKLNLD.savedFeds[oldCharacter2.fed].HKNHEJHIJLL(id3);
                        }

                        Plugin.Log.LogInfo(
                            $"Imported character with id {id3} and name {name2}, merging with character with name {oldCharacterName2}.");
                        break;
                }
                CustomContentSaveFile.ContentMap.AddPreviouslyImportedCharacter(nameWithGuid, importedCharacter.id);
            }

            GameSaveFile.GPFFEHKLNLD.FGMMAKKGCOG(IHLLJIMFJEN);
        }
        catch (Exception e)
        {
            Plugin.Log.LogError(e);
        }
    }
    
    [HarmonyPatch(typeof(Roster), nameof(Roster.ENFAGKBOOAN))]
    [HarmonyPostfix]
    public static void Roster_ENFAGKBOOAN(Roster __instance)
    {
        if (Plugin.Instance.BaseFedLimit.Value >= 48 && __instance.roster.Length == 49)
        {
            Array.Resize(ref __instance.roster, Plugin.Instance.BaseFedLimit.Value + 1);
        }
    }
    
    private static bool CheckIfPreviouslyImported(string nameWithGuid)
    {
        return CustomContentSaveFile.ContentMap.PreviouslyImportedCharacters.Contains(nameWithGuid);
    }
    
    
    private static int GetPreviouslyImportedId(string nameWithGuid)
    {
        return CustomContentSaveFile.ContentMap.PreviouslyImportedCharacterIds[CustomContentSaveFile.ContentMap.PreviouslyImportedCharacters.IndexOf(nameWithGuid)];
    }


    /*
     * GameSaveFile.ICAMLDGDPHC is called when the player saves the game.
     * This patch saves the current custom content map and exports all characters.
     */
    [HarmonyPatch(typeof(GameSaveFile), nameof(GameSaveFile.ICAMLDGDPHC))]
    [HarmonyPostfix]
    public static void GameSaveFile_ICAMLDGDPHC(int IHLLJIMFJEN)
    {
        SaveCurrentMap();
        if (Plugin.Instance.AutoExportCharacters.Value)
        {
            ModdedCharacterManager.SaveAllCharacters();
        }

        if (Plugin.Instance.DeleteImportedCharacters.Value)
        {
            foreach (string file in FilesToDeleteOnSave)
            {
                File.Delete(file);
            }
        }
    }


    /*
    Special cases:
    BodyFemale is negative Flesh[2]
    FaceFemale is negative Material[3]
    SpecialFootwear is negative Material[14] and [15]
    TransparentHairMaterial is negative Material[17]
    TransparentHairHairstyle is negative Shape[17]
    Kneepad is negative Material[24] and [25]
     */
    internal static void PatchCustomContent(ref SaveData saveData)
    {
        CustomContentSaveFile newMap = CustomContentSaveFile.ContentMap;
        CustomContentSaveFile savedMap = LoadPreviousMap();

        bool changed = false;

        if (!VanillaCounts.IsInitialized)
        {
            Plugin.Log.LogError("Vanilla counts not initialized. Skipping custom content patch.");
            return;
        }

        try
        {
            foreach (Character character in saveData.savedChars)
            {
                if (character == null)
                {
                    continue;
                }

                foreach (Costume costume in character.costume)
                {
                    if (costume == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < costume.texture.Length; i++)
                    {
                        if (VanillaCounts.MaterialCounts[i] == 0)
                        {
                            continue;
                        }

                        if (costume.texture[i] > VanillaCounts.MaterialCounts[i])
                        {
                            int oldIndex = costume.texture[i] - VanillaCounts.MaterialCounts[i] - 1;
                            if (oldIndex >= savedMap.MaterialNameMap[i].Count)
                            {
                                Plugin.Log.LogWarning(
                                    $"Custom material {i} index {oldIndex} is out of bounds for character {character.name} ({character.id}). Resetting.");
                                costume.texture[i] = VanillaCounts.MaterialCounts[i];
                                changed = true;
                                continue;
                            }

                            string oldName = savedMap.MaterialNameMap[i][oldIndex];
                            int newIndex = newMap.MaterialNameMap[i].IndexOf(oldName);
                            int internalIndex = newIndex + VanillaCounts.MaterialCounts[i] + 1;
                            if (costume.texture[i] != internalIndex)
                            {
                                Plugin.Log.LogInfo(
                                    $"Custom material {oldName} at index {oldIndex} was remapped to index {newIndex} (internal index {internalIndex}) for material {i} of character {character.name} ({character.id}).");
                                costume.texture[i] = internalIndex;
                                changed = true;
                            }
                        }

                        else if (i == 3 && costume.texture[i] < -VanillaCounts.FaceFemaleCount)
                        {
                            int oldIndex = -costume.texture[i] - VanillaCounts.FaceFemaleCount - 1;
                            if (oldIndex >= savedMap.FaceFemaleNameMap.Count)
                            {
                                Plugin.Log.LogWarning(
                                    $"Custom material {i} index {oldIndex} is out of bounds for character {character.name} ({character.id}). Resetting.");
                                costume.texture[i] = VanillaCounts.MaterialCounts[i];
                                changed = true;
                                continue;
                            }

                            string oldName = savedMap.FaceFemaleNameMap[oldIndex];
                            int newIndex = newMap.FaceFemaleNameMap.IndexOf(oldName);
                            int internalIndex = -newIndex - VanillaCounts.FaceFemaleCount - 1;
                            if (costume.texture[i] != internalIndex)
                            {
                                Plugin.Log.LogInfo(
                                    $"Custom material {oldName} at index {oldIndex} was remapped to index {newIndex} (internal index {internalIndex}) for material {i} of character {character.name} ({character.id}).");
                                costume.texture[i] = internalIndex;
                                changed = true;
                            }
                        }

                        else if (i == 14 && costume.texture[i] < -VanillaCounts.SpecialFootwearCount)
                        {
                            int oldIndex = -costume.texture[i] - VanillaCounts.SpecialFootwearCount - 1;
                            if (oldIndex >= savedMap.SpecialFootwearNameMap.Count)
                            {
                                Plugin.Log.LogWarning(
                                    $"Custom material {i} index {oldIndex} is out of bounds for character {character.name} ({character.id}). Resetting.");
                                costume.texture[i] = VanillaCounts.MaterialCounts[i];
                                changed = true;
                                continue;
                            }

                            string oldName = savedMap.SpecialFootwearNameMap[oldIndex];
                            int newIndex = newMap.SpecialFootwearNameMap.IndexOf(oldName);
                            int internalIndex = -newIndex - VanillaCounts.SpecialFootwearCount - 1;
                            if (costume.texture[i] != internalIndex)
                            {
                                Plugin.Log.LogInfo(
                                    $"Custom material {oldName} at index {oldIndex} was remapped to index {newIndex} (internal index {internalIndex}) for material {i} of character {character.name} ({character.id}).");
                                costume.texture[i] = internalIndex;
                                changed = true;
                            }
                        }

                        else if (i == 15 && costume.texture[i] < -VanillaCounts.SpecialFootwearCount)
                        {
                            int oldIndex = -costume.texture[i] - VanillaCounts.SpecialFootwearCount - 1;
                            if (oldIndex >= savedMap.SpecialFootwearNameMap.Count)
                            {
                                Plugin.Log.LogWarning(
                                    $"Custom material {i} index {oldIndex} is out of bounds for character {character.name} ({character.id}). Resetting.");
                                costume.texture[i] = VanillaCounts.MaterialCounts[i];
                                changed = true;
                                continue;
                            }

                            string oldName = savedMap.SpecialFootwearNameMap[oldIndex];
                            int newIndex = newMap.SpecialFootwearNameMap.IndexOf(oldName);
                            int internalIndex = -newIndex - VanillaCounts.SpecialFootwearCount - 1;
                            if (costume.texture[i] != internalIndex)
                            {
                                Plugin.Log.LogInfo(
                                    $"Custom material {oldName} at index {oldIndex} was remapped to index {newIndex} (internal index {internalIndex}) for material {i} of character {character.name} ({character.id}).");
                                costume.texture[i] = internalIndex;
                                changed = true;
                            }
                        }

                        else if (i == 17 && costume.texture[i] < -VanillaCounts.TransparentHairMaterialCount)
                        {
                            int oldIndex = -costume.texture[i] - VanillaCounts.TransparentHairMaterialCount - 1;
                            if (oldIndex >= savedMap.TransparentHairMaterialNameMap.Count)
                            {
                                Plugin.Log.LogWarning(
                                    $"Custom material {i} index {oldIndex} is out of bounds for character {character.name} ({character.id}). Resetting.");
                                costume.texture[i] = VanillaCounts.MaterialCounts[i];
                                changed = true;
                                continue;
                            }

                            string oldName = savedMap.TransparentHairMaterialNameMap[oldIndex];
                            int newIndex = newMap.TransparentHairMaterialNameMap.IndexOf(oldName);
                            int internalIndex = -newIndex - VanillaCounts.TransparentHairMaterialCount - 1;
                            if (costume.texture[i] != internalIndex)
                            {
                                Plugin.Log.LogInfo(
                                    $"Custom material {oldName} at index {oldIndex} was remapped to index {newIndex} (internal index {internalIndex}) for material {i} of character {character.name} ({character.id}).");
                                costume.texture[i] = internalIndex;
                                changed = true;
                            }
                        }

                        else if (i == 24 && costume.texture[i] < -VanillaCounts.KneepadCount)
                        {
                            int oldIndex = -costume.texture[i] - VanillaCounts.KneepadCount - 1;
                            if (oldIndex >= savedMap.KneepadNameMap.Count)
                            {
                                Plugin.Log.LogWarning(
                                    $"Custom material {i} index {oldIndex} is out of bounds for character {character.name} ({character.id}). Resetting.");
                                costume.texture[i] = VanillaCounts.MaterialCounts[i];
                                changed = true;
                                continue;
                            }

                            string oldName = savedMap.KneepadNameMap[oldIndex];
                            int newIndex = newMap.KneepadNameMap.IndexOf(oldName);
                            int internalIndex = -newIndex - VanillaCounts.KneepadCount - 1;
                            if (costume.texture[i] != internalIndex)
                            {
                                Plugin.Log.LogInfo(
                                    $"Custom material {oldName} at index {oldIndex} was remapped to index {newIndex} (internal index {internalIndex}) for material {i} of character {character.name} ({character.id}).");
                                costume.texture[i] = internalIndex;
                                changed = true;
                            }
                        }

                        else if (i == 25 && costume.texture[i] < -VanillaCounts.KneepadCount)
                        {
                            int oldIndex = -costume.texture[i] - VanillaCounts.KneepadCount - 1;
                            if (oldIndex >= savedMap.KneepadNameMap.Count)
                            {
                                Plugin.Log.LogWarning(
                                    $"Custom material {i} index {oldIndex} is out of bounds for character {character.name} ({character.id}). Resetting.");
                                costume.texture[i] = VanillaCounts.MaterialCounts[i];
                                changed = true;
                                continue;
                            }

                            string oldName = savedMap.KneepadNameMap[oldIndex];
                            int newIndex = newMap.KneepadNameMap.IndexOf(oldName);
                            int internalIndex = -newIndex - VanillaCounts.KneepadCount - 1;
                            if (costume.texture[i] != internalIndex)
                            {
                                Plugin.Log.LogInfo(
                                    $"Custom material {oldName} at index {oldIndex} was remapped to index {newIndex} (internal index {internalIndex}) for material {i} of character {character.name} ({character.id}).");
                                costume.texture[i] = internalIndex;
                                changed = true;
                            }
                        }
                    }

                    for (int i = 0; i < costume.flesh.Length; i++)
                    {
                        if (VanillaCounts.FleshCounts[i] == 0)
                        {
                            continue;
                        }

                        if (costume.flesh[i] > VanillaCounts.FleshCounts[i])
                        {
                            int oldIndex = costume.flesh[i] - VanillaCounts.FleshCounts[i] - 1;
                            if (oldIndex >= savedMap.FleshNameMap.Count)
                            {
                                Plugin.Log.LogWarning(
                                    $"Custom flesh index {oldIndex} is out of bounds for character {character.name} ({character.id}). Resetting.");
                                costume.flesh[i] = VanillaCounts.FleshCounts[i];
                                changed = true;
                                continue;
                            }

                            string oldName = savedMap.FleshNameMap[i][oldIndex];
                            int newIndex = newMap.FleshNameMap[i].IndexOf(oldName);
                            int internalIndex = newIndex + VanillaCounts.FleshCounts[i] + 1;
                            if (costume.flesh[i] != internalIndex)
                            {
                                Plugin.Log.LogInfo(
                                    $"Custom flesh {oldName} at index {oldIndex} was remapped to index {newIndex} (internal index {internalIndex}) for flesh {i} of character {character.name} ({character.id}).");
                                costume.flesh[i] = internalIndex;
                                changed = true;
                            }
                        }

                        else if (i == 2 && costume.flesh[i] < -VanillaCounts.BodyFemaleCount)
                        {
                            int oldIndex = -costume.flesh[i] - VanillaCounts.BodyFemaleCount - 1;
                            if (oldIndex >= savedMap.BodyFemaleNameMap.Count)
                            {
                                Plugin.Log.LogWarning(
                                    $"Custom flesh index {oldIndex} is out of bounds for character {character.name} ({character.id}). Resetting.");
                                costume.flesh[i] = VanillaCounts.FleshCounts[i];
                                changed = true;
                                continue;
                            }

                            string oldName = savedMap.BodyFemaleNameMap[oldIndex];
                            int newIndex = newMap.BodyFemaleNameMap.IndexOf(oldName);
                            int internalIndex = -newIndex - VanillaCounts.BodyFemaleCount - 1;
                            if (costume.flesh[i] != internalIndex)
                            {
                                Plugin.Log.LogInfo(
                                    $"Custom flesh {oldName} at index {oldIndex} was remapped to index {newIndex} (internal index {internalIndex}) for flesh {i} of character {character.name} ({character.id}).");
                                costume.flesh[i] = internalIndex;
                                changed = true;
                            }
                        }
                    }

                    for (int i = 0; i < costume.shape.Length; i++)
                    {
                        if (VanillaCounts.ShapeCounts[i] == 0)
                        {
                            continue;
                        }

                        if (costume.shape[i] > VanillaCounts.ShapeCounts[i])
                        {
                            int oldIndex = costume.shape[i] - VanillaCounts.ShapeCounts[i] - 1;
                            if (oldIndex >= savedMap.ShapeNameMap[i].Count)
                            {
                                Plugin.Log.LogWarning(
                                    $"Custom shape index {oldIndex} is out of bounds for character {character.name} ({character.id}). Resetting.");
                                costume.shape[i] = VanillaCounts.ShapeCounts[i];
                                changed = true;
                                continue;
                            }

                            string oldName = savedMap.ShapeNameMap[i][oldIndex];
                            int newIndex = newMap.ShapeNameMap[i].IndexOf(oldName);
                            int internalIndex = newIndex + VanillaCounts.ShapeCounts[i] + 1;
                            if (costume.shape[i] != internalIndex)
                            {
                                Plugin.Log.LogInfo(
                                    $"Custom shape {oldName} at index {oldIndex} was remapped to index {newIndex} (internal index {internalIndex}) for shape {i} of character {character.name} ({character.id}).");
                                costume.shape[i] = internalIndex;
                                changed = true;
                            }
                        }

                        else if (i == 17 && costume.shape[i] < -VanillaCounts.TransparentHairHairstyleCount)
                        {
                            int oldIndex = -costume.shape[i] - VanillaCounts.TransparentHairHairstyleCount - 1;
                            if (oldIndex >= savedMap.TransparentHairHairstyleNameMap.Count)
                            {
                                Plugin.Log.LogWarning(
                                    $"Custom shape index {oldIndex} is out of bounds for character {character.name} ({character.id}). Resetting.");
                                costume.shape[i] = VanillaCounts.ShapeCounts[i];
                                changed = true;
                                continue;
                            }

                            string oldName = savedMap.TransparentHairHairstyleNameMap[oldIndex];
                            int newIndex = newMap.TransparentHairHairstyleNameMap.IndexOf(oldName);
                            int internalIndex = -newIndex - VanillaCounts.TransparentHairHairstyleCount - 1;
                            if (costume.shape[i] != internalIndex)
                            {
                                Plugin.Log.LogInfo(
                                    $"Custom shape {oldName} at index {oldIndex} was remapped to index {newIndex} (internal index {internalIndex}) for shape {i} of character {character.name} ({character.id}).");
                                costume.shape[i] = internalIndex;
                                changed = true;
                            }
                        }
                    }
                }

                if (character.music > VanillaCounts.MusicCount)
                {
                    int oldIndex = character.music - VanillaCounts.MusicCount - 1;
                    if (oldIndex >= savedMap.MusicNameMap.Count)
                    {
                        Plugin.Log.LogWarning(
                            $"Custom music index {oldIndex} is out of bounds for character {character.name} ({character.id}). Resetting.");
                        character.music = VanillaCounts.MusicCount;
                        changed = true;
                    }
                    else
                    {
                        string oldName = savedMap.MusicNameMap[oldIndex];
                        int newIndex = newMap.MusicNameMap.IndexOf(oldName);
                        int internalIndex = newIndex + VanillaCounts.MusicCount + 1;
                        if (character.music != internalIndex)
                        {
                            Plugin.Log.LogInfo(
                                $"Custom music {oldName} at index {oldIndex} was remapped to index {newIndex} (internal index {internalIndex}) for character {character.name} ({character.id}).");
                            character.music = internalIndex;
                            changed = true;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogError("Failed to remap custom content!");
            Plugin.Log.LogError(e);
        }
    }

    internal static void SaveCurrentMap()
    {
        CustomContentSaveFile.ContentMap.Save();
    }

    internal static CustomContentSaveFile LoadPreviousMap()
    {
        return CustomContentSaveFile.Load();
    }
}