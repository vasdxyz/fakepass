using System;
using UnityEngine;

namespace Xyz.Vasd.FakePass
{
    public class FakePassBase : MonoBehaviour
    {
        [Serializable]
        public struct InjectionSettingsData
        {
            public bool AutoInject;
            public bool AutoFindInjector;
            public FakePassInjector Injector;
        }

        public InjectionSettingsData InjectionSetttings;

        protected virtual object Source => this;

        private void OnEnable()
        {
            if (InjectionSetttings.AutoFindInjector) FindInjector();
            if (InjectionSetttings.AutoInject) Attach();
        }

        private void OnDisable()
        {
            Detach();
        }

        private void OnDestroy()
        {
            Detach();
        }

        [ContextMenu(nameof(Attach) + "()")]
        protected virtual void Attach()
        {
            if (InjectionSetttings.Injector != null && !InjectionSetttings.Injector.Contains(Source)) 
                InjectionSetttings.Injector.Add(Source);
        }

        [ContextMenu(nameof(Detach) + "()")]
        protected virtual void Detach()
        {
            if (InjectionSetttings.Injector != null && InjectionSetttings.Injector.Contains(Source)) 
                InjectionSetttings.Injector.Remove(Source);
        }

        protected void FindInjector()
        {
            if (InjectionSetttings.Injector != null) return;
            InjectionSetttings.Injector = FindObjectOfType<FakePassInjector>();
        }
    }
}