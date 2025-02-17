﻿using RimWorld;                    
using RimWorld.Planet;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using System.Threading.Tasks;
using RimValiCore.RVR;
namespace AvaliMod
{
    public class AvaliPackDriver : GameComponent//WorldComponent//MapComponent//
    {
        private RimValiModSettings settings = LoadedModManager.GetMod<RimValiMod>().GetSettings<RimValiModSettings>();
        private readonly bool enableDebug = LoadedModManager.GetMod<RimValiMod>().GetSettings<RimValiModSettings>().enableDebugMode;
        private readonly int maxSize = LoadedModManager.GetMod<RimValiMod>().GetSettings<RimValiModSettings>().maxPackSize;
        private readonly bool packsEnabled = LoadedModManager.GetMod<RimValiMod>().GetSettings<RimValiModSettings>().packsEnabled;
        private readonly bool checkOtherRaces = LoadedModManager.GetMod<RimValiMod>().GetSettings<RimValiModSettings>().checkOtherRaces;
        private readonly bool allowAllRaces = LoadedModManager.GetMod<RimValiMod>().GetSettings<RimValiModSettings>().allowAllRaces;
        private readonly bool multiThreaded = LoadedModManager.GetMod<RimValiMod>().GetSettings<RimValiModSettings>().packMultiThreading;
        private readonly Dictionary<string, bool> otherRaces = LoadedModManager.GetMod<RimValiMod>().GetSettings<RimValiModSettings>().enabledRaces;
        public bool ThreadIsActive;
        public List<AvaliPack> packs = new List<AvaliPack>();
        private int onTick = 0;
        public Dictionary<Pawn, bool> pawnsHaveHadPacks = new Dictionary<Pawn, bool>();
        public List<Pawn> pawns = new List<Pawn>();
        public List<bool> bools = new List<bool>();
        public List<Pawn> pawnsInWorld= new List<Pawn>();
        //public AvaliPackDriver(Map map) : base(map) { }

        //public AvaliPackDriver(World world) : base(world) { }

        public AvaliPackDriver(Game game) {
            StartedNewGame();
        }//: base(game) { }

        public List<ThingDef> racesInPacks = new List<ThingDef>();

        public override void StartedNewGame()
        {
            packs = new List<AvaliPack>();
            Dictionary<Pawn, bool> pawnsHaveHadPacks = new Dictionary<Pawn, bool>();
            List<Pawn> pawns = new List<Pawn>();
            List<bool> bools = new List<bool>();
            List<Pawn> pawnsInWorld = new List<Pawn>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(x => x.HasComp(typeof(PackComp))))
            {
                racesInPacks.Add(def);
            }
            if (checkOtherRaces)
            {
                foreach (ThingDef race in RimValiDefChecks.potentialPackRaces)
                {
                    racesInPacks.Add(race);
                }
            }
            if (allowAllRaces)
            {
                foreach (ThingDef race in RimValiDefChecks.potentialRaces)
                {
                    if (otherRaces.TryGetValue(race.defName) == true)
                    {
                        racesInPacks.Add(race);
                        if (enableDebug)
                        {
                            Log.Message("Adding race: " + race.defName + " to racesInPacks.");
                        }
                    }
                }
            }
        }
        
        public override void LoadedGame()
        {
            //StartedNewGame();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(x => x.HasComp(typeof(PackComp))))
            {
                racesInPacks.Add(def);
            }
            if (checkOtherRaces)
            {
                foreach (ThingDef race in RimValiDefChecks.potentialPackRaces)
                {
                    racesInPacks.Add(race);
                }
            }
            if (allowAllRaces)
            {
                foreach (ThingDef race in RimValiDefChecks.potentialRaces)
                {
                    if (otherRaces.TryGetValue(race.defName) == true)
                    {
                        racesInPacks.Add(race);
                        if (enableDebug)
                        {
                            Log.Message("Adding race: " + race.defName + " to racesInPacks.");
                        }
                    }
                }
            }
            base.LoadedGame();
        }
       
        
        public override void ExposeData()
        {
            if (!pawnsHaveHadPacks.EnumerableNullOrEmpty())
            {
                foreach (Pawn pawn in pawnsHaveHadPacks.Keys)
                {
                    pawns.Add(pawn);
                    bools.Add(pawnsHaveHadPacks[pawn]);
                }
            }
            //Scribe_Collections.Look<AvaliPack>(ref packs, "packs", LookMode.Deep);
            //Scribe_Collections.Look<Pawn, bool>(ref pawnsHaveHadPacks, "pawnsHasHadPacks", LookMode.Reference, LookMode.Undefined, ref pawns, ref bools);
            Scribe_Collections.Look<Pawn, bool>(ref pawnsHaveHadPacks, "pawnsHasHadPacks", LookMode.Reference, LookMode.Undefined, ref pawns, ref bools);
            Scribe_Collections.Look<AvaliPack>(ref packs, "packs", LookMode.Deep);


            if (pawnsHaveHadPacks == null)
            {
                pawnsHaveHadPacks = new Dictionary<Pawn, bool>();
            }
            if (packs == null)
            {
                packs = new List<AvaliPack>();
            }
            if (pawns == null)
            {
                pawns = new List<Pawn>();
            }
            if (bools == null)
            {
                bools = new List<bool>();
            }
            base.ExposeData();
        }
        public void UpdatePacks()
        {
            lock (packs)
            {
                
                if (!pawnsInWorld.EnumerableNullOrEmpty())
                {
                    foreach (Pawn pawn in pawnsInWorld)
                    {
                        PackComp comp = pawn.TryGetComp<PackComp>();
                        if (!(comp == null))
                        {
                            packs = RimValiUtility.EiPackHandler(packs, pawn, maxSize);
                        }
                    }
                }
            }
            ThreadIsActive = false;
        }

        public override void GameComponentTick()
        {

            if (onTick == 0 && packsEnabled && Find.CurrentMap != null)
            {
                //pawnsInWorld = RimVali.RimValiMapComponent.GetRimValiPawnTracker(Find.CurrentMap).AllPawnsOfRaceInWorld(racesInPacks).ToList();
                pawnsInWorld = RimValiCore.RimValiUtility.AllPawnsOfRaceInWorld(racesInPacks).Where(x => !x.story.traits.HasTrait(AvaliDefs.AvaliPackBroken)).ToList();
                if (multiThreaded && !ThreadIsActive)
                {
                    
                    ThreadIsActive = true;
                    Task packTask = new Task(UpdatePacks);
                    packTask.Start();
                }
                else
                {
                    UpdatePacks();
                }
                onTick = 120;
            }
            else
            {
                onTick--;
            }
        }
    }

   
}