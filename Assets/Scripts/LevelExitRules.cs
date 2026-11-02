public static class LevelExitRules
{
    public static bool EnteredOpenDoor(bool ready,bool previouslyReady,bool inside,bool previouslyInside)
        => ready && previouslyReady && inside && !previouslyInside;
    public static bool ObjectiveReady(LevelDef def,int coins,float elapsed)
        => def.kind==GoalKind.Coins ? coins>=def.target : def.kind!=GoalKind.Survive || elapsed>=def.survive;
    // Coins start on floor zero: the last required coin is on target-1.
    // The exit is one platform above it so collection cannot itself complete the level.
    public static int FixedExitFloor(LevelDef def) => def.kind==GoalKind.Coins ? def.target : def.target-1;
}
