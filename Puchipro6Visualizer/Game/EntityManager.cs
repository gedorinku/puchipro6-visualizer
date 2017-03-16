using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Puchipro6Visualizer.Game {
    class EntityManager : IRenderable {
        private readonly List<EntityBase> _entities;

        public EntityManager() {
            _entities = new List<EntityBase>();
        }

        public void Update(GameTime gameTime) {
            for (var i = 0; i < _entities.Count; ++i) {
                if (_entities[i].IsDeath) {
                    _entities.RemoveAt(i);
                    i--;
                    continue;
                }
                _entities[i].Update(gameTime);
            }
        }

        public void Render(GameTime gameTime) {
            foreach (var entityBase in _entities) {
                entityBase.Render(gameTime);
            }
        }

        public void Add(EntityBase entity) {
            if (entity == null) {
                throw new ArgumentNullException();
            }
            _entities.Add(entity);
        }
    }
}