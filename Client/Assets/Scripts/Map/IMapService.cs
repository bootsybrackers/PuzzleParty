namespace PuzzleParty.Maps
{
    public interface IMapService
    {
        MapsConfig GetMapsConfig();
        Map GetCurrentMap(int lastBeatenLevel);
        Map GetMapById(int mapId);
        Map[] GetAllMaps();
        bool IsMapUnlocked(int mapId, int lastBeatenLevel);
    }
}
