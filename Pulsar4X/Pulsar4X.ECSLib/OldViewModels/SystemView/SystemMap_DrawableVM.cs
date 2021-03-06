﻿using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Pulsar4X.ECSLib
{
    public class SystemMap_DrawableVM : ViewModelBase
    {

        private Entity _viewingFaction;
        public ObservableCollection<Entity> IconableEntitys { get; } = new ObservableCollection<Entity>();
        private HashSet<Entity> _iconableEntites = new HashSet<Entity>(); //doing this just allows quick observablellist.contains without a dictionary, since dictionarys are not observable

        public ManagerSubPulse SystemSubpulse { get; private set; }
        private AEntityChangeListner _changeListner;
        public StarSystem StarSystem;
        //public Dictionary<IDrawIconMethod, List<AIconData<BaseDataBlob>>> IconsDictionary = new Dictionary<IDrawIconMethod, List<AIconData<BaseDataBlob>>>();

        public void InitializeForGM(GameVM gameVM, StarSystem starSys)
        {
            _changeListner = new EntityChangeListnerSM(starSys);

            foreach (var entityWithPosition in starSys.GetAllEntitiesWithDataBlob<PositionDB>())
            {
                AddIconableEntity(entityWithPosition);
                _changeListner.ListningToEntites.Add(entityWithPosition);
            }


        }

        public void Initialise(GameVM gameVM, StarSystem starSys, Entity viewingFaction)
        {
            _viewingFaction = viewingFaction;
            StarSystem = starSys;
            var listnerblobs = new List<int>();
            listnerblobs.Add(EntityManager.DataBlobTypes[typeof(PositionDB)]);
            EntityChangeListner changeListner = new EntityChangeListner(starSys, viewingFaction, listnerblobs);
            _changeListner = changeListner;


            IconableEntitys.Clear();
            _iconableEntites.Clear();


            foreach (Entity entity in _changeListner.ListningToEntites)
            {
                AddIconableEntity(entity);
            }
            SystemSubpulse = starSys.ManagerSubpulses;
            //starSys.SystemManager.GetAllEntitiesWithDataBlob<NewtonBalisticDB>(gameVM.CurrentAuthToken);

            OnPropertyChanged(nameof(IconableEntitys));
        }

        private void AddIconableEntity(Entity entity)
        {
            if (!_iconableEntites.Contains(entity))
            {
                _iconableEntites.Add(entity);
                IconableEntitys.Add(entity);
            }
        }
        private void RemoveIconableEntity(Entity entity)
        {
            if (_iconableEntites.Contains(entity))
            {
                IconableEntitys.Remove(entity);
                _iconableEntites.Remove(entity);
            }
        }

        public bool UpdatesReady
        {
            get
            {
                return _changeListner.HasUpdates();
            }
        }

        public List<EntityChangeData> GetUpdates()
        {

            var changes = new List<EntityChangeData>();

            EntityChangeData change;
            while (_changeListner.TryDequeue(out change))
            {
                
                switch (change.ChangeType)
                {
                    case EntityChangeData.EntityChangeType.EntityAdded:
                        if (change.Entity.HasDataBlob<PositionDB>())
                        {
                            AddIconableEntity(change.Entity);
                        }
                        break;
                    case EntityChangeData.EntityChangeType.EntityRemoved:
                        RemoveIconableEntity(change.Entity);
                        break;

                    case EntityChangeData.EntityChangeType.DBAdded:
                        if (change.Datablob is PositionDB)
                            AddIconableEntity(change.Entity);
                        else
                            changes.Add(change);
                        break;
                    case EntityChangeData.EntityChangeType.DBRemoved:
                        if (change.Datablob is PositionDB)
                            RemoveIconableEntity(change.Entity);
                        else
                            changes.Add(change);
                        break;
                }

            }
        
            return changes;
        }
    }

    public interface IDrawIconMethod
    {
        void UpdateData();
        void DrawIcons();
    }
    public abstract class AIconData //where T: BaseDataBlob
    {
        protected PositionDB Position;
        protected MassVolumeDB massVol;

    }



}
