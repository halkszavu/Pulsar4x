﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    internal class EconProcessor
    {
        [JsonProperty]
        private DateTime _lastRun = DateTime.MinValue;

        //internal void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        //{
        //    if (game.CurrentDateTime - _lastRun < game.Settings.EconomyCycleTime)
        //    {
        //        return;
        //    }

        //    _lastRun = game.CurrentDateTime;

        //    if (game.Settings.EnableMultiThreading ?? false)
        //    {
        //        Parallel.ForEach(systems, system => ProcessSystem(system));
        //    }
        //    else
        //    {
        //        foreach (var system in systems) //TODO thread this
        //        {
        //            ProcessSystem(system);
        //        }
        //    }
        //}

        internal static void ProcessSystem(EntityManager manager)
        {
            Game game = manager.Game;
            //Action<StarSystem> economyMethod = ProcessSystem;
            //system.SystemSubpulses.AddSystemInterupt(system.Game.CurrentDateTime + system.Game.Settings.EconomyCycleTime, economyMethod);
            manager.ManagerSubpulses.AddSystemInterupt(manager.Game.CurrentDateTime + manager.Game.Settings.EconomyCycleTime, PulseActionEnum.EconProcessor);
            

            TechProcessor.ProcessSystem(manager, game);

            foreach (Entity colonyEntity in manager.GetAllEntitiesWithDataBlob<MiningDB>())
            {
                game.ProcessorManager.Hotloop<MiningDB>(manager, 0);
            }
            foreach (Entity colonyEntity in manager.GetAllEntitiesWithDataBlob<RefiningDB>())
            {
                RefiningProcessor.RefineMaterials(colonyEntity, game.StaticData.ProcessedMaterials);
            }
            foreach (Entity colonyEntity in manager.GetAllEntitiesWithDataBlob<ConstructionDB>())
            {
                ConstructionProcessor.ConstructStuff(colonyEntity);
            }
            foreach (Entity colonyEntity in manager.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            {
                PopulationProcessor.GrowPopulation(colonyEntity);
            }
        }
    }
}