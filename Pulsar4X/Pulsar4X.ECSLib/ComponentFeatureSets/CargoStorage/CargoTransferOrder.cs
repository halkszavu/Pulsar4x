﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class CargoUnloadToOrder:EntityCommand
    {
         
        
        public List<(Guid ID, long amount)> ItemsGuidsToTransfer;
        [JsonIgnore]
        public List<(ICargoable item, long amount)> ItemICargoablesToTransfer = new List<(ICargoable item, long amount)>();
        public Guid SendCargoToEntityGuid { get; set; }

        public override int ActionLanes => 1;
        public override bool IsBlocking => true;
        
        public override string Name { get; } = "Cargo Transfer";
        public override string Details
        {
            get
            {
                string fromEntityName = _entityCommanding.GetDataBlob<NameDB>().GetName(factionEntity);
                string toEntityName = sendToEntity.GetDataBlob<NameDB>().GetName(factionEntity);
                string detailStr = "From " + fromEntityName + " To " + toEntityName;
                return detailStr;
            }
        }

        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        private CargoTransferDB _cargoTransferDB;

        [JsonIgnore]
        Entity factionEntity;
        [JsonIgnore]
        Entity sendToEntity;
        
        public static void CreateCommand(Entity faction, Entity cargoFromEntity, Entity cargoToEntity, List<(ICargoable item, long amount)> itemsToMove )
        {
            List<(Guid item, long amount)> itemGuidAmounts = new List<(Guid, long)>();
            foreach (var tup in itemsToMove)
            {
                itemGuidAmounts.Add((tup.item.ID, tup.amount));
            }
            var cmd = new CargoUnloadToOrder()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = cargoFromEntity.Guid,
                CreatedDate = cargoFromEntity.Manager.ManagerSubpulses.StarSysDateTime,
                SendCargoToEntityGuid = cargoToEntity.Guid,
                ItemsGuidsToTransfer = itemGuidAmounts,
                ItemICargoablesToTransfer = itemsToMove
            };
            StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
            
        }

        /// <summary>
        /// Validates and actions the command. 
        /// may eventualy need to return a responce instead of void. 
        /// This creates a CargoTranferDB from the command, which does all the work.
        /// the command is to create and enqueue a CargoTransferDB.
        /// </summary>
        internal override void ActionCommand(DateTime atDateTime)
        {
            if (!IsRunning)
            {
                _cargoTransferDB = new CargoTransferDB();
                _cargoTransferDB.CargoToEntity = sendToEntity;
                _cargoTransferDB.CargoToDB = sendToEntity.GetDataBlob<VolumeStorageDB>();
                _cargoTransferDB.CargoFromEntity = EntityCommanding;
                _cargoTransferDB.CargoFromDB = EntityCommanding.GetDataBlob<VolumeStorageDB>();

                _cargoTransferDB.ItemsLeftToTransfer = ItemICargoablesToTransfer;
                _cargoTransferDB.OrderedToTransfer = ItemICargoablesToTransfer;

                EntityCommanding.Manager.SetDataBlob(EntityCommanding.ID, _cargoTransferDB);
                CargoTransferProcessor.FirstRun(EntityCommanding);
                IsRunning = true;
            }
        }



        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out factionEntity, out _entityCommanding))
            {
                if (game.GlobalManager.FindEntityByGuid(SendCargoToEntityGuid, out sendToEntity))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool IsFinished()
        {
            if (_cargoTransferDB == null)
                return false;
            if (AmountLeftToXfer() > 0)
                return false;
            else
                return true;
        }
        long AmountLeftToXfer()
        {
            long amount = 0;
            foreach (var tup in _cargoTransferDB.ItemsLeftToTransfer)
            {
                amount += tup.Item2;
            }
            return amount;
         }
    }
    
    public class CargoLoadFromOrder : EntityCommand
    {
        public CargoUnloadToOrder Order;
        public override int ActionLanes { get; }
        public override bool IsBlocking { get; }
        public override string Name { get; } = "Cargo Transfer";

        public override string Details
        {
            get { return Order.Details; }
        }

        internal override Entity EntityCommanding { get; }
        internal override bool IsValidCommand(Game game)
        {
            throw new NotImplementedException();
        }

        public static void CreateCommand(Entity faction, Entity cargoFromEntity, Entity cargoToEntity, List<(ICargoable item, long amount)> itemsToMove )
        {
            List<(Guid item, long amount)> itemGuidAmounts = new List<(Guid, long)>();
            foreach (var tup in itemsToMove)
            {
                itemGuidAmounts.Add((tup.item.ID, tup.amount));
            }
            var unloadcmd = new CargoUnloadToOrder()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = cargoFromEntity.Guid,
                CreatedDate = cargoFromEntity.Manager.ManagerSubpulses.StarSysDateTime,
                SendCargoToEntityGuid = cargoToEntity.Guid,
                ItemsGuidsToTransfer = itemGuidAmounts,
                ItemICargoablesToTransfer = itemsToMove
            };

            var loadCmd = new CargoLoadFromOrder()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = cargoFromEntity.Guid,
                CreatedDate = cargoFromEntity.Manager.ManagerSubpulses.StarSysDateTime,
                Order = unloadcmd
            };
            StaticRefLib.Game.OrderHandler.HandleOrder(loadCmd);
        }
        
        
        internal override void ActionCommand(DateTime atDateTime)
        {
            //this needs to happen on a given trigger,ie a finished move command.
            throw new NotImplementedException();
        }

        public override bool IsFinished()
        {
            return Order.IsFinished();
        }
    }
}