using UnityEngine;
using UnityEngine.Assertions;

public static class LevelExitVerification
{
    public static void Run()
    {
        for(int n=1;n<=LevelDefs.Count;n++)
        {
            var def=LevelDefs.Get(n);
            bool ready=LevelExitRules.ObjectiveReady(def,def.target,def.survive);
            Assert.IsTrue(ready);
            Assert.IsFalse(LevelExitRules.EnteredOpenDoor(ready,true,false,false),"Goal alone must not finish level "+n);
            Assert.IsFalse(LevelExitRules.EnteredOpenDoor(false,true,true,false),"Locked door must not finish level "+n);
            Assert.IsFalse(LevelExitRules.EnteredOpenDoor(ready,false,true,true),"Unlocking while inside must not auto-finish level "+n);
            Assert.IsFalse(LevelExitRules.EnteredOpenDoor(ready,true,true,true),"Standing inside is not an entry");
            Assert.IsTrue(LevelExitRules.EnteredOpenDoor(ready,true,true,false),"Entering open door must finish level "+n);
            if(def.kind==GoalKind.Coins)
            {
                Assert.IsFalse(LevelExitRules.ObjectiveReady(def,def.target-1,10000));
                Assert.AreEqual(def.target,LevelExitRules.FixedExitFloor(def));
            }
            else if(def.kind==GoalKind.Survive)
                Assert.IsFalse(LevelExitRules.ObjectiveReady(def,10000,def.survive-.01f));
            else Assert.AreEqual(def.target-1,LevelExitRules.FixedExitFloor(def));
            Assert.IsTrue(def.Goal.Contains("kapıdan geç"));
        }
        Debug.Log("LEVEL EXIT PASS: all 100 definitions; objectives alone cannot win; locked/standing/unlocking-inside rejected; fresh open-door entry required");
    }
}
