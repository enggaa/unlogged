using System.Collections.Generic;
using UnityEngine;

namespace Patterns.Observer
{
    public interface IObserver {
        void OnNotification(object sender, Message subject, params object[] args);
    }

    public static class MessageSystem
    {
        private static Dictionary<Message, List<IObserver>> listeners;

        public static bool initialized = false;

        public static void Init()
        {
            initialized = true;
            listeners = new Dictionary<Message, List<IObserver>>();
        }

        public static void Notify(this object sender, Message subject, params object[] args)
        {
            if (!MessageSystem.initialized)
                Init();

            if (sender == null)
                Debug.LogError("Sender is null!");

            if (listeners.ContainsKey(subject))
                foreach (var observer in listeners[subject])
                    observer.OnNotification(sender, subject, args);
        }

        public static void Observe(this IObserver observer, Message msgType)
        {
            if (!MessageSystem.initialized)
                Init();

            if (!listeners.ContainsKey(msgType))
                listeners.Add(msgType, new List<IObserver>() { observer });
            else
            {
                if (!listeners[msgType].Contains(observer))
                    listeners[msgType].Add(observer);
            }
        }

        public static void Unobserve(this IObserver observer, Message msgType)
        {
            if (!MessageSystem.initialized)
                return;

            if (listeners.ContainsKey(msgType))
            {
                if (listeners[msgType].Contains(observer))
                {
                    listeners[msgType].Remove(observer);
                }
                
                // 리스트가 비어있으면 딕셔너리에서 제거
                if (listeners[msgType].Count == 0)
                {
                    listeners.Remove(msgType);
                }
            }
        }

        public static void UnobserveAll(this IObserver observer)
        {
            if (!MessageSystem.initialized)
                return;

            foreach (var kvp in listeners)
            {
                kvp.Value.Remove(observer);
            }
        }
    }
}